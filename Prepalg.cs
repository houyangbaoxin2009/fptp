using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace fptp
{
	/// <summary>
	/// 预处理算法类
	/// 包含：智能裁剪、变色(黑白)、换背景
	/// </summary>
	public static class Prepalg
	{
		#region 1. 智能裁剪 (通用尺寸)

		/// <summary>
		/// 智能居中裁剪并缩放到指定尺寸
		/// </summary>
		/// <param name="source">原图</param>
		/// <param name="targetW">目标宽度</param>
		/// <param name="targetH">目标高度</param>
		/// <returns>处理后的图片</returns>
		public static Bitmap SmartCrop(Bitmap source, int targetW, int targetH)
		{
			if (source == null) return null;

			// 1. 计算宽高比
			double srcRatio = (double)source.Width / source.Height;
			double dstRatio = (double)targetW / targetH;

			int cropX, cropY, cropW, cropH;

			// 2. 决定裁剪方式（以中心为基准）
			if (srcRatio > dstRatio)
			{
				// 源图太宽，裁掉左右
				cropH = source.Height;
				cropW = (int)(source.Height * dstRatio);
				cropX = (source.Width - cropW) / 2;
				cropY = 0;
			}
			else
			{
				// 源图太高，裁掉上下
				cropW = source.Width;
				cropH = (int)(source.Width / dstRatio);
				cropX = 0;
				cropY = (source.Height - cropH) / 2;
			}

			// 3. 创建目标位图
			Bitmap result = new Bitmap(targetW, targetH);

			// 4. 高质量绘制
			using (Graphics g = Graphics.FromImage(result))
			{
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.SmoothingMode = SmoothingMode.HighQuality;
				g.PixelOffsetMode = PixelOffsetMode.HighQuality;
				g.CompositingQuality = CompositingQuality.HighQuality;

				g.DrawImage(source,
							new Rectangle(0, 0, targetW, targetH),
							new Rectangle(cropX, cropY, cropW, cropH),
							GraphicsUnit.Pixel);
			}

			return result;
		}

		#endregion

		#region 2. 变色 (黑白/灰度)

		/// <summary>
		/// 将图片转换为黑白（灰度）照
		/// </summary>
		public static Bitmap ToGrayscale(Bitmap source)
		{
			Bitmap bmp = new Bitmap(source.Width, source.Height);

			// 使用颜色矩阵进行灰度转换（比像素循环更快）
			Graphics g = Graphics.FromImage(bmp);

			// 灰度矩阵公式
			float[][] matrixItems = {
				new float[] {0.299f, 0.299f, 0.299f, 0, 0},
				new float[] {0.587f, 0.587f, 0.587f, 0, 0},
				new float[] {0.114f, 0.114f, 0.114f, 0, 0},
				new float[] {0,      0,      0,      1, 0},
				new float[] {0,      0,      0,      0, 1}
			};

			ColorMatrix colorMatrix = new ColorMatrix(matrixItems);
			ImageAttributes attributes = new ImageAttributes();
			attributes.SetColorMatrix(colorMatrix);

			g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height),
						0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);

			g.Dispose();
			return bmp;
		}

		#endregion

		#region 3. 换背景 (色键算法)

		/// <summary>
		/// 替换图片背景色
		/// </summary>
		/// <param name="source">原图</param>
		/// <param name="newColor">新背景色</param>
		/// <param name="tolerance">颜色容差 (0-150)</param>
		/// <param name="parent">父窗体，用于刷新界面防止卡死</param>
		public static Bitmap ReplaceBackground(Bitmap source, Color newColor, int tolerance, System.Windows.Forms.Form parent = null)
		{
			// 采样点：左上角 (0,0)
			Color sampleColor = source.GetPixel(0, 0);

			Bitmap bmp = new Bitmap(source.Width, source.Height);
			int processedRows = 0;

			for (int y = 0; y < source.Height; y++)
			{
				for (int x = 0; x < source.Width; x++)
				{
					Color pixelColor = source.GetPixel(x, y);

					// 计算颜色差异
					int diff = Math.Abs(pixelColor.R - sampleColor.R) +
							   Math.Abs(pixelColor.G - sampleColor.G) +
							   Math.Abs(pixelColor.B - sampleColor.B);

					// 差异小于容差，则替换
					if (diff < tolerance)
					{
						bmp.SetPixel(x, y, newColor);
					}
					else
					{
						bmp.SetPixel(x, y, pixelColor);
					}
				}

				// 每100行刷新一次界面，防止假死
				processedRows++;
				if (parent != null && processedRows % 100 == 0)
				{
					System.Windows.Forms.Application.DoEvents();
				}
			}

			return bmp;
		}

		#endregion
	}
}
