using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace fptp
{
	public partial class mainBox : Form
	{
		private Bitmap sourceImage;
		private Bitmap currentImage;
		private GenSettings settings;
		private AppSettings appSettings;
		private bool _applyingSettings;
		private readonly Stack<Bitmap> undoStack = new Stack<Bitmap>();
		private const int MaxUndoSteps = 20;

		public mainBox()
		{
			InitializeComponent();
			Assalg.MergeInstallOptions();
			settings = Assalg.LoadGenSettings();
			appSettings = Assalg.LoadAppSettings();
			Lang.Load(appSettings.Language);
		}

		/// <summary>
		/// 应用当前语言：刷新所有静态控件文本与下拉项。
		/// </summary>
		private void ApplyLang()
		{
			Text = Basic.GetAppTitle();
			groupBox3.Text = Lang.Get("main.importGroup");
			btnLoad.Text = Lang.Get("main.load");
			groupBox2.Text = Lang.Get("main.prepGroup");
			btnAutoCrop.Text = Lang.Get("main.crop");
			btnBlackWhite.Text = Lang.Get("main.grayscale");
			btnChangeBg.Text = Lang.Get("main.changeBg");
			label1.Text = Lang.Get("main.tolerance");
			groupBox1.Text = Lang.Get("main.layoutGroup");
			btnLayout.Text = Lang.Get("main.layout");
			groupBox4.Text = Lang.Get("main.finishGroup");
			btnSave.Text = Lang.Get("main.save");
			btnPrint.Text = Lang.Get("main.print");
			btnUnload.Text = Lang.Get("main.unload");
			btnAbout.Text = Lang.Get("main.about");
			btnSettings.Text = Lang.Get("main.settings");
			groupBox5.Text = Lang.Get("main.historyGroup");
			btnUndo.Text = Lang.Get("main.undo");
			btnReload.Text = Lang.Get("main.reload");

			string[] colors = { "蓝色", "红色", "白色" };
			string[] colorKeys = { "color.blue", "color.red", "color.white" };
			string selColor = cmbBgColor.Text;
			cmbBgColor.Items.Clear();
			for (int i = 0; i < colors.Length; i++)
				cmbBgColor.Items.Add(Lang.Get(colorKeys[i]));
			cmbBgColor.Text = TranslateSetting(selColor, colors, colorKeys);

			ReloadLayoutPresets();
		}

		/// <summary>
		/// 将设置中存储的默认值翻译为当前语言的显示文本。
		/// </summary>
		private static string TranslateSetting(string stored, string[] zhValues, string[] keys)
		{
			for (int i = 0; i < zhValues.Length; i++)
				if (stored == zhValues[i]) return Lang.Get(keys[i]);
			return stored;
		}

		/// <summary>
		/// 按当前语言与设置的排版预设重建下拉项。
		/// </summary>
		private void ReloadLayoutPresets()
		{
			string[] keys = { "layout.preset5", "layout.preset6", "layout.presetA4", "layout.presetA5", "layout.custom" };
			int sel = cmbLayout.SelectedIndex >= 0 ? cmbLayout.SelectedIndex : settings.LayoutPreset;
			cmbLayout.Items.Clear();
			foreach (string key in keys)
				cmbLayout.Items.Add(Lang.Get(key));
			if (sel >= 0 && sel < cmbLayout.Items.Count)
				cmbLayout.SelectedIndex = sel;
			else
				cmbLayout.SelectedIndex = settings.LayoutPreset;
		}

		private void ApplySettings()
		{
			_applyingSettings = true;
			cmbBgColor.SelectedIndex = ColorIndexFromStored(settings.BackgroundColor);
			TrackBar.Value = settings.Tolerance;
			_applyingSettings = false;
		}

		/// <summary>
		/// 将设置中存储的颜色值（蓝色/红色/白色）映射为下拉索引。
		/// </summary>
		private static int ColorIndexFromStored(string stored)
		{
			switch (stored)
			{
				case "蓝色": return 0;
				case "红色": return 1;
				default: return 2;
			}
		}

		private void SaveBgColorSetting()
		{
			switch (cmbBgColor.SelectedIndex)
			{
				case 0: settings.BackgroundColor = "蓝色"; break;
				case 1: settings.BackgroundColor = "红色"; break;
				default: settings.BackgroundColor = "白色"; break;
			}
			Assalg.SaveGenSettings(settings);
		}

		private void SaveToleranceSetting()
		{
			settings.Tolerance = TrackBar.Value;
			Assalg.SaveGenSettings(settings);
		}

		private void BtnLoad_Click(object sender, EventArgs e)
		{
			string filePath = Basic.OpenImageFile(this);

			if (!string.IsNullOrEmpty(filePath))
			{
				try
				{
					var tempImage = new Bitmap(filePath);
					currentImage = (Bitmap)tempImage.Clone();
					sourceImage = tempImage;

					int minSide = Math.Min(sourceImage.Width, sourceImage.Height);

					if (minSide < 300)
					{
						MessageBox.Show(Lang.Get("msg.loadedBad"),
										Lang.Get("msg.badImage"), MessageBoxButtons.OK, MessageBoxIcon.Error);
						currentImage.Dispose();
						sourceImage.Dispose();
						currentImage = null;
						sourceImage = null;
						return;
					}
					else if (minSide < 600)
					{
						MessageBox.Show(Lang.Get("msg.loadedLow"),
										Lang.Get("msg.tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
					else
					{
						lblInfo.Text = Lang.Get("msg.loadedOk", currentImage.Width, currentImage.Height);
					}

					pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
					pictureBox1.Image = currentImage;

					ClearUndo();
					btnReload.Enabled = true;

					ClearPublishFiles();
					ExportStage("原始图片", currentImage);
				}
				catch (Exception ex)
				{
					MessageBox.Show(Lang.Get("msg.loadFailed", ex.Message));
				}
			}
		}

		private void BtnAutoCrop_Click(object sender, EventArgs e)
		{
			if (!Basic.CheckImage(currentImage, this)) return;

			PushUndo();
			settings = Assalg.LoadGenSettings();
			int targetW = settings.DefaultSize switch
			{
				2 => Basic.TWO_INCH_W,
				3 => Basic.PASSPORT_W,
				_ => Basic.ONE_INCH_W,
			};
			int targetH = settings.DefaultSize switch
			{
				2 => Basic.TWO_INCH_H,
				3 => Basic.PASSPORT_H,
				_ => Basic.ONE_INCH_H,
			};

			lblInfo.Text = Lang.Get("msg.cropping");
			Application.DoEvents();

			Bitmap croppedImage = Prepalg.SmartCrop(currentImage, targetW, targetH);

			currentImage.Dispose();
			currentImage = croppedImage;

			pictureBox1.Image = currentImage;
			pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

			string sizeName = settings.DefaultSize switch
			{
				2 => Lang.Get("size.two"),
				3 => Lang.Get("size.passport"),
				_ => Lang.Get("size.one"),
			};
			lblInfo.Text = Lang.Get("msg.cropped", sizeName, targetW, targetH);

			ExportStage("智能裁剪", currentImage);
		}

		private void BtnBlackWhite_Click(object sender, EventArgs e)
		{
			if (!Basic.CheckImage(currentImage, this)) return;

			PushUndo();
			this.Cursor = Cursors.WaitCursor;

			Bitmap bwImage = Prepalg.ToGrayscale(currentImage);

			currentImage.Dispose();
			currentImage = bwImage;
			pictureBox1.Image = currentImage;

			this.Cursor = Cursors.Default;
			lblInfo.Text = Lang.Get("msg.bwDone");

			ExportStage("黑白", currentImage);
		}

		private void BtnChangeBg_Click(object sender, EventArgs e)
		{
			if (!Basic.CheckImage(currentImage, this)) return;

			PushUndo();
			settings = Assalg.LoadGenSettings();

			Color targetColor = Color.White;
			switch (cmbBgColor.SelectedIndex)
			{
				case 0: targetColor = Color.FromArgb(65, 105, 225); break;   // 蓝色
				case 1: targetColor = Color.FromArgb(220, 20, 60); break;    // 红色
				default: targetColor = Color.White; break;                   // 白色
			}

			lblInfo.Text = Lang.Get("msg.bgWorking");
			Application.DoEvents();
			this.Cursor = Cursors.WaitCursor;

			int tolerance = TrackBar.Value;
			Bitmap newImage = Prepalg.ReplaceBackground(currentImage, targetColor, tolerance, this);

			currentImage.Dispose();
			currentImage = newImage;
			pictureBox1.Image = currentImage;

			this.Cursor = Cursors.Default;
			lblInfo.Text = Lang.Get("msg.bgDone");

			ExportStage("换底色", currentImage);
		}

		private void BtnLayout_Click(object sender, EventArgs e)
		{
			if (!Basic.CheckImage(currentImage, this)) return;

			settings = Assalg.LoadGenSettings();
			int preset = cmbLayout.SelectedIndex >= 0 ? cmbLayout.SelectedIndex : settings.LayoutPreset;

			int paperW, paperH;
			switch (preset)
			{
				case 1: paperW = Basic.LAYOUT_6INCH_W; paperH = Basic.LAYOUT_6INCH_H; break;
				case 2: paperW = Basic.LAYOUT_A4_W; paperH = Basic.LAYOUT_A4_H; break;
				case 3: paperW = Basic.LAYOUT_A5_W; paperH = Basic.LAYOUT_A5_H; break;
				case 4:
					if (!TryGetCustomSize(out paperW, out paperH)) return;
					settings.CustomLayoutW = paperW;
					settings.CustomLayoutH = paperH;
					break;
				default: paperW = Basic.LAYOUT_5INCH_W; paperH = Basic.LAYOUT_5INCH_H; break;
			}

			settings.LayoutPreset = preset;
			Assalg.SaveGenSettings(settings);

			string layoutName = Lang.Get(new[]
			{
				"layout.preset5", "layout.preset6", "layout.presetA4", "layout.presetA5", "layout.custom"
			}[preset]);
			lblInfo.Text = Lang.Get("msg.layoutWorking", layoutName);

			DoLayout(paperW, paperH);
		}

		/// <summary>
		/// 弹出自定义尺寸输入框，校验并返回宽高。
		/// </summary>
		private bool TryGetCustomSize(out int width, out int height)
		{
			width = settings.CustomLayoutW;
			height = settings.CustomLayoutH;

			using (CustomSizeBox dialog = new CustomSizeBox(width, height))
			{
				if (dialog.ShowDialog(this) != DialogResult.OK) return false;
				width = dialog.WidthValue;
				height = dialog.HeightValue;
			}

			if (width < 100 || width > 10000 || height < 100 || height > 10000)
			{
				MessageBox.Show(Lang.Get("msg.customSizeInvalid"), Lang.Get("msg.error"),
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			return true;
		}

		/// <summary>
		/// 统一的排版算法：在相纸上居中排列照片，带虚线裁剪辅助线。
		/// </summary>
		private void DoLayout(int paperWidth, int paperHeight)
		{
			int photoW = currentImage.Width;
			int photoH = currentImage.Height;
			int gap = Basic.LAYOUT_GAP;

			int cols = Math.Max(1, (paperWidth + gap) / (photoW + gap));
			int rows = Math.Max(1, (paperHeight + gap) / (photoH + gap));

			int contentWidth = cols * photoW + (cols - 1) * gap;
			int contentHeight = rows * photoH + (rows - 1) * gap;

			int startX = (paperWidth - contentWidth) / 2;
			int startY = (paperHeight - contentHeight) / 2;

			Bitmap layoutPaper = new Bitmap(paperWidth, paperHeight);
			using (Graphics g = Graphics.FromImage(layoutPaper))
			{
				g.Clear(Color.White);
				g.SmoothingMode = SmoothingMode.HighQuality;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;

				for (int r = 0; r < rows; r++)
				{
					for (int c = 0; c < cols; c++)
					{
						int x = startX + c * (photoW + gap);
						int y = startY + r * (photoH + gap);

						g.DrawImage(currentImage, x, y, photoW, photoH);

						// 辅助线样式：0=虚线 1=实线 2=无
						if (settings.GuideLineStyle != 2)
						{
							using (Pen pen = new Pen(Color.LightGray, 1))
							{
								pen.DashStyle = settings.GuideLineStyle == 1 ? DashStyle.Solid : DashStyle.Dash;
								g.DrawRectangle(pen, x, y, photoW, photoH);
							}
						}
					}
				}
			}

			if (pictureBox1.Image != currentImage)
				pictureBox1.Image?.Dispose();
			pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
			pictureBox1.Image = layoutPaper;

			string layoutName = Lang.Get(new[]
			{
				"layout.preset5", "layout.preset6", "layout.presetA4", "layout.presetA5", "layout.custom"
			}[settings.LayoutPreset]);
			lblInfo.Text = Lang.Get("msg.layoutDone", layoutName, cols, rows, cols * rows);

			ExportStage("排版", layoutPaper);
		}

		private void BtnSave_Click(object sender, EventArgs e)
		{
			var toSave = (Bitmap)pictureBox1.Image;
			if (!Basic.CheckImage(toSave, this)) return;

			settings = Assalg.LoadGenSettings();

			string ext = settings.SaveFormat;
			string[] formats = { "jpg", "png", "bmp", "tiff", "gif" };
			string[] formatKeys = { "fmt.jpg", "fmt.png", "fmt.bmp", "fmt.tiff", "fmt.gif" };

			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				// 构造格式过滤器：JPEG|*.jpg|PNG|*.png|...
				string filter = string.Join("|", System.Linq.Enumerable.Range(0, formats.Length)
					.Select(i => $"{Lang.Get(formatKeys[i])}|*.{formats[i]}"));
				sfd.Filter = filter;
				sfd.FileName = $"{Basic.AppName}_照片.{ext}";
				sfd.FilterIndex = Math.Max(1, Array.IndexOf(formats, ext) + 1);

				if (sfd.ShowDialog(this) == DialogResult.OK)
				{
					try
					{
						this.Cursor = Cursors.WaitCursor;

						Assalg.SaveImage(toSave, sfd.FileName, settings.SaveQuality);

						lblInfo.Text = Lang.Get("msg.saveOk");
						MessageBox.Show(Lang.Get("msg.saved"), Lang.Get("msg.done"), MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show(Lang.Get("msg.saveFailed", ex.Message));
					}
					finally
					{
						this.Cursor = Cursors.Default;
					}
				}
			}
		}

		private void BtnPrint_Click(object sender, EventArgs e)
		{
			Image toPrint = pictureBox1.Image;
			if (!Basic.CheckImage((Bitmap)toPrint, this)) return;

			try
			{
				using (PrintDocument pd = new PrintDocument())
				using (PrintDialog dlg = new PrintDialog())
				{
					dlg.Document = pd;
					if (dlg.ShowDialog(this) != DialogResult.OK) return;

					pd.PrintPage += (s, ev) =>
					{
						// 按页面可打印区域等比缩放，居中打印
						RectangleF bounds = ev.MarginBounds;
						float scale = Math.Min(bounds.Width / toPrint.Width, bounds.Height / toPrint.Height);
						int w = (int)(toPrint.Width * scale);
						int h = (int)(toPrint.Height * scale);
						int x = (int)(bounds.X + (bounds.Width - w) / 2);
						int y = (int)(bounds.Y + (bounds.Height - h) / 2);
						ev.Graphics.DrawImage(toPrint, x, y, w, h);
						ev.HasMorePages = false;
					};

					lblInfo.Text = Lang.Get("msg.printing");
					Application.DoEvents();
					pd.Print();
					lblInfo.Text = Lang.Get("msg.printOk");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(Lang.Get("msg.printFailed", ex.Message), Lang.Get("msg.error"),
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnSettings_Click(object sender, EventArgs e)
		{
			settings = Assalg.LoadGenSettings();
			using (GenSettingsBox dialog = new GenSettingsBox(settings))
			{
				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					settings = dialog.Result;
					Assalg.SaveGenSettings(settings);
					appSettings = dialog.AppResult;
					Assalg.SaveAppSettings(appSettings);
					ApplySettings();
					Lang.Load(appSettings.Language);
					ApplyLang();
					lblInfo.Text = Lang.Get("msg.settingsSaved");
				}
			}
		}

		private void BtnAbout_Click(object sender, EventArgs e)
		{
			using (AboutBox about = new AboutBox())
			{
				about.ShowDialog(this);
			}
		}

		private void BtnUnload_Click(object sender, EventArgs e)
		{
			if (pictureBox1.Image != null && pictureBox1.Image != currentImage)
				pictureBox1.Image.Dispose();

			pictureBox1.Image = null;

			if (currentImage != null)
			{
				currentImage.Dispose();
				currentImage = null;
			}

			if (sourceImage != null)
			{
				sourceImage.Dispose();
				sourceImage = null;
			}

			ClearUndo();
			ClearPublishFiles();
			btnReload.Enabled = false;
			lblInfo.Text = Lang.Get("msg.unloaded");
		}

		private void Form1_Load_1(object sender, EventArgs e)
		{
			ApplyLang();
			ApplySettings();
			if (appSettings.AutoUpdate)
				Updater.CheckSilent(this);
		}

		private void groupBox2_Enter(object sender, EventArgs e) { }
		private void groupBox3_Enter(object sender, EventArgs e) { }
		private void groupBox4_Enter(object sender, EventArgs e) { }
		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_applyingSettings) SaveBgColorSetting();
		}

		private void TrackBar_Scroll(object sender, EventArgs e)
		{
			if (!_applyingSettings) SaveToleranceSetting();
		}

		private void label1_Click(object sender, EventArgs e) { }

		// ── 撤回 / 重载 ──

		private void PushUndo()
		{
			if (currentImage == null) return;
			undoStack.Push((Bitmap)currentImage.Clone());

			while (undoStack.Count > MaxUndoSteps)
			{
				Bitmap old = undoStack.Pop();
				old.Dispose();
			}

			btnUndo.Enabled = true;
		}

		private void ClearUndo()
		{
			while (undoStack.Count > 0)
			{
				Bitmap old = undoStack.Pop();
				old.Dispose();
			}
			btnUndo.Enabled = false;
		}

		private void BtnUndo_Click(object sender, EventArgs e)
		{
			if (undoStack.Count == 0 || currentImage == null) return;

			currentImage.Dispose();
			currentImage = undoStack.Pop();

			pictureBox1.Image = currentImage;
			pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
			btnUndo.Enabled = undoStack.Count > 0;

			lblInfo.Text = Lang.Get("msg.undoDone");
		}

		private void BtnReload_Click(object sender, EventArgs e)
		{
			if (sourceImage == null) return;

			ClearUndo();

			currentImage?.Dispose();
			currentImage = (Bitmap)sourceImage.Clone();
			pictureBox1.Image = currentImage;
			pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

			ClearPublishFiles();
			ExportStage("原始图片", currentImage);
			lblInfo.Text = Lang.Get("msg.reloadDone");
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			ClearUndo();
			base.OnFormClosing(e);
		}

		// ── 各阶段图片导出 ──

		private static string PublishDir => Path.Combine(
			Path.GetDirectoryName(Application.ExecutablePath)!, "publish");

		private void EnsurePublishDir()
		{
			if (!Directory.Exists(PublishDir))
				Directory.CreateDirectory(PublishDir);
		}

		private void ClearPublishFiles()
		{
			if (!Directory.Exists(PublishDir)) return;
			foreach (string f in Directory.GetFiles(PublishDir, "*.jpg"))
				File.Delete(f);
		}

		private void ExportStage(string name, Bitmap image)
		{
			if (image == null) return;
			if (!appSettings.Privacy.AllowExternalAccess) return;
			EnsurePublishDir();
			string path = Path.Combine(PublishDir, $"{name}.jpg");
			Assalg.SaveImage(image, path);
		}
	}
}
