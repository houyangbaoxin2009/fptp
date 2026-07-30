using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace fptp;

partial class AboutBox : Form
{
	public AboutBox()
	{
		InitializeComponent();
	}

	private void AboutBox1_Load(object sender, EventArgs e)
	{
		this.Text = $"关于 {Basic.AppName}";
		labelProductName.Text = Basic.AppName;
		labelVersion.Text = $"{Basic.AppVersion}";
		labelCopyright.Text = $"{Basic.AppCopyright}";
		labelCompanyName.Text = $"{Basic.AppCompany}";
	}

	private void labelProductName_Click(object sender, EventArgs e) { }
	private void tableLayoutPanel_Paint(object sender, PaintEventArgs e) { }
}
