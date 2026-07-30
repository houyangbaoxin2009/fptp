using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

namespace fptp
{
	public static class Basic
	{
		// ================= 配置区域 =================

		// 项目名称
		public const string AppName = "FPTP";

		// 项目版本号 (硬编码显示用，实际EXE版本请参考之前的 .csproj 设置)
		public const string AppVersion = "1.1.1.0";

		// 项目版权
		public const string AppCopyright = "Copyright © 2026 Jiro";

		// 所属公司
		public const string AppCompany = "FranJ2";

		// ================= 尺寸常量 =================
		// 300DPI
		// 一寸照 (25mm x 35mm)
		public const int ONE_INCH_W = 295;
		public const int ONE_INCH_H = 413;

		// 二寸照 (35mm x 53mm)
		public const int TWO_INCH_W = 413;
		public const int TWO_INCH_H = 626;

		// 小二寸 (护照/签证) (33mm x 48mm)
		public const int PASSPORT_W = 390;
		public const int PASSPORT_H = 567;

		// ================= 封装区域 =================

		/// <summary>
		/// 检查图片是否已加载。如果未加载，弹出提示并返回 false。
		/// </summary>
		/// <param name="img">要检查的图片对象</param>
		/// <param name="parent">父窗体，用于弹窗居中</param>
		/// <returns>如果图片存在返回 true，否则返回 false</returns>
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
		/// 获取应用程序的完整标题（包含版本号）
		/// </summary>
		public static string GetAppTitle()
		{
			// 优先使用程序集的真实版本（如果配置了自动版本）
			// string realVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			// return $"{AppName} v{realVer}";

			// 使用上面硬编码的版本
			return $"{AppName} v{AppVersion}";
		}

		/// <summary>
		/// 通用的打开文件对话框
		/// </summary>
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
