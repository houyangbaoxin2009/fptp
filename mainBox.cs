using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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
			settings = Assalg.LoadGenSettings();
			appSettings = Assalg.LoadAppSettings();
		}

		private void ApplySettings()
		{
			_applyingSettings = true;
			cmbBgColor.Text = settings.BackgroundColor;
			TrackBar.Value = settings.Tolerance;
			_applyingSettings = false;
		}

		private void SaveBgColorSetting()
		{
			if (cmbBgColor.SelectedItem != null)
			{
				settings.BackgroundColor = cmbBgColor.SelectedItem.ToString();
				Assalg.SaveGenSettings(settings);
			}
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
						MessageBox.Show("图片分辨率过低（小于300像素），无法生成清晰的照片，请更换图片。",
										"图片不合格", MessageBoxButtons.OK, MessageBoxIcon.Error);
						currentImage.Dispose();
						sourceImage.Dispose();
						currentImage = null;
						sourceImage = null;
						return;
					}
					else if (minSide < 600)
					{
						MessageBox.Show("图片分辨率较低，打印效果可能不够理想，建议使用更高清的照片。",
										"提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
					else
					{
						lblInfo.Text = $"图片已加载 (尺寸: {currentImage.Width}x{currentImage.Height})，质量良好。";
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
					MessageBox.Show("加载失败: " + ex.Message);
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

			lblInfo.Text = "正在智能裁剪...";
			Application.DoEvents();

			Bitmap croppedImage = Prepalg.SmartCrop(currentImage, targetW, targetH);

			currentImage.Dispose();
			currentImage = croppedImage;

			pictureBox1.Image = currentImage;
			pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

			string sizeName = settings.DefaultSize switch { 2 => "二寸", 3 => "小二寸", _ => "一寸" };
			lblInfo.Text = $"已裁剪为{sizeName}照 ({targetW}x{targetH})。";

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
			lblInfo.Text = "已转换为黑白照。";

			ExportStage("黑白", currentImage);
		}

		private void BtnChangeBg_Click(object sender, EventArgs e)
		{
			if (!Basic.CheckImage(currentImage, this)) return;

			PushUndo();
			settings = Assalg.LoadGenSettings();

			Color targetColor = Color.White;
			if (cmbBgColor.SelectedItem != null)
			{
				switch (cmbBgColor.SelectedItem.ToString())
				{
					case "蓝色": targetColor = Color.FromArgb(65, 105, 225); break;
					case "红色": targetColor = Color.FromArgb(220, 20, 60); break;
					case "白色": targetColor = Color.White; break;
				}
			}

			lblInfo.Text = "正在处理底色，请稍候...";
			Application.DoEvents();
			this.Cursor = Cursors.WaitCursor;

			int tolerance = TrackBar.Value;
			Bitmap newImage = Prepalg.ReplaceBackground(currentImage, targetColor, tolerance, this);

			currentImage.Dispose();
			currentImage = newImage;
			pictureBox1.Image = currentImage;

			this.Cursor = Cursors.Default;
			lblInfo.Text = "底色修改完成。";

			ExportStage("换底色", currentImage);
		}

		private void BtnLayout5_Click(object sender, EventArgs e)
		{
			if (!Basic.CheckImage(currentImage, this)) return;

			lblInfo.Text = "正在生成5寸排版...";

			int photoW = currentImage.Width;
			int photoH = currentImage.Height;
			int paperWidth = 1500;
			int paperHeight = 1050;
			int gap = 40;

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

						using (Pen pen = new Pen(Color.LightGray, 1))
						{
							pen.DashStyle = DashStyle.Dash;
							g.DrawRectangle(pen, x, y, photoW, photoH);
						}
					}
				}
			}

			if (pictureBox1.Image != currentImage)
				pictureBox1.Image?.Dispose();
			pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
			pictureBox1.Image = layoutPaper;

			lblInfo.Text = $"5寸排版完成 ({cols}列x{rows}行，共{cols * rows}张)。请点击保存。";

			ExportStage("排版", layoutPaper);
		}

		private void BtnLayout6_Click(object sender, EventArgs e)
		{
			if (!Basic.CheckImage(currentImage, this)) return;

			lblInfo.Text = "正在生成6寸排版...";

			int photoW = currentImage.Width;
			int photoH = currentImage.Height;
			int paperWidth = 1800;
			int paperHeight = 1200;
			int gap = 50;

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

						using (Pen pen = new Pen(Color.LightGray, 1))
						{
							pen.DashStyle = DashStyle.Dash;
							g.DrawRectangle(pen, x, y, photoW, photoH);
						}
					}
				}
			}

			if (pictureBox1.Image != currentImage)
				pictureBox1.Image?.Dispose();
			pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
			pictureBox1.Image = layoutPaper;

			lblInfo.Text = $"6寸排版完成 ({cols}列x{rows}行，共{cols * rows}张)。请点击保存。";

			ExportStage("排版", layoutPaper);
		}

		private void BtnSave_Click(object sender, EventArgs e)
		{
			var toSave = (Bitmap)pictureBox1.Image;
			if (!Basic.CheckImage(toSave, this)) return;

			settings = Assalg.LoadGenSettings();

			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "JPEG 图片|*.jpg|PNG 图片|*.png";
				string ext = settings.SaveFormat == "png" ? "png" : "jpg";
				sfd.FileName = $"{Basic.AppName}_照片.{ext}";

				if (sfd.ShowDialog(this) == DialogResult.OK)
				{
					try
					{
						this.Cursor = Cursors.WaitCursor;

						Assalg.SaveImage(toSave, sfd.FileName);

						lblInfo.Text = "保存成功！";
						MessageBox.Show("图片已保存。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show("保存失败: " + ex.Message);
					}
					finally
					{
						this.Cursor = Cursors.Default;
					}
				}
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
					ApplySettings();
					lblInfo.Text = "设置已保存。";
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
			lblInfo.Text = "图片已卸载，请重新加载。";
		}

		private void Form1_Load_1(object sender, EventArgs e)
		{
			this.Text = Basic.GetAppTitle();
			ApplySettings();
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

			lblInfo.Text = "已撤回上一步操作。";
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
			lblInfo.Text = "已重新加载原始图片。";
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
