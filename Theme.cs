using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace fptp
{
	/// <summary>
	/// 界面主题：自动跟随系统深浅色，扁平化配色。
	/// 主题来源优先级：
	/// 1. 设置文件中的主题包（Assalg.LoadThemePackage，用户导入/自定义）
	/// 2. 内置深浅色调色板（跟随系统）
	/// 仅启动与手动切换时调用 Apply，无运行时开销。
	/// </summary>
	public static class Theme
	{
		public static bool DarkMode { get; private set; }

		// 调色板
		public static Color WindowBg;
		public static Color PanelBg;
		public static Color TextColor;
		public static Color SubText;
		public static Color Accent;
		public static Color Border;
		public static Color ButtonBg;
		public static Color PreviewBg;

		/// <summary>当前主题 id（auto=跟随系统，否则为主题包 id）。</summary>
		public static string CurrentId { get; private set; } = "auto";

		/// <summary>当前主题显示名（如 跟随系统 / 深蓝）。</summary>
		public static string CurrentName { get; private set; } = "";

		/// <summary>调色板键（主题包 ass 字典的键）。</summary>
		public static readonly string[] PaletteKeys =
		{
			"windowBg", "panelBg", "textColor", "subText", "accent", "border", "buttonBg", "previewBg"
		};

		/// <summary>内置主题定义：id → (显示名键, 调色板)。auto 特殊：跟随系统。</summary>
		private struct BuiltInTheme
		{
			public string Id;
			public string NameKey;
			public Color[] Palette; // null 表示跟随系统（auto）
		}

		private static readonly BuiltInTheme[] BuiltInThemes =
		{
			new BuiltInTheme { Id = "auto", NameKey = "settings.theme.auto", Palette = null },
			new BuiltInTheme { Id = "light", NameKey = "settings.theme.light", Palette = new Color[] {
				Color.FromArgb(245, 246, 250), Color.White, Color.FromArgb(31, 37, 51),
				Color.FromArgb(107, 114, 128), Color.FromArgb(65, 105, 225),
				Color.FromArgb(225, 228, 235), Color.White, Color.FromArgb(232, 235, 242) } },
			new BuiltInTheme { Id = "dark", NameKey = "settings.theme.dark", Palette = new Color[] {
				Color.FromArgb(32, 32, 36), Color.FromArgb(45, 45, 51), Color.FromArgb(232, 232, 235),
				Color.FromArgb(160, 160, 168), Color.FromArgb(94, 140, 255),
				Color.FromArgb(70, 70, 78), Color.FromArgb(58, 58, 66), Color.FromArgb(24, 24, 28) } },
			new BuiltInTheme { Id = "green", NameKey = "settings.theme.green", Palette = new Color[] {
				Color.FromArgb(240, 248, 242), Color.FromArgb(252, 255, 253), Color.FromArgb(30, 62, 44),
				Color.FromArgb(100, 130, 112), Color.FromArgb(46, 139, 87),
				Color.FromArgb(214, 232, 220), Color.FromArgb(252, 255, 253), Color.FromArgb(228, 240, 232) } },
			new BuiltInTheme { Id = "blue", NameKey = "settings.theme.blue", Palette = new Color[] {
				Color.FromArgb(26, 34, 52), Color.FromArgb(38, 48, 72), Color.FromArgb(226, 232, 246),
				Color.FromArgb(148, 163, 194), Color.FromArgb(99, 158, 255),
				Color.FromArgb(58, 70, 102), Color.FromArgb(46, 58, 88), Color.FromArgb(20, 26, 42) } },
		};

		/// <summary>检测系统深浅色并装载调色板。
		/// 优先级：1. 设置文件主题包（导入） 2. 内置主题（AppSettings.ThemeId，auto 跟随系统）。</summary>
		public static void Init()
		{
			DarkMode = DetectDarkMode();

			// 1. 设置文件中的主题包
			ThemePackage? pkg = Assalg.LoadThemePackage();
			if (pkg != null && pkg.Ass != null && TryApplyPalette(pkg.Ass))
			{
				CurrentId = string.IsNullOrEmpty(pkg.Con.Id) ? "custom" : pkg.Con.Id;
				CurrentName = string.IsNullOrEmpty(pkg.Con.Name) ? CurrentId : pkg.Con.Name;
				return;
			}

			// 2. 内置主题（按 AppSettings.ThemeId）
			ApplyBuiltIn(Assalg.LoadAppSettings().ThemeId);
		}

		/// <summary>应用指定内置主题（保存到 AppSettings.ThemeId 并加载调色板）。</summary>
		public static void SetBuiltIn(string id)
		{
			var app = Assalg.LoadAppSettings();
			app.ThemeId = string.IsNullOrEmpty(id) ? "auto" : id;
			Assalg.SaveAppSettings(app);
			ApplyBuiltIn(app.ThemeId);
		}

		private static void ApplyBuiltIn(string id)
		{
			foreach (BuiltInTheme t in BuiltInThemes)
			{
				if (t.Id == id)
				{
					CurrentId = t.Id;
					CurrentName = Lang.Get(t.NameKey);
					if (t.Palette == null)
					{
						LoadPalette(); // auto：跟随系统深浅色
					}
					else
					{
						WindowBg = t.Palette[0]; PanelBg = t.Palette[1]; TextColor = t.Palette[2];
						SubText = t.Palette[3]; Accent = t.Palette[4]; Border = t.Palette[5];
						ButtonBg = t.Palette[6]; PreviewBg = t.Palette[7];
					}
					return;
				}
			}
			// 未知 id 回退 auto
			CurrentId = "auto";
			CurrentName = Lang.Get("settings.theme.auto");
			LoadPalette();
		}

		/// <summary>强制指定深浅色（内置主题 auto 用）。</summary>
		public static void SetDark(bool dark)
		{
			DarkMode = dark;
			LoadPalette();
		}

		private static bool DetectDarkMode()
		{
			try
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
					@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
				{
					if (key != null && key.GetValue("AppsUseLightTheme") is int v)
						return v == 0;
				}
			}
			catch { }
			return false;
		}

		private static void LoadPalette()
		{
			if (DarkMode)
			{
				WindowBg = Color.FromArgb(32, 32, 36);
				PanelBg = Color.FromArgb(45, 45, 51);
				TextColor = Color.FromArgb(232, 232, 235);
				SubText = Color.FromArgb(160, 160, 168);
				Accent = Color.FromArgb(94, 140, 255);
				Border = Color.FromArgb(70, 70, 78);
				ButtonBg = Color.FromArgb(58, 58, 66);
				PreviewBg = Color.FromArgb(24, 24, 28);
			}
			else
			{
				WindowBg = Color.FromArgb(245, 246, 250);
				PanelBg = Color.White;
				TextColor = Color.FromArgb(31, 37, 51);
				SubText = Color.FromArgb(107, 114, 128);
				Accent = Color.FromArgb(65, 105, 225);
				Border = Color.FromArgb(225, 228, 235);
				ButtonBg = Color.White;
				PreviewBg = Color.FromArgb(232, 235, 242);
			}
		}

		/// <summary>校验主题包调色板：8 个键全部存在且颜色值可解析（不应用，仅校验）。</summary>
		public static bool ValidatePalette(Dictionary<string, string> ass)
		{
			if (ass == null) return false;
			for (int i = 0; i < PaletteKeys.Length; i++)
				if (!ass.TryGetValue(PaletteKeys[i], out string hex) || !TryParseColor(hex, out _))
					return false;
			return true;
		}

		/// <summary>尝试将主题包调色板应用到当前颜色。8 个键全部有效才算成功。</summary>
		private static bool TryApplyPalette(Dictionary<string, string> ass)
		{
			Color[] colors = new Color[PaletteKeys.Length];
			for (int i = 0; i < PaletteKeys.Length; i++)
			{
				if (!ass.TryGetValue(PaletteKeys[i], out string hex) || !TryParseColor(hex, out colors[i]))
					return false;
			}

			WindowBg = colors[0];
			PanelBg = colors[1];
			TextColor = colors[2];
			SubText = colors[3];
			Accent = colors[4];
			Border = colors[5];
			ButtonBg = colors[6];
			PreviewBg = colors[7];
			return true;
		}

		/// <summary>解析颜色值：支持 #RRGGBB 或 #AARRGGBB 或 ARGB 整数。</summary>
		private static bool TryParseColor(string s, out Color color)
		{
			color = Color.Empty;
			if (string.IsNullOrWhiteSpace(s)) return false;
			s = s.Trim();
			try
			{
				if (s.StartsWith("#"))
				{
					color = ColorTranslator.FromHtml(s);
					return color.A != 0 || s.Length >= 9; // 避免全透明误判为失败
				}
				if (int.TryParse(s, out int argb))
				{
					color = Color.FromArgb(argb);
					return true;
				}
			}
			catch { }
			return false;
		}

		/// <summary>将颜色格式化为 #RRGGBB 或 #AARRGGBB 字符串（主题包导出用，保留 alpha）。</summary>
		private static string ToHex(Color c)
		{
			if (c.A < 255)
				return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
			return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
		}

		/// <summary>注册（导入）主题包并写入设置文件并立即应用。</summary>
		public static void Register(string id, string name, Dictionary<string, string> palette)
		{
			Assalg.SaveThemePackage(new ThemePackage
			{
				Con = new ThemeCon { Id = string.IsNullOrEmpty(id) ? "custom" : id, Name = string.IsNullOrEmpty(name) ? id : name },
				Ass = palette
			});
			Init();
		}

		/// <summary>导出当前调色板（主题包本体，key → 颜色值）。</summary>
		public static Dictionary<string, string> ExportTable()
		{
			return new Dictionary<string, string>
			{
				["windowBg"] = ToHex(WindowBg),
				["panelBg"] = ToHex(PanelBg),
				["textColor"] = ToHex(TextColor),
				["subText"] = ToHex(SubText),
				["accent"] = ToHex(Accent),
				["border"] = ToHex(Border),
				["buttonBg"] = ToHex(ButtonBg),
				["previewBg"] = ToHex(PreviewBg)
			};
		}

		/// <summary>可用主题列表：内置主题（跟随系统/浅色/深色/护眼绿/深空蓝）+ 设置文件中已导入的主题。</summary>
		public static List<ThemeCon> AvailableThemes()
		{
			var list = new List<ThemeCon>();
			foreach (BuiltInTheme t in BuiltInThemes)
				list.Add(new ThemeCon { Id = t.Id, Name = Lang.Get(t.NameKey) });

			ThemePackage? pkg = Assalg.LoadThemePackage();
			if (pkg != null && !string.IsNullOrEmpty(pkg.Con.Id) && !list.Exists(x => x.Id == pkg.Con.Id))
			{
				list.Add(new ThemeCon
				{
					Id = pkg.Con.Id,
					Name = string.IsNullOrEmpty(pkg.Con.Name) ? pkg.Con.Id : pkg.Con.Name
				});
			}
			return list;
		}

		/// <summary>是否为内置主题 id。</summary>
		public static bool IsBuiltIn(string id)
		{
			foreach (BuiltInTheme t in BuiltInThemes)
				if (t.Id == id) return true;
			return false;
		}

		/// <summary>递归应用主题到整个控件树。仅启动/切换时调用。</summary>
		public static void Apply(Control root)
		{
			Stack<Control> stack = new Stack<Control>();
			stack.Push(root);
			while (stack.Count > 0)
			{
				Control c = stack.Pop();
				ApplyTo(c);
				foreach (Control child in c.Controls)
					stack.Push(child);
			}
		}

		private static void ApplyTo(Control c)
		{
			if (c is TrackBar || c is PictureBox)
			{
				if (c is PictureBox pb)
				{
					pb.BackColor = pb.Image == null ? PreviewBg : PanelBg;
					return;
				}
				return; // TrackBar 保持系统原生样式
			}

			c.BackColor = c is Form || c is Panel ? WindowBg : PanelBg;
			c.ForeColor = TextColor;

			switch (c)
			{
				case Button btn:
					btn.FlatStyle = FlatStyle.Flat;
					btn.FlatAppearance.BorderColor = Border;
					btn.FlatAppearance.BorderSize = 1;
					break;
				case TextBox tb:
					tb.BorderStyle = BorderStyle.FixedSingle;
					break;
				case ComboBox cmb:
					cmb.FlatStyle = FlatStyle.Flat;
					break;
			}
		}
	}
}
