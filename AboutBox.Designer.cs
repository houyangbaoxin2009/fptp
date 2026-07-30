namespace fptp;

partial class AboutBox
{
	private System.ComponentModel.IContainer components = null;

	protected override void Dispose(bool disposing)
	{
		if (disposing && (components != null))
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	#region Windows 窗体设计器生成的代码

	private void InitializeComponent()
	{
		labelProductName = new Label();
		labelVersion = new Label();
		labelCopyright = new Label();
		labelCompany = new Label();
		labelLicense = new Label();
		flowButtons = new FlowLayoutPanel();
		btnChangelog = new Button();
		btnReadme = new Button();
		btnContributing = new Button();
		btnApiDoc = new Button();
		btnGitHub = new Button();
		btnDonate = new Button();
		btnWebsite = new Button();
		btnOk = new Button();
		flowButtons.SuspendLayout();
		SuspendLayout();
		//
		// labelProductName
		//
		labelProductName.AutoSize = true;
		labelProductName.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
		labelProductName.Location = new Point(16, 16);
		labelProductName.Name = "labelProductName";
		labelProductName.TabIndex = 0;
		labelProductName.Text = "产品名称";
		//
		// labelVersion
		//
		labelVersion.AutoSize = true;
		labelVersion.Location = new Point(16, 52);
		labelVersion.Name = "labelVersion";
		labelVersion.TabIndex = 1;
		labelVersion.Text = "版本";
		//
		// labelCopyright
		//
		labelCopyright.AutoSize = true;
		labelCopyright.Location = new Point(16, 78);
		labelCopyright.Name = "labelCopyright";
		labelCopyright.TabIndex = 2;
		labelCopyright.Text = "版权";
		//
		// labelCompany
		//
		labelCompany.AutoSize = true;
		labelCompany.Location = new Point(16, 104);
		labelCompany.Name = "labelCompany";
		labelCompany.TabIndex = 3;
		labelCompany.Text = "公司";
		//
		// labelLicense
		//
		labelLicense.AutoSize = true;
		labelLicense.ForeColor = SystemColors.GrayText;
		labelLicense.Location = new Point(16, 130);
		labelLicense.Name = "labelLicense";
		labelLicense.TabIndex = 4;
		labelLicense.Text = "许可";
		//
		// flowButtons
		//
		flowButtons.Controls.Add(btnChangelog);
		flowButtons.Controls.Add(btnReadme);
		flowButtons.Controls.Add(btnContributing);
		flowButtons.Controls.Add(btnApiDoc);
		flowButtons.Controls.Add(btnGitHub);
		flowButtons.Controls.Add(btnDonate);
		flowButtons.Controls.Add(btnWebsite);
		flowButtons.FlowDirection = FlowDirection.LeftToRight;
		flowButtons.Location = new Point(16, 164);
		flowButtons.Name = "flowButtons";
		flowButtons.Size = new Size(450, 120);
		flowButtons.TabIndex = 5;
		flowButtons.WrapContents = true;
		//
		// btnChangelog
		//
		btnChangelog.Location = new Point(3, 3);
		btnChangelog.Name = "btnChangelog";
		btnChangelog.Size = new Size(142, 32);
		btnChangelog.TabIndex = 0;
		btnChangelog.Text = "更新日志";
		btnChangelog.UseVisualStyleBackColor = true;
		btnChangelog.Click += btnChangelog_Click;
		//
		// btnReadme
		//
		btnReadme.Location = new Point(151, 3);
		btnReadme.Name = "btnReadme";
		btnReadme.Size = new Size(142, 32);
		btnReadme.TabIndex = 1;
		btnReadme.Text = "README";
		btnReadme.UseVisualStyleBackColor = true;
		btnReadme.Click += btnReadme_Click;
		//
		// btnContributing
		//
		btnContributing.Location = new Point(299, 3);
		btnContributing.Name = "btnContributing";
		btnContributing.Size = new Size(142, 32);
		btnContributing.TabIndex = 2;
		btnContributing.Text = "贡献指南";
		btnContributing.UseVisualStyleBackColor = true;
		btnContributing.Click += btnContributing_Click;
		//
		// btnApiDoc
		//
		btnApiDoc.Location = new Point(3, 41);
		btnApiDoc.Name = "btnApiDoc";
		btnApiDoc.Size = new Size(142, 32);
		btnApiDoc.TabIndex = 3;
		btnApiDoc.Text = "API 文档";
		btnApiDoc.UseVisualStyleBackColor = true;
		btnApiDoc.Click += btnApiDoc_Click;
		//
		// btnGitHub
		//
		btnGitHub.Location = new Point(151, 41);
		btnGitHub.Name = "btnGitHub";
		btnGitHub.Size = new Size(142, 32);
		btnGitHub.TabIndex = 4;
		btnGitHub.Text = "GitHub";
		btnGitHub.UseVisualStyleBackColor = true;
		btnGitHub.Click += btnGitHub_Click;
		//
		// btnDonate
		//
		btnDonate.Location = new Point(299, 41);
		btnDonate.Name = "btnDonate";
		btnDonate.Size = new Size(142, 32);
		btnDonate.TabIndex = 5;
		btnDonate.Text = "给作者买一杯咖啡";
		btnDonate.UseVisualStyleBackColor = true;
		btnDonate.Click += btnDonate_Click;
		//
		// btnWebsite
		//
		btnWebsite.Location = new Point(3, 79);
		btnWebsite.Name = "btnWebsite";
		btnWebsite.Size = new Size(142, 32);
		btnWebsite.TabIndex = 6;
		btnWebsite.Text = "访问官网";
		btnWebsite.UseVisualStyleBackColor = true;
		btnWebsite.Click += btnWebsite_Click;
		//
		// btnOk
		//
		btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
		btnOk.DialogResult = DialogResult.Cancel;
		btnOk.Location = new Point(370, 300);
		btnOk.Name = "btnOk";
		btnOk.Size = new Size(96, 30);
		btnOk.TabIndex = 7;
		btnOk.Text = "确定(&O)";
		//
		// AboutBox
		//
		AcceptButton = btnOk;
		AutoScaleMode = AutoScaleMode.None;
		CancelButton = btnOk;
		ClientSize = new Size(482, 346);
		Controls.Add(labelProductName);
		Controls.Add(labelVersion);
		Controls.Add(labelCopyright);
		Controls.Add(labelCompany);
		Controls.Add(labelLicense);
		Controls.Add(flowButtons);
		Controls.Add(btnOk);
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		Name = "AboutBox";
		ShowIcon = false;
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.CenterParent;
		Text = "AboutBox1";
		Load += AboutBox1_Load;
		flowButtons.ResumeLayout(false);
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private Label labelProductName;
	private Label labelVersion;
	private Label labelCopyright;
	private Label labelCompany;
	private Label labelLicense;
	private FlowLayoutPanel flowButtons;
	private Button btnChangelog;
	private Button btnReadme;
	private Button btnContributing;
	private Button btnApiDoc;
	private Button btnGitHub;
	private Button btnDonate;
	private Button btnWebsite;
	private Button btnOk;
}
