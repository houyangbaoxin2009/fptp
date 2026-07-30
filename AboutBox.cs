using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace fptp;

partial class AboutBox : Form
{
	private void AboutBox1_Load(object sender, EventArgs e)
	{
		// ================= 使用我们的 Basic 类统一管理信息 =================

		// 设置窗体标题
		this.Text = $"关于 {Basic.AppName}";

		// 设置产品名称 (注意：labelProductName 是默认控件名，如果你改过名字请对应修改)
		labelProductName.Text = Basic.AppName;

		// 设置版本号
		labelVersion.Text = $"{Basic.AppVersion}";

		// 设置版权信息
		labelCopyright.Text = $"{Basic.AppCopyright}";

		// 设置公司名称 (注意：labelCompanyName 是默认控件名，如果你改过名字请对应修改)
		labelCompanyName.Text = $"{Basic.AppCompany}";
	}

	public AboutBox()
	{
		InitializeComponent();
		// ================= 使用我们的 Basic 类统一管理信息 =================

		// 设置窗体标题
		this.Text = $"关于 {Basic.AppName}";

		// 设置产品名称 (注意：labelProductName 是默认控件名，如果你改过名字请对应修改)
		labelProductName.Text = Basic.AppName;

		// 设置版本号
		labelVersion.Text = $"{Basic.AppVersion}";

		// 设置版权信息
		labelCopyright.Text = $"{Basic.AppCopyright}";

		// 设置公司名称 (注意：labelCompanyName 是默认控件名，如果你改过名字请对应修改)
		labelCompanyName.Text = $"{Basic.AppCompany}";
	}

	#region 程序集特性访问器

	public string AssemblyTitle
	{
		get
		{
			object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
			if (attributes.Length > 0)
			{
				AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
				if (titleAttribute.Title != "")
				{
					return titleAttribute.Title;
				}
			}
			return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
		}
	}

	public string AssemblyVersion
	{
		get
		{
			return Assembly.GetExecutingAssembly().GetName().Version.ToString();
		}
	}

	public string AssemblyDescription
	{
		get
		{
			object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
			if (attributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyDescriptionAttribute)attributes[0]).Description;
		}
	}

	public string AssemblyProduct
	{
		get
		{
			object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
			if (attributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyProductAttribute)attributes[0]).Product;
		}
	}

	public string AssemblyCopyright
	{
		get
		{
			object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
			if (attributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
		}
	}

	public string AssemblyCompany
	{
		get
		{
			object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
			if (attributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyCompanyAttribute)attributes[0]).Company;
		}
	}
	#endregion

	private void labelProductName_Click(object sender, EventArgs e)
	{

	}

	private void tableLayoutPanel_Paint(object sender, PaintEventArgs e)
	{

	}
}
