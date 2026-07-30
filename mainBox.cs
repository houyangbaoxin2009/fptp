using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace fptp
{
	public partial class mainBox : Form
	{
		private Bitmap sourceImage; // 原始加载的图片
		private Bitmap currentImage; // 当前处理中的图片（可能已被黑白处理）

		public mainBox()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{

			this.Text = Basic.GetAppTitle();
		}

		private void BtnLoad_Click(object sender, EventArgs e)
		{
			string filePath = Basic.OpenImageFile(this);

			if (!string.IsNullOrEmpty(filePath))
			{
				try
				{
					sourceImage = new Bitmap(filePath);
					currentImage = (Bitmap)sourceImage.Clone();

					// 获取短边长度
					int minSide = Math.Min(sourceImage.Width, sourceImage.Height);

					// 分级检查
					if (minSide < 300)
					{
						MessageBox.Show("图片分辨率过低（小于300像素），无法生成清晰的照片，请更换图片。",
										"图片不合格", MessageBoxButtons.OK, MessageBoxIcon.Error);
						currentImage.Dispose();
						sourceImage.Dispose();
						currentImage = null;
						sourceImage = null;
						return; // 直接退出，不加载
					}
					else if (minSide < 600)
					{
						MessageBox.Show("图片分辨率较低，打印效果可能不够理想，建议使用更高清的照片。",
										"提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						// 继续加载，让用户自己决定
					}
					else
					{
						// 分辨率合格，不提示或提示“图片质量良好”
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

			lblInfo.Text = "正在智能裁剪...";
			Application.DoEvents();

			// 调用 Prepalg 中的智能裁剪方法
			Bitmap croppedImage = Prepalg.SmartCrop(currentImage, Basic.ONE_INCH_W, Basic.ONE_INCH_H);

			// 更新当前图片
			currentImage.Dispose(); // 释放旧图
			currentImage = croppedImage;

			// 更新显示
			pictureBox1.Image = currentImage;
			pictureBox1.SizeMode = PictureBoxSizeMode.Zoom; // 裁剪后建议用 Zoom 查看细节

			lblInfo.Text = "已裁剪为一寸照 (295x413)。";
		}

		// 2. 变黑白
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

			// 获取目标颜色
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

			// 调用 Prepalg.ReplaceBackground
			// 传入当前图片、目标颜色、容差(从滑动条获取)、当前窗体(用于防卡顿)
			int tolerance = TrackBar.Value;
			Bitmap newImage = Prepalg.ReplaceBackground(currentImage, targetColor, tolerance, this);

			// 更新界面
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

			// 判断用户是否裁剪
			Bitmap oneInchPhoto = Prepalg.SmartCrop(currentImage, Basic.ONE_INCH_W, Basic.ONE_INCH_H);

			// 创建相纸画布
			int gap = 40;
			int cols = 4;
			int rows = 2;

			int canvasWidth = (Basic.ONE_INCH_W * cols) + (gap * (cols + 1));
			int canvasHeight = (Basic.ONE_INCH_H * rows) + (gap * (rows + 1));

			Bitmap layoutPaper = new Bitmap(canvasWidth, canvasHeight);
			using (Graphics g = Graphics.FromImage(layoutPaper))
			{
				g.Clear(Color.White); // 背景设为白色
				g.SmoothingMode = SmoothingMode.HighQuality;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;

				// 循环绘制8张照片
				for (int r = 0; r < rows; r++)
				{
					for (int c = 0; c < cols; c++)
					{
						int x = gap + c * (Basic.ONE_INCH_W + gap);
						int y = gap + r * (Basic.ONE_INCH_H + gap);

						g.DrawImage(oneInchPhoto, x, y, Basic.ONE_INCH_W, Basic.ONE_INCH_H);

						// 画虚线裁剪线
						using (Pen pen = new Pen(Color.LightGray, 1))
						{
							pen.DashStyle = DashStyle.Dash;
							g.DrawRectangle(pen, x, y, Basic.ONE_INCH_W, Basic.ONE_INCH_H);
						}
					}
				}
			}

			// 显示排版结果
			pictureBox1.SizeMode = PictureBoxSizeMode.Zoom; // 缩放以适应预览框
			pictureBox1.Image = layoutPaper;

			// 暂存排版结果以便保存（注意：这里简单起见直接把排版图挂在PictureBox上，
			// 真实保存时直接保存PictureBox的内容即可）

			lblInfo.Text = "排版完成 (4x2)。请点击保存。";
		}

		// 生成6寸排版
		private void BtnLayout6_Click(object sender, EventArgs e)
		{
			if (!Basic.CheckImage(currentImage, this)) return;

			lblInfo.Text = "正在生成6寸排版...";

			// 判断用户是否裁剪
			Bitmap oneInchPhoto = Prepalg.SmartCrop(currentImage, Basic.ONE_INCH_W, Basic.ONE_INCH_H);

			// 创建六寸相纸画布
			int paperWidth = 1800;
			int paperHeight = 1200;

			Bitmap layoutPaper = new Bitmap(paperWidth, paperHeight);

			using (Graphics g = Graphics.FromImage(layoutPaper))
			{
				g.Clear(Color.White); // 背景设为白色
				g.SmoothingMode = SmoothingMode.HighQuality;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;

				// 定义布局参数
				int cols = 5; // 列数
				int rows = 2; // 行数
				int gap = 50; // 照片之间的间距 (像素)

				// 计算整体内容区域的宽度和高度
				int contentWidth = cols * Basic.ONE_INCH_W + (cols - 1) * gap;
				int contentHeight = rows * Basic.ONE_INCH_H + (rows - 1) * gap;

				// 计算起始坐标，使整体在相纸中居中
				int startX = (paperWidth - contentWidth) / 2;
				int startY = (paperHeight - contentHeight) / 2;

				// 循环绘制10张照片
				for (int r = 0; r < rows; r++)
				{
					for (int c = 0; c < cols; c++)
					{
						// 计算每张照片的绘制坐标
						int x = startX + c * (Basic.ONE_INCH_W + gap);
						int y = startY + r * (Basic.ONE_INCH_H + gap);

						// 绘制照片
						g.DrawImage(oneInchPhoto, x, y, Basic.ONE_INCH_W, Basic.ONE_INCH_H);

						// 绘制裁剪辅助线 (灰色虚线)
						using (Pen pen = new Pen(Color.LightGray, 1))
						{
							pen.DashStyle = DashStyle.Dash;
							g.DrawRectangle(pen, x, y, Basic.ONE_INCH_W, Basic.ONE_INCH_H);
						}
					}
				}
			}

			// 显示排版结果
			pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
			pictureBox1.Image = layoutPaper;

			lblInfo.Text = "6寸排版完成 (5列x2行，共10张)。请点击保存。";
		}

		private void BtnSave_Click(object sender, EventArgs e)
		{
			if (!Basic.CheckImage(currentImage, this)) return;

			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "JPEG 图片|*.jpg|PNG 图片|*.png";
				sfd.FileName = $"{Basic.AppName}_照片.jpg";

				if (sfd.ShowDialog(this) == DialogResult.OK)
				{
					try
					{
						this.Cursor = Cursors.WaitCursor;

						// 【修改点】使用 Assalg 中的高质量保存方法
						Assalg.SaveImage(currentImage, sfd.FileName);

						this.Cursor = Cursors.Default;
						lblInfo.Text = "保存成功！";
						MessageBox.Show("图片已保存。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show("保存失败: " + ex.Message);
					}
				}
			}
		}


		private void BtnAbout_Click(object sender, EventArgs e)
		{
			// 创建并显示关于框
			using (AboutBox about = new AboutBox())
			{
				about.ShowDialog(this); // this 表示主窗体是父窗体
			}
		}

		private void BtnUnload_Click(object sender, EventArgs e)
		{
			// 释放图片资源
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

			// 清空 PictureBox 的显示
			pictureBox1.Image = null;

			// 重置状态提示
			lblInfo.Text = "图片已卸载，请重新加载。";
		}

		private void Form1_Load_1(object sender, EventArgs e)
		{
			this.Text = Basic.GetAppTitle();
		}

		private void groupBox2_Enter(object sender, EventArgs e)
		{

		}

		private void groupBox3_Enter(object sender, EventArgs e)
		{

		}

		private void groupBox4_Enter(object sender, EventArgs e)
		{

		}

		private void btnAbout_Click_1(object sender, EventArgs e)
		{

		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
		}

		private void label1_Click(object sender, EventArgs e)
		{

		}
	}
}
