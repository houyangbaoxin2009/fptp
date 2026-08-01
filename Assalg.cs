using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
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

			// 无扩展名或未知扩展名：直接按默认 PNG 编码保存（避免空串命中第一个编码器误存 BMP）
			if (ext == "")
			{
				bmp.Save(filePath, ImageFormat.Png);
				return;
			}

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
		/// 检测图片是否含透明像素（Alpha < 255）。
		/// 用于判断是否必须以 PNG 等支持透明的格式保存。
		/// </summary>
		/// <param name="bmp">待检测图片</param>
		/// <returns>含透明像素返回 true</returns>
		public static bool HasAlpha(Bitmap bmp)
		{
			if (bmp == null) return false;
			if (bmp.PixelFormat != PixelFormat.Format32bppArgb && bmp.PixelFormat != PixelFormat.Format32bppPArgb)
				return false;

			int width = bmp.Width;
			int height = bmp.Height;
			int[] pixels = new int[width * height];
			BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height),
				ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			try
			{
				Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
			}
			finally
			{
				bmp.UnlockBits(data);
			}

			// 全量扫描 alpha 通道，避免抽样漏掉零星透明像素导致透明图被存成 JPG
			for (int i = 0; i < pixels.Length; i++)
			{
				if (((pixels[i] >> 24) & 0xFF) < 255)
					return true;
			}
			return false;
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

		/// <summary>
		/// 设置文件路径：优先 exe 目录（便携/可写场景），
		/// 不可写时（如安装到 Program Files 的普通用户）回退 %APPDATA%\FPTP。
		/// 首次访问时探测并缓存，运行期间路径不变。
		/// </summary>
		private static string? _settingsFile;

		private static string SettingsFile
		{
			get
			{
				if (_settingsFile != null) return _settingsFile;
				string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
				if (IsDirWritable(exeDir))
					_settingsFile = Path.Combine(exeDir, "setting.json");
				else
				{
					string appData = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FPTP");
					try { Directory.CreateDirectory(appData); } catch { }
					_settingsFile = Path.Combine(appData, "setting.json");
				}
				return _settingsFile;
			}
		}

		/// <summary>检测目录是否可写（尝试创建并删除探针文件）。</summary>
		private static bool IsDirWritable(string dir)
		{
			if (string.IsNullOrEmpty(dir)) return false;
			try
			{
				string probe = Path.Combine(dir, ".fptp_write_probe.tmp");
				File.WriteAllText(probe, "t");
				File.Delete(probe);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// 读取完整设置包。文件不存在或损坏时返回默认包。
		/// 兼容旧格式（setting.json 为纯 app、gen_setting.json 为纯 gen）自动迁移。
		/// </summary>
		private static SettingsPackage LoadPackage()		{
			try
			{
				if (File.Exists(SettingsFile))
				{
					string json = File.ReadAllText(SettingsFile);
					// 新格式：顶层含 app/gen/lang（用 JsonDocument 精确判断，避免子串误匹配）
					if (IsNewFormat(json))
					{
						var pkg = JsonSerializer.Deserialize<SettingsPackage>(json) ?? new SettingsPackage();
						SanitizePackage(pkg);
						return pkg;
					}
					// 旧格式：整个文件是 AppSettings
					return SanitizePackage(MigrateLegacySettings());
				}
			}
			catch
			{
			}
			return SanitizePackage(MigrateLegacySettings());
		}

		/// <summary>判断 JSON 是否为统一设置包格式（顶层含 app/gen 等对象 key）。</summary>
		private static bool IsNewFormat(string json)
		{
			try
			{
				using (var doc = JsonDocument.Parse(json))
				{
					if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;
					foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
					{
						if (prop.Name == "app" || prop.Name == "gen" || prop.Name == "lang")
							return true;
					}
				}
			}
			catch { }
			return false;
		}

		/// <summary>
		/// 校验并修正设置包中所有数值/枚举字段，防止手改 setting.json 写入非法值导致窗体加载崩溃。
		/// 非法的索引 clamp 到合法范围、null 回退默认值。
		/// </summary>
		private static SettingsPackage SanitizePackage(SettingsPackage pkg)
		{
			pkg.App ??= new AppSettings();
			pkg.Gen ??= new GenSettings();
			pkg.Key ??= new KeySettings();
			pkg.High ??= new HighSettings();
			pkg.Lang ??= new LangPackage();
			pkg.Theme ??= new ThemePackage();

			pkg.App.Privacy ??= new PrivacySettings();
			if (string.IsNullOrEmpty(pkg.App.Language)) pkg.App.Language = "zh-CN";
			if (string.IsNullOrEmpty(pkg.App.ThemeId)) pkg.App.ThemeId = "auto";
			if (pkg.App.TempImageMode != "memory" && pkg.App.TempImageMode != "disk")
				pkg.App.TempImageMode = "memory";

			pkg.Key.Actions ??= new Dictionary<string, string>();

			SanitizeGen(pkg.Gen);
			return pkg;
		}

		private static void SanitizeGen(GenSettings g)
		{
			if (string.IsNullOrEmpty(g.SaveFormat))
				g.SaveFormat = "jpg";
			else
				g.SaveFormat = g.SaveFormat.ToLowerInvariant();
			g.SaveQuality = Math.Max(70, Math.Min(100, g.SaveQuality));
			g.GuideLineStyle = Math.Max(0, Math.Min(2, g.GuideLineStyle));
			g.DefaultSize = Math.Max(1, Math.Min(3, g.DefaultSize));
			if (string.IsNullOrEmpty(g.BackgroundColor) ||
				(g.BackgroundColor != "白色" && g.BackgroundColor != "蓝色" && g.BackgroundColor != "红色" && g.BackgroundColor != "透明"))
				g.BackgroundColor = "蓝色";
			g.Tolerance = Math.Max(0, Math.Min(100, g.Tolerance));
			g.LayoutPreset = Math.Max(0, Math.Min(4, g.LayoutPreset));
			g.CustomLayoutW = Math.Max(100, Math.Min(10000, g.CustomLayoutW));
			g.CustomLayoutH = Math.Max(100, Math.Min(10000, g.CustomLayoutH));
			g.CurrentPreset = Math.Max(-1, Math.Min(g.Presets.Count - 1, g.CurrentPreset));

			g.Presets ??= new List<PresetProfile>();
			foreach (PresetProfile p in g.Presets)
			{
				if (p == null) continue;
				if (string.IsNullOrEmpty(p.Name)) p.Name = "";
				p.DefaultSize = Math.Max(1, Math.Min(3, p.DefaultSize));
				if (string.IsNullOrEmpty(p.BackgroundColor) ||
					(p.BackgroundColor != "白色" && p.BackgroundColor != "蓝色" && p.BackgroundColor != "红色" && p.BackgroundColor != "透明"))
					p.BackgroundColor = "蓝色";
				p.Tolerance = Math.Max(0, Math.Min(100, p.Tolerance));
				p.LayoutPreset = Math.Max(0, Math.Min(4, p.LayoutPreset));
				if (string.IsNullOrEmpty(p.SaveFormat))
					p.SaveFormat = "jpg";
				p.SaveQuality = Math.Max(70, Math.Min(100, p.SaveQuality));
			}
			g.CurrentPreset = Math.Max(-1, Math.Min(g.Presets.Count - 1, g.CurrentPreset));
		}

		/// <summary>
		/// 旧版两个独立文件（exe 目录 setting.json=app、gen_setting.json=gen）迁移到统一文件。
		/// 兼容路径：exe 目录（旧版存放处）与 SettingsFile（当前路径，可能已回退 %APPDATA%）。
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
				// 旧 app 设置：优先 exe 目录旧文件，其次当前 SettingsFile
				string legacyApp = Path.Combine(exeDir, "setting.json");
				if (File.Exists(SettingsFile))
					pkg.App = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsFile)) ?? new AppSettings();
				else if (File.Exists(legacyApp))
					pkg.App = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(legacyApp)) ?? new AppSettings();
			}
			catch
			{
			}

			// 启动时自动迁移，写失败静默（如 Program Files 只读），下次启动再试
			SavePackage(pkg, showError: false);
			return pkg;
		}

		/// <summary>将完整设置包写回统一文件。</summary>
		/// <param name="showError">写失败时是否弹窗提示（启动自动路径传 false 静默）。</param>
		private static void SavePackage(SettingsPackage pkg, bool showError = true)
		{
			try
			{
				string json = JsonSerializer.Serialize(pkg, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(SettingsFile, json);
			}
			catch (Exception ex)
			{
				if (showError)
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

		// ── 主题包读写 ──

		/// <summary>读取设置文件中的主题包，无则返回 null（调用方回退内置主题）。</summary>
		public static ThemePackage? LoadThemePackage()
		{
			var pkg = LoadPackage();
			if (pkg.Theme != null && pkg.Theme.Ass != null && pkg.Theme.Ass.Count > 0)
				return pkg.Theme;
			return null;
		}

		/// <summary>将主题包写入设置文件。</summary>
		public static void SaveThemePackage(ThemePackage theme)
		{
			var pkg = LoadPackage();
			pkg.Theme = theme;
			SavePackage(pkg);
		}

		// ── 快捷键读写（key 段）──

		public static KeySettings LoadKeySettings()
		{
			return LoadPackage().Key ?? new KeySettings();
		}

		public static void SaveKeySettings(KeySettings keys)
		{
			var pkg = LoadPackage();
			pkg.Key = keys;
			SavePackage(pkg);
		}

		// ── 隐藏设置读写（high 段：安装程序写入，不入设置面板与导入导出）──

		public static HighSettings LoadHighSettings()
		{
			return LoadPackage().High;
		}

		public static void SaveHighSettings(HighSettings settings, bool showError = true)
		{
			var pkg = LoadPackage();
			pkg.High = settings;
			SavePackage(pkg, showError);
		}

		/// <summary>
		/// 合并安装程序写入的 install-options.json 到设置 high 段。
		/// 不删除标记文件：静默更新时安装器不写该文件（保留用户原偏好），
		/// 文件常驻以记录安装选项。仅当值变化时才写盘，避免每次启动无谓写入。
		/// </summary>
		public static void MergeInstallOptions()
		{
			string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
			string optionsFile = Path.Combine(exeDir, "install-options.json");
			if (!File.Exists(optionsFile)) return;

			try
			{
				var options = JsonSerializer.Deserialize<HighSettings>(File.ReadAllText(optionsFile));
				if (options != null)
				{
					HighSettings high = LoadHighSettings();
					bool changed = false;
					if (!string.IsNullOrEmpty(options.DocsFormat) && options.DocsFormat != high.DocsFormat)
					{
						high.DocsFormat = options.DocsFormat;
						changed = true;
					}
					if (!string.IsNullOrEmpty(options.InstallLang) && options.InstallLang != high.InstallLang)
					{
						high.InstallLang = options.InstallLang;
						changed = true;
					}
					if (changed)
						SaveHighSettings(high, showError: false);
				}
			}
			catch
			{
				// 合并失败不阻塞启动，保留标记文件供下次尝试
			}
		}
	}
}
