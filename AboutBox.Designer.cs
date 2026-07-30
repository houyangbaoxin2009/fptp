namespace fptp;

partial class AboutBox
{
	/// <summary>
	/// 必需的设计器变量。
	/// </summary>
	private System.ComponentModel.IContainer components = null;

	/// <summary>
	/// 清理所有正在使用的资源。
	/// </summary>
	protected override void Dispose(bool disposing)
	{
		if (disposing && (components != null))
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	#region Windows 窗体设计器生成的代码

	/// <summary>
	/// 设计器支持所需的方法 - 不要修改
	/// 使用代码编辑器修改此方法的内容。
	/// </summary>
	private void InitializeComponent()
	{
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutBox));
		tableLayoutPanel = new TableLayoutPanel();
		logoPictureBox = new PictureBox();
		labelProductName = new Label();
		labelVersion = new Label();
		labelCopyright = new Label();
		labelCompanyName = new Label();
		okButton = new Button();
		tableLayoutPanel.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
		SuspendLayout();
		// 
		// tableLayoutPanel
		// 
		tableLayoutPanel.ColumnCount = 2;
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 67F));
		tableLayoutPanel.Controls.Add(logoPictureBox, 0, 0);
		tableLayoutPanel.Controls.Add(labelProductName, 1, 0);
		tableLayoutPanel.Controls.Add(labelVersion, 1, 1);
		tableLayoutPanel.Controls.Add(labelCopyright, 1, 2);
		tableLayoutPanel.Controls.Add(labelCompanyName, 1, 3);
		tableLayoutPanel.Controls.Add(okButton, 1, 5);
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.Location = new Point(10, 12);
		tableLayoutPanel.Margin = new Padding(4);
		tableLayoutPanel.Name = "tableLayoutPanel";
		tableLayoutPanel.RowCount = 6;
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
		tableLayoutPanel.Size = new Size(487, 346);
		tableLayoutPanel.TabIndex = 0;
		tableLayoutPanel.Paint += tableLayoutPanel_Paint;
		// 
		// logoPictureBox
		// 
		logoPictureBox.Dock = DockStyle.Fill;
		logoPictureBox.Image = (Image)resources.GetObject("logoPictureBox.Image");
		logoPictureBox.Location = new Point(4, 4);
		logoPictureBox.Margin = new Padding(4);
		logoPictureBox.Name = "logoPictureBox";
		tableLayoutPanel.SetRowSpan(logoPictureBox, 6);
		logoPictureBox.Size = new Size(152, 338);
		logoPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
		logoPictureBox.TabIndex = 12;
		logoPictureBox.TabStop = false;
		// 
		// labelProductName
		// 
		labelProductName.Dock = DockStyle.Fill;
		labelProductName.Location = new Point(167, 0);
		labelProductName.Margin = new Padding(7, 0, 4, 0);
		labelProductName.MaximumSize = new Size(0, 22);
		labelProductName.Name = "labelProductName";
		labelProductName.Size = new Size(316, 22);
		labelProductName.TabIndex = 19;
		labelProductName.Text = "产品名称";
		labelProductName.TextAlign = ContentAlignment.MiddleLeft;
		labelProductName.Click += labelProductName_Click;
		// 
		// labelVersion
		// 
		labelVersion.Dock = DockStyle.Fill;
		labelVersion.Location = new Point(167, 34);
		labelVersion.Margin = new Padding(7, 0, 4, 0);
		labelVersion.MaximumSize = new Size(0, 22);
		labelVersion.Name = "labelVersion";
		labelVersion.Size = new Size(316, 22);
		labelVersion.TabIndex = 0;
		labelVersion.Text = "版本";
		labelVersion.TextAlign = ContentAlignment.MiddleLeft;
		// 
		// labelCopyright
		// 
		labelCopyright.Dock = DockStyle.Fill;
		labelCopyright.Location = new Point(167, 68);
		labelCopyright.Margin = new Padding(7, 0, 4, 0);
		labelCopyright.MaximumSize = new Size(0, 22);
		labelCopyright.Name = "labelCopyright";
		labelCopyright.Size = new Size(316, 22);
		labelCopyright.TabIndex = 21;
		labelCopyright.Text = "版权";
		labelCopyright.TextAlign = ContentAlignment.MiddleLeft;
		// 
		// labelCompanyName
		// 
		labelCompanyName.Dock = DockStyle.Fill;
		labelCompanyName.Location = new Point(167, 102);
		labelCompanyName.Margin = new Padding(7, 0, 4, 0);
		labelCompanyName.MaximumSize = new Size(0, 22);
		labelCompanyName.Name = "labelCompanyName";
		labelCompanyName.Size = new Size(316, 22);
		labelCompanyName.TabIndex = 22;
		labelCompanyName.Text = "公司名称";
		labelCompanyName.TextAlign = ContentAlignment.MiddleLeft;
		// 
		// okButton
		// 
		okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
		okButton.DialogResult = DialogResult.Cancel;
		okButton.Location = new Point(395, 313);
		okButton.Margin = new Padding(4);
		okButton.Name = "okButton";
		okButton.Size = new Size(88, 29);
		okButton.TabIndex = 24;
		okButton.Text = "确定(&O)";
		// 
		// AboutBox
		// 
		AcceptButton = okButton;
		AutoScaleDimensions = new SizeF(7F, 17F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(507, 370);
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
		((System.ComponentModel.ISupportInitialize)logoPictureBox).EndInit();
		ResumeLayout(false);

	}

	#endregion

	private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
	private System.Windows.Forms.PictureBox logoPictureBox;
	private System.Windows.Forms.Label labelProductName;
	private System.Windows.Forms.Label labelVersion;
	private System.Windows.Forms.Label labelCopyright;
	private System.Windows.Forms.Label labelCompanyName;
	private System.Windows.Forms.Button okButton;
}
