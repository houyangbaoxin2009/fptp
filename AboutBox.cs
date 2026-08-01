using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace fptp;

partial class AboutBox : Form
{
	private string docExt = ".md";

	public AboutBox()
	{
		InitializeComponent();
	}

	private void AboutBox1_Load(object sender, EventArgs e)
	{
		Text = Lang.Get("about.title", Basic.AppName);
		labelProductName.Text = $"{Basic.AppName} v{Basic.AppVersion}";
		labelVersion.Text = $".NET Framework 4.8 | Windows 7 SP1+";
		labelCopyright.Text = Basic.AppCopyright;
		labelCompany.Text = Basic.AppCompany;
		labelLicense.Text = "Apache License, Version 2.0";
		labelUpdateSource.Text = Lang.Get("about.updateSource", RegionDetector.IsChina() ? Lang.Get("about.sourceGitCode") : Lang.Get("about.sourceGitHub"));
		groupDocs.Text = Lang.Get("about.groupDocs");
		groupSupport.Text = Lang.Get("about.groupSupport");
		groupAction.Text = Lang.Get("about.groupAction");
		btnChangelog.Text = Lang.Get("about.changelog");
		btnReadme.Text = Lang.Get("about.readme");
		btnContributing.Text = Lang.Get("about.contributing");
		btnApiDoc.Text = Lang.Get("about.api");
		btnGitHub.Text = Lang.Get("about.github");
		btnReportIssue.Text = Lang.Get("about.reportIssue");
		btnContact.Text = Lang.Get("about.contact");
		btnDonate.Text = Lang.Get("about.donate");
		btnWebsite.Text = Lang.Get("about.website");
		btnCheckUpdate.Text = Lang.Get("about.checkUpdate");
		btnOk.Text = Lang.Get("about.ok");

		string docsFormat = Assalg.LoadHighSettings().DocsFormat;
		docExt = docsFormat == "pdf" ? ".pdf" : ".md";
		bool docsEnabled = docsFormat != "none";
		btnChangelog.Enabled = docsEnabled;
		btnReadme.Enabled = docsEnabled;
		btnContributing.Enabled = docsEnabled;
		btnApiDoc.Enabled = docsEnabled;
	}

	private void OpenFile(string fileName)
	{
		try
		{
			string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
			string[] candidates =
			{
				Path.Combine(exeDir, "doc", fileName),
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

			MessageBox.Show(this, Lang.Get("about.fileNotFound", fileName), Lang.Get("msg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, Lang.Get("about.openFailed", ex.Message), Lang.Get("msg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
			MessageBox.Show(this, Lang.Get("about.urlFailed", ex.Message), Lang.Get("msg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void OpenDoc(string fileName)
	{
		OpenFile(Path.ChangeExtension(fileName, docExt));
	}

	private void btnChangelog_Click(object sender, EventArgs e) => OpenDoc("CHANGELOG.md");
	private void btnReadme_Click(object sender, EventArgs e) => OpenDoc("README.md");
	private void btnContributing_Click(object sender, EventArgs e) => OpenDoc("CONTRIBUTING.md");
	private void btnApiDoc_Click(object sender, EventArgs e) => OpenDoc("API.md");
	private void btnGitHub_Click(object sender, EventArgs e) => OpenUrl(Basic.AppGitHub);
	private void btnReportIssue_Click(object sender, EventArgs e) =>
		OpenUrl(RegionDetector.IsChina() ? "https://gitcode.com/jiro2025/fptp/issues" : "https://github.com/houyangbaoxin2009/fptp/issues");
	private void btnContact_Click(object sender, EventArgs e)
	{
		using (var form = new Form())
		{
			form.Text = Lang.Get("about.contactTitle");
			form.FormBorderStyle = FormBorderStyle.FixedDialog;
			form.MaximizeBox = false;
			form.MinimizeBox = false;
			form.StartPosition = FormStartPosition.CenterParent;
			form.ShowIcon = false;
			form.ShowInTaskbar = false;
			form.KeyPreview = true;
			form.ClientSize = new Size(280, 142);

			var lblEmail = new Label
			{
				AutoSize = true,
				Location = new Point(16, 16),
				Text = Lang.Get("about.email"),
			};
			var lnkEmail = new LinkLabel
			{
				AutoSize = true,
				Location = new Point(16, 40),
				Text = "3187909557@qq.com",
			};
			lnkEmail.Click += (_, _) => OpenUrl("mailto:3187909557@qq.com");
			var lblQq = new Label
			{
				AutoSize = true,
				Location = new Point(16, 64),
				Text = Lang.Get("about.qq"),
			};
			var lnkQq = new LinkLabel
			{
				AutoSize = true,
				Location = new Point(16, 88),
				Text = "3187909557",
			};
			var lblQqGroup = new Label
			{
				AutoSize = true,
				Location = new Point(16, 112),
				Text = Lang.Get("about.qqGroup"),
			};
			var lnkQqGroup = new LinkLabel
			{
				AutoSize = true,
				Location = new Point(76, 112),
				Text = Lang.Get("about.qqGroupJoin"),
			};
			lnkQqGroup.Click += (_, _) => OpenUrl("https://qm.qq.com/q/rM7Vy0YSiI");

			form.Controls.Add(lblEmail);
			form.Controls.Add(lnkEmail);
			form.Controls.Add(lblQq);
			form.Controls.Add(lnkQq);
			form.Controls.Add(lblQqGroup);
			form.Controls.Add(lnkQqGroup);

			// Escape 关闭
			form.KeyDown += (_, args) =>
			{
				if (args.KeyCode == Keys.Escape) form.Close();
			};

			form.ShowDialog(this);
		}
	}
	private void btnDonate_Click(object sender, EventArgs e)
	{
		string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
		string imgPath = Path.Combine(exeDir, "img", "donate.jpg");
		if (!File.Exists(imgPath))
		{
			MessageBox.Show(this, Lang.Get("about.noQrCode"), Lang.Get("msg.tip"), MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		using (var img = Image.FromFile(imgPath))
		using (var form = new Form())
		{
			form.Text = Lang.Get("about.donateTitle");
			form.FormBorderStyle = FormBorderStyle.FixedDialog;
			form.MaximizeBox = false;
			form.MinimizeBox = false;
			form.StartPosition = FormStartPosition.CenterParent;
			form.ShowIcon = false;
			form.ShowInTaskbar = false;
			form.KeyPreview = true;

			// 缩放到屏幕工作区的 80%，避免过大无法关闭
			var wa = Screen.GetWorkingArea(form);
			float maxW = wa.Width * 0.5f;
			float maxH = wa.Height * 0.5f;
			float scale = Math.Min(1f, Math.Min(maxW / img.Width, maxH / img.Height));
			int dispW = (int)(img.Width * scale);
			int dispH = (int)(img.Height * scale);

			form.ClientSize = new Size(dispW + 20, dispH + 40);

			var pb = new PictureBox
			{
				SizeMode = PictureBoxSizeMode.Zoom,
				Image = (Image)img.Clone(),
				Location = new Point(10, 10),
				Size = new Size(dispW, dispH),
			};
			form.Controls.Add(pb);
			// 关闭时释放克隆位图，避免每次打赏弹窗泄漏一张图
			form.FormClosed += (_, _) => pb.Image?.Dispose();

			// Escape 关闭
			form.KeyDown += (_, args) =>
			{
				if (args.KeyCode == Keys.Escape) form.Close();
			};

			form.ShowDialog(this);
		}
	}
	private void btnWebsite_Click(object sender, EventArgs e) => OpenUrl(Basic.AppWebsite);
	private void btnCheckUpdate_Click(object sender, EventArgs e) => Updater.CheckManual(this);
}
