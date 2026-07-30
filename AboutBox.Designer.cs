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
		tableLayoutPanel = new TableLayoutPanel();
		logoPictureBox = new PictureBox();
		labelProductName = new Label();
		labelVersion = new Label();
		labelCopyright = new Label();
		labelCompanyName = new Label();
		flowLayoutPanel = new FlowLayoutPanel();
		btnChangelog = new Button();
		btnReadme = new Button();
		btnContributing = new Button();
		btnApiDoc = new Button();
		btnGitHub = new Button();
		btnDonate = new Button();
		btnWebsite = new Button();
		okButton = new Button();
		tableLayoutPanel.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
		flowLayoutPanel.SuspendLayout();
		SuspendLayout();
		//
		// tableLayoutPanel
		//
		tableLayoutPanel.ColumnCount = 2;
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
		tableLayoutPanel.Controls.Add(logoPictureBox, 0, 0);
		tableLayoutPanel.Controls.Add(labelProductName, 1, 0);
		tableLayoutPanel.Controls.Add(labelVersion, 1, 1);
		tableLayoutPanel.Controls.Add(labelCopyright, 1, 2);
		tableLayoutPanel.Controls.Add(labelCompanyName, 1, 3);
		tableLayoutPanel.Controls.Add(flowLayoutPanel, 1, 5);
		tableLayoutPanel.Controls.Add(okButton, 1, 4);
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.Location = new Point(10, 12);
		tableLayoutPanel.Margin = new Padding(4);
		tableLayoutPanel.Name = "tableLayoutPanel";
		tableLayoutPanel.RowCount = 6;
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 12F));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		tableLayoutPanel.Size = new Size(556, 446);
		tableLayoutPanel.TabIndex = 0;
		//
		// logoPictureBox
		//
		logoPictureBox.Dock = DockStyle.Fill;
		logoPictureBox.Location = new Point(4, 4);
		logoPictureBox.Margin = new Padding(4);
		logoPictureBox.Name = "logoPictureBox";
		logoPictureBox.Size = new Size(186, 438);
		logoPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
		logoPictureBox.TabIndex = 12;
		logoPictureBox.TabStop = false;
		tableLayoutPanel.SetRowSpan(logoPictureBox, 6);
		//
		// labelProductName
		//
		labelProductName.Dock = DockStyle.Fill;
		labelProductName.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
		labelProductName.Location = new Point(198, 0);
		labelProductName.Margin = new Padding(4, 0, 4, 0);
		labelProductName.Name = "labelProductName";
		labelProductName.Size = new Size(354, 35);
		labelProductName.TabIndex = 19;
		labelProductName.Text = "产品名称";
		labelProductName.TextAlign = ContentAlignment.MiddleLeft;
		//
		// labelVersion
		//
		labelVersion.Dock = DockStyle.Fill;
		labelVersion.Location = new Point(198, 35);
		labelVersion.Margin = new Padding(4, 0, 4, 0);
		labelVersion.Name = "labelVersion";
		labelVersion.Size = new Size(354, 35);
		labelVersion.TabIndex = 0;
		labelVersion.Text = "版本";
		labelVersion.TextAlign = ContentAlignment.MiddleLeft;
		//
		// labelCopyright
		//
		labelCopyright.Dock = DockStyle.Fill;
		labelCopyright.Location = new Point(198, 70);
		labelCopyright.Margin = new Padding(4, 0, 4, 0);
		labelCopyright.Name = "labelCopyright";
		labelCopyright.Size = new Size(354, 35);
		labelCopyright.TabIndex = 21;
		labelCopyright.Text = "版权";
		labelCopyright.TextAlign = ContentAlignment.MiddleLeft;
		//
		// labelCompanyName
		//
		labelCompanyName.Dock = DockStyle.Fill;
		labelCompanyName.Location = new Point(198, 105);
		labelCompanyName.Margin = new Padding(4, 0, 4, 0);
		labelCompanyName.Name = "labelCompanyName";
		labelCompanyName.Size = new Size(354, 35);
		labelCompanyName.TabIndex = 22;
		labelCompanyName.Text = "公司名称";
		labelCompanyName.TextAlign = ContentAlignment.MiddleLeft;
		//
		// flowLayoutPanel
		//
		flowLayoutPanel.Controls.Add(btnChangelog);
		flowLayoutPanel.Controls.Add(btnReadme);
		flowLayoutPanel.Controls.Add(btnContributing);
		flowLayoutPanel.Controls.Add(btnApiDoc);
		flowLayoutPanel.Controls.Add(btnGitHub);
		flowLayoutPanel.Controls.Add(btnDonate);
		flowLayoutPanel.Controls.Add(btnWebsite);
		flowLayoutPanel.Dock = DockStyle.Fill;
		flowLayoutPanel.FlowDirection = FlowDirection.LeftToRight;
		flowLayoutPanel.Location = new Point(198, 152);
		flowLayoutPanel.Margin = new Padding(4);
		flowLayoutPanel.Name = "flowLayoutPanel";
		flowLayoutPanel.Padding = new Padding(0, 12, 0, 0);
		flowLayoutPanel.Size = new Size(354, 290);
		flowLayoutPanel.TabIndex = 23;
		flowLayoutPanel.WrapContents = true;
		//
		// btnChangelog
		//
		btnChangelog.Location = new Point(4, 15);
		btnChangelog.Margin = new Padding(4, 3, 4, 3);
		btnChangelog.Name = "btnChangelog";
		btnChangelog.Size = new Size(162, 32);
		btnChangelog.TabIndex = 0;
		btnChangelog.Text = "查看更新日志";
		btnChangelog.UseVisualStyleBackColor = true;
		btnChangelog.Click += btnChangelog_Click;
		//
		// btnReadme
		//
		btnReadme.Location = new Point(174, 15);
		btnReadme.Margin = new Padding(4, 3, 4, 3);
		btnReadme.Name = "btnReadme";
		btnReadme.Size = new Size(162, 32);
		btnReadme.TabIndex = 1;
		btnReadme.Text = "查看 README";
		btnReadme.UseVisualStyleBackColor = true;
		btnReadme.Click += btnReadme_Click;
		//
		// btnContributing
		//
		btnContributing.Location = new Point(4, 53);
		btnContributing.Margin = new Padding(4, 3, 4, 3);
		btnContributing.Name = "btnContributing";
		btnContributing.Size = new Size(162, 32);
		btnContributing.TabIndex = 2;
		btnContributing.Text = "查看贡献指南";
		btnContributing.UseVisualStyleBackColor = true;
		btnContributing.Click += btnContributing_Click;
		//
		// btnApiDoc
		//
		btnApiDoc.Location = new Point(174, 53);
		btnApiDoc.Margin = new Padding(4, 3, 4, 3);
		btnApiDoc.Name = "btnApiDoc";
		btnApiDoc.Size = new Size(162, 32);
		btnApiDoc.TabIndex = 3;
		btnApiDoc.Text = "查看 API 文档";
		btnApiDoc.UseVisualStyleBackColor = true;
		btnApiDoc.Click += btnApiDoc_Click;
		//
		// btnGitHub
		//
		btnGitHub.Location = new Point(4, 91);
		btnGitHub.Margin = new Padding(4, 3, 4, 3);
		btnGitHub.Name = "btnGitHub";
		btnGitHub.Size = new Size(162, 32);
		btnGitHub.TabIndex = 4;
		btnGitHub.Text = "GitHub";
		btnGitHub.UseVisualStyleBackColor = true;
		btnGitHub.Click += btnGitHub_Click;
		//
		// btnDonate
		//
		btnDonate.Location = new Point(174, 91);
		btnDonate.Margin = new Padding(4, 3, 4, 3);
		btnDonate.Name = "btnDonate";
		btnDonate.Size = new Size(162, 32);
		btnDonate.TabIndex = 5;
		btnDonate.Text = "给作者买一杯咖啡";
		btnDonate.UseVisualStyleBackColor = true;
		btnDonate.Click += btnDonate_Click;
		//
		// btnWebsite
		//
		btnWebsite.Location = new Point(4, 129);
		btnWebsite.Margin = new Padding(4, 3, 4, 3);
		btnWebsite.Name = "btnWebsite";
		btnWebsite.Size = new Size(162, 32);
		btnWebsite.TabIndex = 6;
		btnWebsite.Text = "访问官网";
		btnWebsite.UseVisualStyleBackColor = true;
		btnWebsite.Click += btnWebsite_Click;
		//
		// okButton
		//
		okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
		okButton.DialogResult = DialogResult.Cancel;
		okButton.Font = new Font("Microsoft YaHei UI", 9F);
		okButton.Location = new Point(456, 117);
		okButton.Margin = new Padding(4);
		okButton.Name = "okButton";
		okButton.Size = new Size(96, 29);
		okButton.TabIndex = 24;
		okButton.Text = "确定(&O)";
		//
		// AboutBox
		//
		AcceptButton = okButton;
		AutoScaleDimensions = new SizeF(7F, 17F);
		AutoScaleMode = AutoScaleMode.Font;
		CancelButton = okButton;
		ClientSize = new Size(576, 470);
		Controls.Add(tableLayoutPanel);
		FormBorderStyle = FormBorderStyle.FixedDialog;
		Margin = new Padding(4);
		MaximizeBox = false;
		MinimizeBox = false;
		Name = "AboutBox";
		Padding = new Padding(10, 12, 10, 12);
		ShowIcon = false;
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.CenterParent;
		Text = "AboutBox1";
		tableLayoutPanel.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
		flowLayoutPanel.ResumeLayout(false);
		ResumeLayout(false);
	}

	#endregion

	private TableLayoutPanel tableLayoutPanel;
	private PictureBox logoPictureBox;
	private Label labelProductName;
	private Label labelVersion;
	private Label labelCopyright;
	private Label labelCompanyName;
	private FlowLayoutPanel flowLayoutPanel;
	private Button btnChangelog;
	private Button btnReadme;
	private Button btnContributing;
	private Button btnApiDoc;
	private Button btnGitHub;
	private Button btnDonate;
	private Button btnWebsite;
	private Button okButton;
}
