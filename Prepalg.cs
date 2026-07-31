using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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
		/// 替换图片背景色（色键算法 + 边缘羽化）。
		/// 以左上角像素颜色为基准，容差范围内的像素替换为目标颜色；
		/// 容差至容差+羽化带之间的过渡像素按距离线性混合，
		/// 消除头发边缘抗锯齿产生的白边。
		/// 新背景色为透明（Alpha=0）时输出透明背景（需 PNG 保存）。
		/// </summary>
		/// <param name="source">原图</param>
		/// <param name="newColor">新背景色（Alpha=0 表示透明）</param>
		/// <param name="tolerance">颜色容差 0-150，越大替换越激进</param>
		/// <param name="parent">父窗体，传入后可刷新界面防止假死</param>
		/// <returns>处理后的图片</returns>
		public static Bitmap ReplaceBackground(Bitmap source, Color newColor, int tolerance, System.Windows.Forms.Form parent = null)
		{
			int width = source.Width;
			int height = source.Height;

			// 读取全部像素到数组，便于快速访问
			int[] srcPixels = new int[width * height];
			BitmapData srcData = source.LockBits(new Rectangle(0, 0, width, height),
				ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			try
			{
				Marshal.Copy(srcData.Scan0, srcPixels, 0, srcPixels.Length);
			}
			finally
			{
				source.UnlockBits(srcData);
			}

			Color sampleColor = source.GetPixel(0, 0);
			int sr = sampleColor.R, sg = sampleColor.G, sb = sampleColor.B;
			int newArgb = newColor.ToArgb();
			bool transparent = newColor.A == 0;
			const int feather = 30;

			int[] outPixels = new int[width * height];
			int processed = 0;

			for (int i = 0; i < outPixels.Length; i++)
			{
				int p = srcPixels[i];
				int diff = Math.Abs(((p >> 16) & 0xFF) - sr) +
						   Math.Abs(((p >> 8) & 0xFF) - sg) +
						   Math.Abs((p & 0xFF) - sb);

				if (diff < tolerance)
				{
					outPixels[i] = transparent ? 0 : newArgb;
				}
				else if (diff < tolerance + feather)
				{
					// 边缘羽化：按距离线性混合原色与新背景色
					double t = (double)(diff - tolerance) / feather;
					int pr = (p >> 16) & 0xFF, pg = (p >> 8) & 0xFF, pb = p & 0xFF;
					if (transparent)
					{
						// 透明目标：只衰减 alpha，保留原色（去白边）
						int a = (int)(255 * (1 - t));
						outPixels[i] = unchecked((a << 24) | (pr << 16) | (pg << 8) | pb);
					}
					else
					{
						int nr = (newArgb >> 16) & 0xFF, ng = (newArgb >> 8) & 0xFF, nb = newArgb & 0xFF;
						int r = (int)(pr + (nr - pr) * t);
						int g = (int)(pg + (ng - pg) * t);
						int b = (int)(pb + (nb - pb) * t);
						outPixels[i] = unchecked((int)0xFF000000 | (r << 16) | (g << 8) | b);
					}
				}
				else
				{
					outPixels[i] = p;
				}

				processed++;
				if (parent != null && processed % 20000 == 0)
					System.Windows.Forms.Application.DoEvents();
			}

			// 写回结果
			Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
			BitmapData outData = result.LockBits(new Rectangle(0, 0, width, height),
				ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			try
			{
				Marshal.Copy(outPixels, 0, outData.Scan0, outPixels.Length);
			}
			finally
			{
				result.UnlockBits(outData);
			}

			return result;
		}

		/// <summary>
		/// 替换图片背景色（动画模式，连通域洪泛填充）。
		/// 以图像四角区域与采样色接近的像素为种子，向图像内部洪泛填充，
		/// 仅替换与背景连通的区域。三层防护避免误吞主体：
		/// 1) 种子仅取自四角，衣服等主体色块若未接触四角不会被播种；
		/// 2) 梯度屏障：扩散遇到颜色突变（如描边线）即停止；
		/// 3) 面积兜底：背景占比异常高时自动降低容差重试。
		/// 动画角色眼白、衣服等被轮廓包围的相似色区域不会被误替换。
		/// </summary>
		/// <param name="source">原图</param>
		/// <param name="newColor">新背景色</param>
		/// <param name="tolerance">颜色容差 0-150，越大替换越激进</param>
		/// <param name="parent">父窗体，传入后可刷新界面防止假死</param>
		/// <returns>处理后的图片</returns>
		public static Bitmap ReplaceBackgroundAnime(Bitmap source, Color newColor, int tolerance, System.Windows.Forms.Form parent = null)
		{
			int width = source.Width;
			int height = source.Height;

			// 读取全部像素到数组，便于快速访问
			int[] srcPixels = new int[width * height];
			BitmapData srcData = source.LockBits(new Rectangle(0, 0, width, height),
				ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			try
			{
				Marshal.Copy(srcData.Scan0, srcPixels, 0, srcPixels.Length);
			}
			finally
			{
				source.UnlockBits(srcData);
			}

			// 背景种子色：取四角中最接近的颜色（防止单角异常像素）
			Color corner0 = source.GetPixel(0, 0);
			Color corner1 = source.GetPixel(width - 1, 0);
			Color corner2 = source.GetPixel(0, height - 1);
			Color corner3 = source.GetPixel(width - 1, height - 1);
			Color sampleColor = MostCommonCorner(corner0, corner1, corner2, corner3);

			// 洪泛填充得到背景标记
			byte[] mark = FloodFillBackground(srcPixels, width, height, sampleColor, tolerance, parent);

			// 面积兜底：背景占比异常高（>90%）说明可能误吞主体，降容差重试
			int bgCount = 0;
			for (int i = 0; i < mark.Length; i++)
				if (mark[i] == 1) bgCount++;
			if (bgCount > width * height * 0.90 && tolerance > 20)
			{
				int reduced = Math.Max(10, tolerance * 2 / 5);
				mark = FloodFillBackground(srcPixels, width, height, sampleColor, reduced, parent);
			}

			// 生成结果：背景像素替换为新色，其余保留原色；
			// 贴着背景的前景像素做边缘羽化，消除抗锯齿白边
			Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
			BitmapData outData = result.LockBits(new Rectangle(0, 0, width, height),
				ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			try
			{
				int newArgb = newColor.ToArgb();
				bool transparent = newColor.A == 0;
				int[] outPixels = new int[width * height];
				const int feather = 30;
				int sr2 = sampleColor.R, sg2 = sampleColor.G, sb2 = sampleColor.B;

				for (int i = 0; i < outPixels.Length; i++)
				{
					if (mark[i] == 1)
					{
						outPixels[i] = transparent ? 0 : newArgb;
						continue;
					}

					// 是否贴着背景（四邻域存在背景像素）→ 是则进入羽化候选
					int x = i % width;
					int y = i / width;
					bool adjacentBg = (x > 0 && mark[i - 1] == 1) ||
									   (x < width - 1 && mark[i + 1] == 1) ||
									   (y > 0 && mark[i - width] == 1) ||
									   (y < height - 1 && mark[i + width] == 1);

					int p = srcPixels[i];
					if (!adjacentBg)
					{
						outPixels[i] = p;
						continue;
					}

					// 边缘羽化：按与采样色的距离线性混合
					int diff = Math.Abs(((p >> 16) & 0xFF) - sr2) +
							   Math.Abs(((p >> 8) & 0xFF) - sg2) +
							   Math.Abs((p & 0xFF) - sb2);
					if (diff < tolerance + feather)
					{
						double t = (double)Math.Max(0, diff - tolerance) / feather;
						int pr = (p >> 16) & 0xFF, pg = (p >> 8) & 0xFF, pb = p & 0xFF;
						if (transparent)
						{
							// 透明目标：只衰减 alpha，保留原色（去白边）
							int a = (int)(255 * (1 - t));
							outPixels[i] = unchecked((a << 24) | (pr << 16) | (pg << 8) | pb);
						}
						else
						{
							int nr = (newArgb >> 16) & 0xFF, ng = (newArgb >> 8) & 0xFF, nb = newArgb & 0xFF;
							int r = (int)(pr + (nr - pr) * t);
							int g = (int)(pg + (ng - pg) * t);
							int b = (int)(pb + (nb - pb) * t);
							outPixels[i] = unchecked((int)0xFF000000 | (r << 16) | (g << 8) | b);
						}
					}
					else
					{
						outPixels[i] = p;
					}
				}
				Marshal.Copy(outPixels, 0, outData.Scan0, outPixels.Length);
			}
			finally
			{
				result.UnlockBits(outData);
			}

			return result;
		}

		/// <summary>
		/// 连通域洪泛填充：从四角种子区域向内部扩散，标记与背景连通的像素。
		/// 梯度屏障阈值 60（曼哈顿距离），跨过颜色突变（描边线）即停止。
		/// </summary>
		private static byte[] FloodFillBackground(int[] srcPixels, int width, int height,
			Color sampleColor, int tolerance, System.Windows.Forms.Form parent)
		{
			int sr = sampleColor.R, sg = sampleColor.G, sb = sampleColor.B;
			const int gradientBarrier = 60;

			byte[] mark = new byte[width * height];
			Queue<int> queue = new Queue<int>();

			// 种子区域：四角各取 min(w,h)/8 边长的正方形区域
			int margin = Math.Max(8, Math.Min(width, height) / 8);

			// 局部函数：入队与采样色接近且未标记的像素
			void TryEnqueue(int x, int y)
			{
				int idx = y * width + x;
				if (mark[idx] != 0) return;
				int p = srcPixels[idx];
				int diff = Math.Abs(((p >> 16) & 0xFF) - sr) +
						   Math.Abs(((p >> 8) & 0xFF) - sg) +
						   Math.Abs((p & 0xFF) - sb);
				if (diff < tolerance)
					queue.Enqueue(idx);
			}

			// 播种：四个角区域
			for (int y = 0; y < margin; y++)
			{
				for (int x = 0; x < margin; x++)
				{
					TryEnqueue(x, y);
					TryEnqueue(width - 1 - x, y);
					TryEnqueue(x, height - 1 - y);
					TryEnqueue(width - 1 - x, height - 1 - y);
				}
			}

			int processed = 0;
			while (queue.Count > 0)
			{
				int idx = queue.Dequeue();
				if (mark[idx] != 0) continue;
				mark[idx] = 1;

				int x = idx % width;
				int y = idx / width;
				int cur = srcPixels[idx];

				// 四连通扩展：邻居须同时满足
				//   a) 与采样色接近  b) 与当前像素差异小于梯度屏障（不跨描边线）
				if (x > 0) TryVisit(idx - 1, cur);
				if (x < width - 1) TryVisit(idx + 1, cur);
				if (y > 0) TryVisit(idx - width, cur);
				if (y < height - 1) TryVisit(idx + width, cur);

				processed++;
				if (parent != null && processed % 20000 == 0)
					System.Windows.Forms.Application.DoEvents();
			}

			return mark;

			// 局部函数：梯度屏障检查后入队
			void TryVisit(int nidx, int curColor)
			{
				if (mark[nidx] != 0) return;
				int p = srcPixels[nidx];
				int diffSample = Math.Abs(((p >> 16) & 0xFF) - sr) +
								 Math.Abs(((p >> 8) & 0xFF) - sg) +
								 Math.Abs((p & 0xFF) - sb);
				if (diffSample >= tolerance) return;

				// 梯度屏障：与当前像素颜色突变则视为边界（描边/轮廓线）
				int diffCur = Math.Abs(((p >> 16) & 0xFF) - ((curColor >> 16) & 0xFF)) +
							  Math.Abs(((p >> 8) & 0xFF) - ((curColor >> 8) & 0xFF)) +
							  Math.Abs((p & 0xFF) - (curColor & 0xFF));
				if (diffCur >= gradientBarrier) return;

				queue.Enqueue(nidx);
			}
		}

		/// <summary>
		/// 从四个角落颜色中选出最接近的一个作为背景采样色。
		/// 消除单角被主体占据导致的误采样。
		/// </summary>
		private static Color MostCommonCorner(Color c0, Color c1, Color c2, Color c3)
		{
			// 两两曼哈顿距离，取与其他三个总距离最小者为代表色
			Color[] corners = { c0, c1, c2, c3 };
			int best = 0;
			int bestSum = int.MaxValue;
			for (int i = 0; i < corners.Length; i++)
			{
				int sum = 0;
				for (int j = 0; j < corners.Length; j++)
				{
					if (i == j) continue;
					sum += Math.Abs(corners[i].R - corners[j].R) +
						   Math.Abs(corners[i].G - corners[j].G) +
						   Math.Abs(corners[i].B - corners[j].B);
				}
				if (sum < bestSum)
				{
					bestSum = sum;
					best = i;
				}
			}
			return corners[best];
		}
	}
}
