using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace fptp
{
	/// <summary>
	/// 图像预处理算法。
	/// 包含智能居中裁剪、灰度转换、色键替换背景。
	/// </summary>
	public static class Prepalg
	{
		/// <summary>
		/// 智能居中裁剪并缩放到目标尺寸。
		/// 以图像中心为基准，裁掉多余边缘后等比缩放。
		/// </summary>
		/// <param name="source">原图</param>
		/// <param name="targetW">目标宽度（像素）</param>
		/// <param name="targetH">目标高度（像素）</param>
		/// <returns>裁剪缩放后的图片</returns>
		public static Bitmap SmartCrop(Bitmap source, int targetW, int targetH)
		{
			if (source == null) return null;

			double srcRatio = (double)source.Width / source.Height;
			double dstRatio = (double)targetW / targetH;

			int cropX, cropY, cropW, cropH;

			// 比较宽高比，决定裁左右还是裁上下
			if (srcRatio > dstRatio)
			{
				cropH = source.Height;
				cropW = (int)(source.Height * dstRatio);
				cropX = (source.Width - cropW) / 2;
				cropY = 0;
			}
			else
			{
				cropW = source.Width;
				cropH = (int)(source.Width / dstRatio);
				cropX = 0;
				cropY = (source.Height - cropH) / 2;
			}

			Bitmap result = new Bitmap(targetW, targetH);
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

		/// <summary>
		/// 将彩色图片转为灰度（黑白）照。
		/// 使用 ColorMatrix 实现，性能优于逐像素操作。
		/// </summary>
		/// <param name="source">原图</param>
		/// <returns>灰度图</returns>
		public static Bitmap ToGrayscale(Bitmap source)
		{
			Bitmap bmp = new Bitmap(source.Width, source.Height);

			using (Graphics g = Graphics.FromImage(bmp))
			{
				float[][] matrixItems = {
					new float[] {0.299f, 0.299f, 0.299f, 0, 0},
					new float[] {0.587f, 0.587f, 0.587f, 0, 0},
					new float[] {0.114f, 0.114f, 0.114f, 0, 0},
					new float[] {0,      0,      0,      1, 0},
					new float[] {0,      0,      0,      0, 1}
				};

				ColorMatrix colorMatrix = new ColorMatrix(matrixItems);
				using (ImageAttributes attributes = new ImageAttributes())
				{
					attributes.SetColorMatrix(colorMatrix);
					g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height),
								0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
				}
			}

			return bmp;
		}

		/// <summary>
		/// 替换图片背景色（色键算法）。
		/// 以左上角像素颜色为基准，容差范围内的像素替换为目标颜色。
		/// </summary>
		/// <param name="source">原图</param>
		/// <param name="newColor">新背景色</param>
		/// <param name="tolerance">颜色容差 0-150，越大替换越激进</param>
		/// <param name="parent">父窗体，传入后可刷新界面防止假死</param>
		/// <returns>处理后的图片</returns>
		public static Bitmap ReplaceBackground(Bitmap source, Color newColor, int tolerance, System.Windows.Forms.Form parent = null)
		{
			Color sampleColor = source.GetPixel(0, 0);

			Bitmap bmp = new Bitmap(source.Width, source.Height);
			int processedRows = 0;

			for (int y = 0; y < source.Height; y++)
			{
				for (int x = 0; x < source.Width; x++)
				{
					Color pixelColor = source.GetPixel(x, y);

					int diff = Math.Abs(pixelColor.R - sampleColor.R) +
							   Math.Abs(pixelColor.G - sampleColor.G) +
							   Math.Abs(pixelColor.B - sampleColor.B);

					if (diff < tolerance)
					{
						bmp.SetPixel(x, y, newColor);
					}
					else
					{
						bmp.SetPixel(x, y, pixelColor);
					}
				}

				processedRows++;
				if (parent != null && processedRows % 100 == 0)
				{
					System.Windows.Forms.Application.DoEvents();
				}
			}

			return bmp;
		}
	}
}
