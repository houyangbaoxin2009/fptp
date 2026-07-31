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
		labelUpdateSource = new Label();
		groupDocs = new GroupBox();
		flowDocs = new FlowLayoutPanel();
		btnChangelog = new Button();
		btnReadme = new Button();
		btnContributing = new Button();
		btnApiDoc = new Button();
		groupSupport = new GroupBox();
		flowSupport = new FlowLayoutPanel();
		btnGitHub = new Button();
		btnWebsite = new Button();
		btnReportIssue = new Button();
		btnContact = new Button();
		groupAction = new GroupBox();
		flowAction = new FlowLayoutPanel();
		btnCheckUpdate = new Button();
		btnDonate = new Button();
		btnOk = new Button();
		groupDocs.SuspendLayout();
		flowDocs.SuspendLayout();
		groupSupport.SuspendLayout();
		flowSupport.SuspendLayout();
		groupAction.SuspendLayout();
		flowAction.SuspendLayout();
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
		// labelUpdateSource
		//
		labelUpdateSource.AutoSize = true;
		labelUpdateSource.ForeColor = SystemColors.GrayText;
		labelUpdateSource.Location = new Point(16, 156);
		labelUpdateSource.Name = "labelUpdateSource";
		labelUpdateSource.TabIndex = 5;
		labelUpdateSource.Text = "更新源";
		//
		// groupDocs
		//
		groupDocs.Controls.Add(flowDocs);
		groupDocs.Location = new Point(16, 184);
		groupDocs.Name = "groupDocs";
		groupDocs.Size = new Size(488, 112);
		groupDocs.TabIndex = 6;
		groupDocs.TabStop = false;
		groupDocs.Text = "文档";
		//
		// flowDocs
		//
		flowDocs.Controls.Add(btnChangelog);
		flowDocs.Controls.Add(btnReadme);
		flowDocs.Controls.Add(btnContributing);
		flowDocs.Controls.Add(btnApiDoc);
		flowDocs.FlowDirection = FlowDirection.LeftToRight;
		flowDocs.Location = new Point(12, 26);
		flowDocs.Name = "flowDocs";
		flowDocs.Size = new Size(464, 76);
		flowDocs.TabIndex = 0;
		flowDocs.WrapContents = true;
		//
		// btnChangelog
		//
		btnChangelog.Location = new Point(3, 3);
		btnChangelog.Name = "btnChangelog";
		btnChangelog.Size = new Size(140, 32);
		btnChangelog.TabIndex = 0;
		btnChangelog.Text = "更新日志";
		btnChangelog.UseVisualStyleBackColor = true;
		btnChangelog.Click += btnChangelog_Click;
		//
		// btnReadme
		//
		btnReadme.Location = new Point(149, 3);
		btnReadme.Name = "btnReadme";
		btnReadme.Size = new Size(140, 32);
		btnReadme.TabIndex = 1;
		btnReadme.Text = "README";
		btnReadme.UseVisualStyleBackColor = true;
		btnReadme.Click += btnReadme_Click;
		//
		// btnContributing
		//
		btnContributing.Location = new Point(3, 41);
		btnContributing.Name = "btnContributing";
		btnContributing.Size = new Size(140, 32);
		btnContributing.TabIndex = 2;
		btnContributing.Text = "贡献指南";
		btnContributing.UseVisualStyleBackColor = true;
		btnContributing.Click += btnContributing_Click;
		//
		// btnApiDoc
		//
		btnApiDoc.Location = new Point(149, 41);
		btnApiDoc.Name = "btnApiDoc";
		btnApiDoc.Size = new Size(140, 32);
		btnApiDoc.TabIndex = 3;
		btnApiDoc.Text = "API 文档";
		btnApiDoc.UseVisualStyleBackColor = true;
		btnApiDoc.Click += btnApiDoc_Click;
		//
		// groupSupport
		//
		groupSupport.Controls.Add(flowSupport);
		groupSupport.Location = new Point(16, 304);
		groupSupport.Name = "groupSupport";
		groupSupport.Size = new Size(488, 112);
		groupSupport.TabIndex = 7;
		groupSupport.TabStop = false;
		groupSupport.Text = "支持与联系";
		//
		// flowSupport
		//
		flowSupport.Controls.Add(btnGitHub);
		flowSupport.Controls.Add(btnWebsite);
		flowSupport.Controls.Add(btnReportIssue);
		flowSupport.Controls.Add(btnContact);
		flowSupport.FlowDirection = FlowDirection.LeftToRight;
		flowSupport.Location = new Point(12, 26);
		flowSupport.Name = "flowSupport";
		flowSupport.Size = new Size(464, 76);
		flowSupport.TabIndex = 0;
		flowSupport.WrapContents = true;
		//
		// btnGitHub
		//
		btnGitHub.Location = new Point(3, 3);
		btnGitHub.Name = "btnGitHub";
		btnGitHub.Size = new Size(140, 32);
		btnGitHub.TabIndex = 0;
		btnGitHub.Text = "GitHub";
		btnGitHub.UseVisualStyleBackColor = true;
		btnGitHub.Click += btnGitHub_Click;
		//
		// btnWebsite
		//
		btnWebsite.Location = new Point(149, 3);
		btnWebsite.Name = "btnWebsite";
		btnWebsite.Size = new Size(140, 32);
		btnWebsite.TabIndex = 1;
		btnWebsite.Text = "访问官网";
		btnWebsite.UseVisualStyleBackColor = true;
		btnWebsite.Click += btnWebsite_Click;
		//
		// btnReportIssue
		//
		btnReportIssue.Location = new Point(3, 41);
		btnReportIssue.Name = "btnReportIssue";
		btnReportIssue.Size = new Size(140, 32);
		btnReportIssue.TabIndex = 2;
		btnReportIssue.Text = "报告问题";
		btnReportIssue.UseVisualStyleBackColor = true;
		btnReportIssue.Click += btnReportIssue_Click;
		//
		// btnContact
		//
		btnContact.Location = new Point(149, 41);
		btnContact.Name = "btnContact";
		btnContact.Size = new Size(140, 32);
		btnContact.TabIndex = 3;
		btnContact.Text = "联系作者";
		btnContact.UseVisualStyleBackColor = true;
		btnContact.Click += btnContact_Click;
		//
		// groupAction
		//
		groupAction.Controls.Add(flowAction);
		groupAction.Location = new Point(16, 424);
		groupAction.Name = "groupAction";
		groupAction.Size = new Size(488, 64);
		groupAction.TabIndex = 8;
		groupAction.TabStop = false;
		groupAction.Text = "操作";
		//
		// flowAction
		//
		flowAction.Controls.Add(btnCheckUpdate);
		flowAction.Controls.Add(btnDonate);
		flowAction.FlowDirection = FlowDirection.LeftToRight;
		flowAction.Location = new Point(12, 24);
		flowAction.Name = "flowAction";
		flowAction.Size = new Size(464, 32);
		flowAction.TabIndex = 0;
		flowAction.WrapContents = true;
		//
		// btnCheckUpdate
		//
		btnCheckUpdate.Location = new Point(3, 3);
		btnCheckUpdate.Name = "btnCheckUpdate";
		btnCheckUpdate.Size = new Size(140, 32);
		btnCheckUpdate.TabIndex = 0;
		btnCheckUpdate.Text = "检查更新";
		btnCheckUpdate.UseVisualStyleBackColor = true;
		btnCheckUpdate.Click += btnCheckUpdate_Click;
		//
		// btnDonate
		//
		btnDonate.Location = new Point(149, 3);
		btnDonate.Name = "btnDonate";
		btnDonate.Size = new Size(140, 32);
		btnDonate.TabIndex = 1;
		btnDonate.Text = "给作者买一杯咖啡";
		btnDonate.UseVisualStyleBackColor = true;
		btnDonate.Click += btnDonate_Click;
		//
		// btnOk
		//
		btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
		btnOk.DialogResult = DialogResult.Cancel;
		btnOk.Location = new Point(408, 496);
		btnOk.Name = "btnOk";
		btnOk.Size = new Size(96, 30);
		btnOk.TabIndex = 9;
		btnOk.Text = "确定(&O)";
		//
		// AboutBox
		//
		AcceptButton = btnOk;
		AutoScaleMode = AutoScaleMode.None;
		CancelButton = btnOk;
		ClientSize = new Size(520, 530);
		Controls.Add(labelProductName);
		Controls.Add(labelVersion);
		Controls.Add(labelCopyright);
		Controls.Add(labelCompany);
		Controls.Add(labelLicense);
		Controls.Add(labelUpdateSource);
		Controls.Add(groupDocs);
		Controls.Add(groupSupport);
		Controls.Add(groupAction);
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
		flowAction.ResumeLayout(false);
		groupAction.ResumeLayout(false);
		flowSupport.ResumeLayout(false);
		groupSupport.ResumeLayout(false);
		flowDocs.ResumeLayout(false);
		groupDocs.ResumeLayout(false);
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private Label labelProductName;
	private Label labelVersion;
	private Label labelCopyright;
	private Label labelCompany;
	private Label labelLicense;
	private Label labelUpdateSource;
	private GroupBox groupDocs;
	private FlowLayoutPanel flowDocs;
	private Button btnChangelog;
	private Button btnReadme;
	private Button btnContributing;
	private Button btnApiDoc;
	private GroupBox groupSupport;
	private FlowLayoutPanel flowSupport;
	private Button btnGitHub;
	private Button btnWebsite;
	private Button btnReportIssue;
	private Button btnContact;
	private GroupBox groupAction;
	private FlowLayoutPanel flowAction;
	private Button btnCheckUpdate;
	private Button btnDonate;
	private Button btnOk;
}
