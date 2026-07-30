using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace fptp
{
	/// <summary>
	/// 辅助算法工具类。
	/// 包含高质量图片保存、颜色差异计算、分辨率检查。
	/// </summary>
	public static class Assalg
	{
		/// <summary>
		/// 以最高质量保存图片到指定路径。
		/// JPEG 格式自动设为 Quality=100，PNG/BMP 直接保存。
		/// </summary>
		/// <param name="bmp">要保存的图片</param>
		/// <param name="filePath">保存路径（根据扩展名判断格式）</param>
		public static void SaveImage(Bitmap bmp, string filePath)
		{
			if (bmp == null) return;

			string ext = Path.GetExtension(filePath).ToLower();

			ImageCodecInfo codecInfo = GetEncoderInfo(ext);
			if (codecInfo == null)
			{
				bmp.Save(filePath);
				return;
			}

			using (EncoderParameters encoderParams = new EncoderParameters(1))
			{
				encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
				bmp.Save(filePath, codecInfo, encoderParams);
			}
		}

		/// <summary>
		/// 根据文件扩展名查找对应的图像编码器。
		/// </summary>
		/// <param name="extension">文件扩展名（如 .jpg, .png）</param>
		/// <returns>编码器信息，未找到返回 null</returns>
		private static ImageCodecInfo GetEncoderInfo(string extension)
		{
			ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
			foreach (ImageCodecInfo codec in codecs)
			{
				if (codec.FilenameExtension.ToLower().Contains(extension))
					return codec;
			}
			return null;
		}

		/// <summary>
		/// 计算两个颜色的曼哈顿距离。
		/// 用于换底色算法中判断像素与采样色是否接近。
		/// </summary>
		/// <returns>差异值，越小颜色越接近</returns>
		public static int GetColorDifference(Color c1, Color c2)
		{
			return Math.Abs(c1.R - c2.R) +
				   Math.Abs(c1.G - c2.G) +
				   Math.Abs(c1.B - c2.B);
		}

		/// <summary>
		/// 检查图片分辨率是否达到最低要求。
		/// </summary>
		/// <returns>满足要求返回 true</returns>
		public static bool CheckResolution(Bitmap source, int minWidth, int minHeight)
		{
			if (source == null) return false;
			return (source.Width >= minWidth && source.Height >= minHeight);
		}
	}
}
