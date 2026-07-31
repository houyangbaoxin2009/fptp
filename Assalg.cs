using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace fptp
{
	/// <summary>
	/// 辅助算法工具类。
	/// 包含高质量图片保存、颜色差异计算、分辨率检查。
	/// </summary>
	public static class Assalg
	{
		/// <summary>
		/// 以指定质量保存图片到指定路径。
		/// 支持 JPEG/PNG/BMP/TIFF/GIF（根据扩展名判断格式）。
		/// JPEG 应用质量参数，其余格式忽略质量直接保存。
		/// </summary>
		/// <param name="bmp">要保存的图片</param>
		/// <param name="filePath">保存路径（根据扩展名判断格式）</param>
		/// <param name="quality">JPEG 质量（1-100，默认 100）</param>
		public static void SaveImage(Bitmap bmp, string filePath, int quality = 100)
		{
			if (bmp == null) return;

			string ext = Path.GetExtension(filePath).ToLower();

			ImageCodecInfo codecInfo = GetEncoderInfo(ext);
			if (codecInfo == null)
			{
				bmp.Save(filePath);
				return;
			}

			bool isJpeg = codecInfo.MimeType == "image/jpeg";
			if (isJpeg)
			{
				int q = Math.Max(1, Math.Min(100, quality));
				using (EncoderParameters encoderParams = new EncoderParameters(1))
				{
					encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, q);
					bmp.Save(filePath, codecInfo, encoderParams);
				}
			}
			else
			{
				bmp.Save(filePath, codecInfo, null);
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

		// ── 设置读写（统一文件 setting.json：{app}{gen}{lang}）──

		private static string SettingsFile => Path.Combine(
			Path.GetDirectoryName(Application.ExecutablePath), "setting.json");

		/// <summary>
		/// 读取完整设置包。文件不存在或损坏时返回默认包。
		/// 兼容旧格式（setting.json 为纯 app、gen_setting.json 为纯 gen）自动迁移。
		/// </summary>
		private static SettingsPackage LoadPackage()
		{
			try
			{
				if (File.Exists(SettingsFile))
				{
					string json = File.ReadAllText(SettingsFile);
					// 新格式：顶层含 app/gen/lang
					if (json.Contains("\"app\"") || json.Contains("\"gen\"") || json.Contains("\"lang\""))
						return JsonSerializer.Deserialize<SettingsPackage>(json) ?? new SettingsPackage();
					// 旧格式：整个文件是 AppSettings
					return MigrateLegacySettings();
				}
			}
			catch
			{
			}
			return MigrateLegacySettings();
		}

		/// <summary>
		/// 旧版两个独立文件（setting.json=app、gen_setting.json=gen）迁移到统一文件。
		/// </summary>
		private static SettingsPackage MigrateLegacySettings()
		{
			var pkg = new SettingsPackage();
			string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
			string legacyGen = Path.Combine(exeDir, "gen_setting.json");

			try
			{
				if (File.Exists(legacyGen))
					pkg.Gen = JsonSerializer.Deserialize<GenSettings>(File.ReadAllText(legacyGen)) ?? new GenSettings();
				if (File.Exists(SettingsFile))
					pkg.App = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsFile)) ?? new AppSettings();
			}
			catch
			{
			}

			SavePackage(pkg);
			return pkg;
		}

		/// <summary>将完整设置包写回统一文件。</summary>
		private static void SavePackage(SettingsPackage pkg)
		{
			try
			{
				string json = JsonSerializer.Serialize(pkg, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(SettingsFile, json);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"保存设置失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		public static AppSettings LoadAppSettings()
		{
			return LoadPackage().App;
		}

		public static void SaveAppSettings(AppSettings settings)
		{
			var pkg = LoadPackage();
			pkg.App = settings;
			SavePackage(pkg);
		}

		// ── 生成设置读写 ──

		public static GenSettings LoadGenSettings()
		{
			return LoadPackage().Gen;
		}

		public static void SaveGenSettings(GenSettings settings)
		{
			var pkg = LoadPackage();
			pkg.Gen = settings;
			SavePackage(pkg);
		}

		// ── 语言包读写 ──

		/// <summary>读取设置文件中的语言包，无则返回 null（调用方回退内置资源）。</summary>
		public static LangPackage? LoadLangPackage()
		{
			var pkg = LoadPackage();
			if (pkg.Lang != null && pkg.Lang.Ass != null && pkg.Lang.Ass.Count > 0)
				return pkg.Lang;
			return null;
		}

		/// <summary>将语言包写入设置文件。</summary>
		public static void SaveLangPackage(LangPackage lang)
		{
			var pkg = LoadPackage();
			pkg.Lang = lang;
			SavePackage(pkg);
		}
	}
}
