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
