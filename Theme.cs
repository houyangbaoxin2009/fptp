using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace fptp
{
	/// <summary>
	/// 界面主题：自动跟随系统深浅色，扁平化配色。
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

		/// <summary>检测系统深浅色并装载调色板。Windows 7 无注册表键，默认浅色。</summary>
		public static void Init()
		{
			DarkMode = DetectDarkMode();
			LoadPalette();
		}

		/// <summary>强制指定深浅色（设置面板手动切换用）。</summary>
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
