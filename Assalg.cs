
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace fptp
{
	/// <summary>
	/// 辅助算法类
	/// 包含：高质量保存、颜色计算、分辨率检查
	/// </summary>
	public static class Assalg
	{
		#region 1. 高质量保存图片

		/// <summary>
		/// 以高质量保存图片到指定路径
		/// 自动根据扩展名选择格式，JPEG 设置为最高质量
		/// </summary>
		public static void SaveImage(Bitmap bmp, string filePath)
		{
			if (bmp == null) return;

			string ext = Path.GetExtension(filePath).ToLower();

			// 创建编码信息
			ImageCodecInfo codecInfo = GetEncoderInfo(ext);
			if (codecInfo == null)
			{
				// 如果没有特定编码器（比如bmp），直接保存
				bmp.Save(filePath);
				return;
			}

			// 设置高质量编码参数
			EncoderParameters encoderParams = new EncoderParameters(1);
			encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);

			bmp.Save(filePath, codecInfo, encoderParams);
		}

		// 获取图像编码信息的私有辅助方法
		private static ImageCodecInfo GetEncoderInfo(string extension)
		{
			ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
			foreach (ImageCodecInfo codec in codecs)
			{
				if (codec.FilenameExtension.ToLower().Contains(extension))
					return codec;
			}
			return null;
		}

		#endregion

		#region 2. 颜色差异计算 (辅助换底色)

		/// <summary>
		/// 计算两个颜色的差异值 (曼哈顿距离)
		/// </summary>
		/// <returns>差异值，越小越接近</returns>
		public static int GetColorDifference(Color c1, Color c2)
		{
			return Math.Abs(c1.R - c2.R) +
				   Math.Abs(c1.G - c2.G) +
				   Math.Abs(c1.B - c2.B);
		}

		#endregion

		#region 3. 分辨率检查

		/// <summary>
		/// 检查图片分辨率是否满足最低要求
		/// </summary>
		/// <returns>满足返回 true，否则返回 false</returns>
		public static bool CheckResolution(Bitmap source, int minWidth, int minHeight)
		{
			if (source == null) return false;
			return (source.Width >= minWidth && source.Height >= minHeight);
		}

		#endregion
	}
}