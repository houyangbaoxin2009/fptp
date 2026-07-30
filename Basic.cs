using System;
using System.Drawing;
using System.Windows.Forms;

namespace fptp
{
	/// <summary>
	/// 应用配置常量与通用工具方法。
	/// 所有证件照尺寸基于 300 DPI 换算。
	/// </summary>
	public static class Basic
	{
		// ── 应用信息 ──
		public const string AppName = "FPTP";
		public const string AppVersion = "1.1.1.1";
		public const string AppCopyright = "Copyright © 2026 Jiro";
		public const string AppCompany = "FranJ2";
		public const string AppGitHub = "https://github.com/houyangbaoxin2009/fptp";
		public const string AppWebsite = "https://github.com/houyangbaoxin2009/fptp.git";

		// ── 证件照标准尺寸（像素 @300DPI） ──
		public const int ONE_INCH_W = 295;   // 一寸 25mm x 35mm
		public const int ONE_INCH_H = 413;
		public const int TWO_INCH_W = 413;   // 二寸 35mm x 53mm
		public const int TWO_INCH_H = 626;
		public const int PASSPORT_W = 390;   // 小二寸（护照/签证）33mm x 48mm
		public const int PASSPORT_H = 567;

		/// <summary>
		/// 检查图片是否已加载，未加载时弹出警告。
		/// </summary>
		/// <param name="img">待检查的图片</param>
		/// <param name="parent">父窗体（用于弹窗居中）</param>
		/// <returns>已加载返回 true，否则 false</returns>
		public static bool CheckImage(Bitmap img, Form parent)
		{
			if (img == null)
			{
				MessageBox.Show(parent, "请先加载图片！", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}
			return true;
		}

		/// <summary>
		/// 获取带版本号的应用标题，用于窗口标题栏。
		/// </summary>
		public static string GetAppTitle()
		{
			return $"{AppName} v{AppVersion}";
		}

		/// <summary>
		/// 弹出文件选择对话框，返回用户选取的图片路径。
		/// </summary>
		/// <param name="parent">父窗体</param>
		/// <returns>文件路径，取消则返回 null</returns>
		public static string OpenImageFile(Form parent)
		{
			using (OpenFileDialog ofd = new OpenFileDialog())
			{
				ofd.Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp";
				if (ofd.ShowDialog(parent) == DialogResult.OK)
				{
					return ofd.FileName;
				}
			}
			return null;
		}

	}
}
