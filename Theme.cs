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

		/// <summary>检测系统深浅色并装载调色板。优先设置文件主题包，否则内置。</summary>
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

			// 2. 内置（跟随系统）
			CurrentId = "auto";
			CurrentName = Lang.Get("settings.theme.auto");
			LoadPalette();
		}

		/// <summary>强制指定深浅色（内置主题用）。</summary>
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

		/// <summary>将颜色格式化为 #RRGGBB 字符串（主题包导出用）。</summary>
		private static string ToHex(Color c)
		{
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

		/// <summary>可用主题列表：内置"跟随系统" + 设置文件中已导入的主题。</summary>
		public static List<ThemeCon> AvailableThemes()
		{
			var list = new List<ThemeCon>
			{
				new ThemeCon { Id = "auto", Name = Lang.Get("settings.theme.auto") }
			};
			ThemePackage? pkg = Assalg.LoadThemePackage();
			if (pkg != null && !string.IsNullOrEmpty(pkg.Con.Id))
			{
				list.Add(new ThemeCon
				{
					Id = pkg.Con.Id,
					Name = string.IsNullOrEmpty(pkg.Con.Name) ? pkg.Con.Id : pkg.Con.Name
				});
			}
			return list;
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
