using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace fptp
{
	public partial class mainBox : Form
	{
		private Bitmap sourceImage;
		private Bitmap currentImage;
		private GenSettings settings;
		private bool _applyingSettings;

		public mainBox()
		{
			InitializeComponent();
			settings = Assalg.LoadGenSettings();
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
		}

		private void BtnBlackWhite_Click(object sender, EventArgs e)
		{
			if (!Basic.CheckImage(currentImage, this)) return;

			this.Cursor = Cursors.WaitCursor;

			Bitmap bwImage = Prepalg.ToGrayscale(currentImage);

			currentImage.Dispose();
			currentImage = bwImage;
			pictureBox1.Image = currentImage;

			this.Cursor = Cursors.Default;
			lblInfo.Text = "已转换为黑白照。";
		}

		private void BtnChangeBg_Click(object sender, EventArgs e)
		{
			if (!Basic.CheckImage(currentImage, this)) return;

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
		}

		private void BtnLayout5_Click(object sender, EventArgs e)
		{
			if (!Basic.CheckImage(currentImage, this)) return;

			lblInfo.Text = "正在生成排版...";

			int gap = 40;
			int cols = 4;
			int rows = 2;

			int canvasWidth = (Basic.ONE_INCH_W * cols) + (gap * (cols + 1));
			int canvasHeight = (Basic.ONE_INCH_H * rows) + (gap * (rows + 1));

			Bitmap layoutPaper = new Bitmap(canvasWidth, canvasHeight);
			using (Bitmap oneInchPhoto = Prepalg.SmartCrop(currentImage, Basic.ONE_INCH_W, Basic.ONE_INCH_H))
			using (Graphics g = Graphics.FromImage(layoutPaper))
			{
				g.Clear(Color.White);
				g.SmoothingMode = SmoothingMode.HighQuality;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;

				for (int r = 0; r < rows; r++)
				{
					for (int c = 0; c < cols; c++)
					{
						int x = gap + c * (Basic.ONE_INCH_W + gap);
						int y = gap + r * (Basic.ONE_INCH_H + gap);

						g.DrawImage(oneInchPhoto, x, y, Basic.ONE_INCH_W, Basic.ONE_INCH_H);

						using (Pen pen = new Pen(Color.LightGray, 1))
						{
							pen.DashStyle = DashStyle.Dash;
							g.DrawRectangle(pen, x, y, Basic.ONE_INCH_W, Basic.ONE_INCH_H);
						}
					}
				}
			}

			if (pictureBox1.Image != currentImage)
				pictureBox1.Image?.Dispose();
			pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
			pictureBox1.Image = layoutPaper;

			lblInfo.Text = "排版完成 (4x2)。请点击保存。";
		}

		private void BtnLayout6_Click(object sender, EventArgs e)
		{
			if (!Basic.CheckImage(currentImage, this)) return;

			lblInfo.Text = "正在生成6寸排版...";

			int paperWidth = 1800;
			int paperHeight = 1200;

			Bitmap layoutPaper = new Bitmap(paperWidth, paperHeight);

			using (Bitmap oneInchPhoto = Prepalg.SmartCrop(currentImage, Basic.ONE_INCH_W, Basic.ONE_INCH_H))
			using (Graphics g = Graphics.FromImage(layoutPaper))
			{
				g.Clear(Color.White);
				g.SmoothingMode = SmoothingMode.HighQuality;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;

				int cols = 5;
				int rows = 2;
				int gap = 50;

				int contentWidth = cols * Basic.ONE_INCH_W + (cols - 1) * gap;
				int contentHeight = rows * Basic.ONE_INCH_H + (rows - 1) * gap;

				int startX = (paperWidth - contentWidth) / 2;
				int startY = (paperHeight - contentHeight) / 2;

				for (int r = 0; r < rows; r++)
				{
					for (int c = 0; c < cols; c++)
					{
						int x = startX + c * (Basic.ONE_INCH_W + gap);
						int y = startY + r * (Basic.ONE_INCH_H + gap);

						g.DrawImage(oneInchPhoto, x, y, Basic.ONE_INCH_W, Basic.ONE_INCH_H);

						using (Pen pen = new Pen(Color.LightGray, 1))
						{
							pen.DashStyle = DashStyle.Dash;
							g.DrawRectangle(pen, x, y, Basic.ONE_INCH_W, Basic.ONE_INCH_H);
						}
					}
				}
			}

			if (pictureBox1.Image != currentImage)
				pictureBox1.Image?.Dispose();
			pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
			pictureBox1.Image = layoutPaper;

			lblInfo.Text = "6寸排版完成 (5列x2行，共10张)。请点击保存。";
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
	}
}
