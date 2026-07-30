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
		labelProductName.Text = $"{Basic.AppName} v{Basic.AppVersion}";
		labelVersion.Text = $".NET Framework 4.8 | Windows 7 SP1+";
		labelCopyright.Text = Basic.AppCopyright;
		labelCompany.Text = Basic.AppCompany;
		labelLicense.Text = "Apache License, Version 2.0";
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
	private void btnApiDoc_Click(object sender, EventArgs e) => OpenFile("API.md");
	private void btnGitHub_Click(object sender, EventArgs e) => OpenUrl(Basic.AppGitHub);
	private void btnDonate_Click(object sender, EventArgs e)
	{
		string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
		string imgPath = Path.Combine(exeDir, "Assets", "donate.jpg");
		if (!File.Exists(imgPath))
		{
			MessageBox.Show(this, "未找到收款码图片。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		using (var img = Image.FromFile(imgPath))
		using (var form = new Form())
		{
			form.Text = "给作者买一杯咖啡 ☕";
			form.FormBorderStyle = FormBorderStyle.FixedDialog;
			form.MaximizeBox = false;
			form.MinimizeBox = false;
			form.StartPosition = FormStartPosition.CenterParent;
			form.ClientSize = new Size(img.Width + 20, img.Height + 40);
			form.ShowIcon = false;
			form.ShowInTaskbar = false;

			var pb = new PictureBox
			{
				Image = (Image)img.Clone(),
				SizeMode = PictureBoxSizeMode.AutoSize,
				Location = new Point(10, 10),
			};
			form.Controls.Add(pb);
			form.ShowDialog(this);
		}
	}
	private void btnWebsite_Click(object sender, EventArgs e) => OpenUrl(Basic.AppWebsite);
}
