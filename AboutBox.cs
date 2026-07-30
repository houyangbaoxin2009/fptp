using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
		Text = $"关于 {Basic.AppName}";
		labelProductName.Text = Basic.AppName;
		labelVersion.Text = $"v{Basic.AppVersion}";
		labelCopyright.Text = Basic.AppCopyright;
		labelCompanyName.Text = Basic.AppCompany;
	}

	private void OpenFile(string fileName)
	{
		try
		{
			string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
			string[] candidates =
			{
				Path.Combine(exeDir, fileName),
				Path.Combine(exeDir, "..", "..", "..", fileName),
				Path.Combine(exeDir, "..", "..", "..", "..", fileName),
			};

			foreach (string path in candidates)
			{
				string full = Path.GetFullPath(path);
				if (File.Exists(full))
				{
					Process.Start(new ProcessStartInfo { FileName = full, UseShellExecute = true });
					return;
				}
			}

			MessageBox.Show(this, $"文件未找到：{fileName}\n请确保文档文件与程序在同一目录。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, $"无法打开文件：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void OpenUrl(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, $"无法打开链接：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void btnChangelog_Click(object sender, EventArgs e) => OpenFile("CHANGELOG.md");
	private void btnReadme_Click(object sender, EventArgs e) => OpenFile("README.md");
	private void btnContributing_Click(object sender, EventArgs e) => OpenFile("CONTRIBUTING.md");
	private void btnApiDoc_Click(object sender, EventArgs e) => OpenFile("README.md");
	private void btnGitHub_Click(object sender, EventArgs e) => OpenUrl("https://github.com/FranJ2/fptp");
	private void btnDonate_Click(object sender, EventArgs e) => OpenUrl("https://github.com/FranJ2/fptp");
	private void btnWebsite_Click(object sender, EventArgs e) => OpenUrl("https://github.com/FranJ2/fptp");
}
