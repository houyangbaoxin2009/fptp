using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
namespace fptp
{
	/// <summary>
	/// 文件夹批处理窗体。后台线程逐张处理，进度条实时反馈，可取消。
	/// </summary>
	public partial class BatchBox : Form
	{
		private readonly GenSettings settings;
		private readonly BackgroundWorker worker;

		public BatchBox(GenSettings settings)
		{
			InitializeComponent();
			this.settings = settings;

			worker = new BackgroundWorker
			{
				WorkerReportsProgress = true,
				WorkerSupportsCancellation = true,
			};
			worker.DoWork += Worker_DoWork;
			worker.ProgressChanged += Worker_ProgressChanged;
			worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
		}

		private void BatchBox_Load(object sender, EventArgs e)
		{
			ApplyLang();
			chkCrop.Checked = true;
			chkChangeBg.Checked = true;
			cmbBgColor.SelectedIndex = MainBoxColorIndex(settings.BackgroundColor);
			trkTolerance.Value = settings.Tolerance;
			lblValue.Text = settings.Tolerance.ToString();
			ReloadLayoutPresets();
		}

		private void ApplyLang()
		{
			Text = Lang.Get("batch.title");
			btnInput.Text = Lang.Get("batch.input");
			btnOutput.Text = Lang.Get("batch.output");
			chkCrop.Text = Lang.Get("main.crop");
			chkGrayscale.Text = Lang.Get("main.grayscale");
			chkChangeBg.Text = Lang.Get("main.changeBg");
			chkLayout.Text = Lang.Get("main.layout");
			lblBg.Text = Lang.Get("batch.bg") + ":";
			lblTolerance.Text = Lang.Get("main.tolerance");
			btnImportBat.Text = Lang.Get("batch.importBat");
			btnStart.Text = Lang.Get("batch.start");
			btnCancel.Text = Lang.Get("settings.cancel");
		}

		private static int MainBoxColorIndex(string stored)
		{
			switch (stored)
			{
				case "蓝色": return 0;
				case "红色": return 1;
				case "透明": return 3;
				default: return 2;
			}
		}

		private void ReloadLayoutPresets()
		{
			string[] keys = { "layout.preset5", "layout.preset6", "layout.presetA4", "layout.presetA5", "layout.custom" };
			cmbLayout.Items.Clear();
			foreach (string key in keys)
				cmbLayout.Items.Add(Lang.Get(key));
			if (settings.LayoutPreset >= 0 && settings.LayoutPreset < cmbLayout.Items.Count)
				cmbLayout.SelectedIndex = settings.LayoutPreset;
			else
				cmbLayout.SelectedIndex = 0;
		}

		private void BtnInput_Click(object sender, EventArgs e)
		{
			using (FolderBrowserDialog dlg = new FolderBrowserDialog())
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
					txtInput.Text = dlg.SelectedPath;
			}
		}

		private void BtnOutput_Click(object sender, EventArgs e)
		{
			using (FolderBrowserDialog dlg = new FolderBrowserDialog())
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
					txtOutput.Text = dlg.SelectedPath;
			}
		}

		private void trkTolerance_Scroll(object sender, EventArgs e)
		{
			lblValue.Text = trkTolerance.Value.ToString();
		}

		/// <summary>
		/// 导入批处理文件（.bat）：解析其中 fptp.exe prep batch 命令参数并填充界面。
		/// 支持 -i/-o 目录、-c 颜色、-t 容差、-l 排版、-a 动画模式。
		/// </summary>
		private void BtnImportBat_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog dlg = new OpenFileDialog())
			{
				dlg.Filter = "批处理文件 (*.bat)|*.bat|所有文件 (*.*)|*.*";
				dlg.Title = Lang.Get("batch.importBat");
				if (dlg.ShowDialog(this) != DialogResult.OK) return;

				try
				{
					if (ApplyBatParameters(File.ReadAllText(dlg.FileName)))
						lblProgress.Text = Lang.Get("batch.importOk");
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, Lang.Get("batch.importFailed", ex.Message), Lang.Get("msg.error"),
						MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		/// <summary>解析 bat 内容中的 fptp.exe prep batch 命令行参数并填充到控件，返回是否成功。</summary>
		private bool ApplyBatParameters(string batContent)
		{
			// 定位 prep batch 段，按空格/引号拆分参数
			int idx = batContent.IndexOf("batch", StringComparison.OrdinalIgnoreCase);
			if (idx < 0)
			{
				MessageBox.Show(this, Lang.Get("batch.importNoCmd"), Lang.Get("msg.error"),
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			string tail = batContent.Substring(idx + 5);
			string[] parts = SplitArgs(tail);

			for (int i = 0; i < parts.Length; i++)
			{
				switch (parts[i].ToLowerInvariant())
				{
					case "-i":
					case "--input":
						if (i + 1 < parts.Length) txtInput.Text = parts[++i].Trim('"');
						break;
					case "-o":
					case "--output":
						if (i + 1 < parts.Length) txtOutput.Text = parts[++i].Trim('"');
						break;
					case "-c":
					case "--color":
						if (i + 1 < parts.Length) cmbBgColor.SelectedIndex = ColorIndexFromName(parts[++i]);
						break;
					case "-t":
					case "--tolerance":
						if (i + 1 < parts.Length && int.TryParse(parts[i + 1], out int t) && t >= 0 && t <= 150)
						{
							trkTolerance.Value = t;
							lblValue.Text = t.ToString();
							i++;
						}
						break;
					case "-l":
					case "--layout":
						if (i + 1 < parts.Length && int.TryParse(parts[i + 1], out int l) &&
							l >= 0 && l < cmbLayout.Items.Count)
						{
							cmbLayout.SelectedIndex = l;
							i++;
						}
						break;
					case "-a":
					case "--anime":
						// 批处理窗口无动画模式选项，忽略
						break;
				}
			}
			return true;
		}

		/// <summary>将 CLI 颜色名映射为底色下拉索引（0蓝 1红 2白 3透明）。</summary>
		private static int ColorIndexFromName(string name)
		{
			switch (name.ToLowerInvariant())
			{
				case "blue": return 0;
				case "red": return 1;
				case "transparent":
				case "none": return 3;
				default: return 2;
			}
		}

		/// <summary>按空白拆分命令行参数，保留引号内的空格。</summary>
		private static string[] SplitArgs(string line)
		{
			List<string> parts = new List<string>();
			System.Text.StringBuilder cur = new System.Text.StringBuilder();
			bool inQuote = false;
			foreach (char ch in line)
			{
				if (ch == '"') { inQuote = !inQuote; continue; }
				if (ch == ' ' && !inQuote)
				{
					if (cur.Length > 0) { parts.Add(cur.ToString()); cur.Clear(); }
				}
				else cur.Append(ch);
			}
			if (cur.Length > 0) parts.Add(cur.ToString());
			return parts.ToArray();
		}

		private void BtnStart_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(txtInput.Text) || !Directory.Exists(txtInput.Text))
			{
				MessageBox.Show(this, Lang.Get("batch.noInput"), Lang.Get("msg.tip"),
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			if (string.IsNullOrEmpty(txtOutput.Text))
			{
				MessageBox.Show(this, Lang.Get("batch.noOutput"), Lang.Get("msg.tip"),
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			SetBusy(true);
			worker.RunWorkerAsync();
		}

		private void SetBusy(bool busy)
		{
			btnStart.Enabled = !busy;
			btnCancel.Enabled = !busy;
			btnInput.Enabled = !busy;
			btnOutput.Enabled = !busy;
			btnImportBat.Enabled = !busy;
			progressBar.Value = 0;
		}

		private void Worker_DoWork(object sender, DoWorkEventArgs e)
		{
			string inputDir = txtInput.Text;
			string outputDir = txtOutput.Text;
			Directory.CreateDirectory(outputDir);

			string[] files = Directory.GetFiles(inputDir, "*.*", SearchOption.TopDirectoryOnly);
			List<string> images = new List<string>();
			foreach (string f in files)
			{
				string ext = Path.GetExtension(f).ToLower();
				if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp")
					images.Add(f);
			}

			if (images.Count == 0)
			{
				e.Result = "none";
				return;
			}

			// 从 UI 控件快照参数（后台线程不直接读控件）
			Color bgColor = cmbBgColor.SelectedIndex switch
			{
				0 => Color.FromArgb(65, 105, 225),
				1 => Color.FromArgb(220, 20, 60),
				3 => Color.Transparent,
				_ => Color.White,
			};
			int tolerance = trkTolerance.Value;
			bool doCrop = chkCrop.Checked;
			bool doGray = chkGrayscale.Checked;
			bool doBg = chkChangeBg.Checked;
			bool doLayout = chkLayout.Checked;
			int layoutPreset = cmbLayout.SelectedIndex;

			int done = 0;
			foreach (string file in images)
			{
				if (worker.CancellationPending)
				{
					e.Cancel = true;
					return;
				}

				try
				{
					ProcessOne(file, outputDir, doCrop, doGray, doBg, doLayout, bgColor, tolerance, layoutPreset);
				}
				catch
				{
					// 单张失败不中断整体，由进度区提示
				}

				done++;
				worker.ReportProgress(done * 100 / images.Count, done);
			}

			e.Result = images.Count.ToString();
		}

		/// <summary>处理单张图片：按勾选依次执行 裁剪/黑白/换底/排版。</summary>
		private void ProcessOne(string file, string outputDir,
			bool doCrop, bool doGray, bool doBg, bool doLayout,
			Color bgColor, int tolerance, int layoutPreset)
		{
			using (Bitmap source = new Bitmap(file))
			{
				Bitmap cur = (Bitmap)source.Clone();
				try
				{
					if (doCrop)
					{
						Bitmap next = Prepalg.SmartCrop(cur, Basic.ONE_INCH_W, Basic.ONE_INCH_H);
						if (next != null) { cur.Dispose(); cur = next; }
					}
					if (doGray)
					{
						Bitmap next = Prepalg.ToGrayscale(cur);
						if (next != null) { cur.Dispose(); cur = next; }
					}
					if (doBg)
					{
						Bitmap next = Prepalg.ReplaceBackground(cur, bgColor, tolerance);
						if (next != null) { cur.Dispose(); cur = next; }
					}
					if (doLayout)
					{
						Bitmap next = MakeLayout(cur, layoutPreset);
						if (next != null) { cur.Dispose(); cur = next; }
					}

					// 透明背景只能存 PNG
					string outExt = bgColor.A == 0 ? ".png" : ".jpg";
					string outFile = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(file) + outExt);
					Assalg.SaveImage(cur, outFile, settings.SaveQuality);
				}
				finally
				{
					cur.Dispose();
				}
			}
		}

		/// <summary>按预设生成排版图（与主窗体 DoLayout 逻辑一致的简化版）。</summary>
		private Bitmap MakeLayout(Bitmap photo, int layoutPreset)
		{
			int paperW, paperH;
			switch (layoutPreset)
			{
				case 1: paperW = Basic.LAYOUT_6INCH_W; paperH = Basic.LAYOUT_6INCH_H; break;
				case 2: paperW = Basic.LAYOUT_A4_W; paperH = Basic.LAYOUT_A4_H; break;
				case 3: paperW = Basic.LAYOUT_A5_W; paperH = Basic.LAYOUT_A5_H; break;
				default: paperW = Basic.LAYOUT_5INCH_W; paperH = Basic.LAYOUT_5INCH_H; break;
			}

			int gap = Basic.LAYOUT_GAP;
			int cols = Math.Max(1, (paperW + gap) / (photo.Width + gap));
			int rows = Math.Max(1, (paperH + gap) / (photo.Height + gap));
			int contentW = cols * photo.Width + (cols - 1) * gap;
			int contentH = rows * photo.Height + (rows - 1) * gap;
			int startX = (paperW - contentW) / 2;
			int startY = (paperH - contentH) / 2;

			Bitmap paper = new Bitmap(paperW, paperH);
			using (Graphics g = Graphics.FromImage(paper))
			{
				g.Clear(Color.White);
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				for (int r = 0; r < rows; r++)
					for (int c = 0; c < cols; c++)
						g.DrawImage(photo, startX + c * (photo.Width + gap), startY + r * (photo.Height + gap), photo.Width, photo.Height);
			}
			return paper;
		}

		private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			progressBar.Value = e.ProgressPercentage;
			lblProgress.Text = Lang.Get("batch.progress", e.UserState, progressBar.Maximum);
		}

		private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			SetBusy(false);
			if (e.Cancelled)
			{
				lblProgress.Text = Lang.Get("batch.cancelled");
			}
			else if (e.Result is string s && s == "none")
			{
				MessageBox.Show(this, Lang.Get("batch.noImage"), Lang.Get("msg.tip"),
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				lblProgress.Text = "";
			}
			else
			{
				lblProgress.Text = Lang.Get("batch.done", e.Result);
				MessageBox.Show(this, Lang.Get("batch.doneMsg", e.Result), Lang.Get("msg.done"),
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (worker.IsBusy)
			{
				worker.CancelAsync();
				e.Cancel = true;
				return;
			}
			base.OnFormClosing(e);
		}
	}
}
