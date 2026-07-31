namespace fptp;

partial class BatchBox
{
	/// <summary>
	///  Required designer variable.
	/// </summary>
	private System.ComponentModel.IContainer components = null;

	/// <summary>
	///  Clean up any resources being used.
	/// </summary>
	protected override void Dispose(bool disposing)
	{
		if (disposing && (components != null))
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	#region Windows Form Designer generated code

	private void InitializeComponent()
	{
		btnInput = new Button();
		txtInput = new TextBox();
		btnOutput = new Button();
		txtOutput = new TextBox();
		chkCrop = new CheckBox();
		chkGrayscale = new CheckBox();
		chkChangeBg = new CheckBox();
		chkLayout = new CheckBox();
		cmbBgColor = new ComboBox();
		lblBg = new Label();
		trkTolerance = new TrackBar();
		lblTolerance = new Label();
		lblValue = new Label();
		cmbLayout = new ComboBox();
		progressBar = new ProgressBar();
		lblProgress = new Label();
		btnStart = new Button();
		btnCancel = new Button();
		((System.ComponentModel.ISupportInitialize)trkTolerance).BeginInit();
		SuspendLayout();
		// 
		// btnInput
		// 
		btnInput.Location = new Point(12, 16);
		btnInput.Name = "btnInput";
		btnInput.Size = new Size(90, 30);
		btnInput.TabIndex = 0;
		btnInput.Text = "输入文件夹…";
		btnInput.UseVisualStyleBackColor = true;
		btnInput.Click += BtnInput_Click;
		// 
		// txtInput
		// 
		txtInput.Location = new Point(108, 18);
		txtInput.Name = "txtInput";
		txtInput.ReadOnly = true;
		txtInput.Size = new Size(424, 23);
		txtInput.TabIndex = 1;
		// 
		// btnOutput
		// 
		btnOutput.Location = new Point(12, 56);
		btnOutput.Name = "btnOutput";
		btnOutput.Size = new Size(90, 30);
		btnOutput.TabIndex = 2;
		btnOutput.Text = "输出文件夹…";
		btnOutput.UseVisualStyleBackColor = true;
		btnOutput.Click += BtnOutput_Click;
		// 
		// txtOutput
		// 
		txtOutput.Location = new Point(108, 58);
		txtOutput.Name = "txtOutput";
		txtOutput.ReadOnly = true;
		txtOutput.Size = new Size(424, 23);
		txtOutput.TabIndex = 3;
		// 
		// chkCrop
		// 
		chkCrop.AutoSize = true;
		chkCrop.Location = new Point(16, 106);
		chkCrop.Name = "chkCrop";
		chkCrop.Size = new Size(90, 21);
		chkCrop.TabIndex = 4;
		chkCrop.Text = "智能裁剪";
		chkCrop.UseVisualStyleBackColor = true;
		// 
		// chkGrayscale
		// 
		chkGrayscale.AutoSize = true;
		chkGrayscale.Location = new Point(112, 106);
		chkGrayscale.Name = "chkGrayscale";
		chkGrayscale.Size = new Size(78, 21);
		chkGrayscale.TabIndex = 5;
		chkGrayscale.Text = "变黑白";
		chkGrayscale.UseVisualStyleBackColor = true;
		// 
		// chkChangeBg
		// 
		chkChangeBg.AutoSize = true;
		chkChangeBg.Location = new Point(196, 106);
		chkChangeBg.Name = "chkChangeBg";
		chkChangeBg.Size = new Size(90, 21);
		chkChangeBg.TabIndex = 6;
		chkChangeBg.Text = "修改底色";
		chkChangeBg.UseVisualStyleBackColor = true;
		// 
		// chkLayout
		// 
		chkLayout.AutoSize = true;
		chkLayout.Location = new Point(292, 106);
		chkLayout.Name = "chkLayout";
		chkLayout.Size = new Size(60, 21);
		chkLayout.TabIndex = 7;
		chkLayout.Text = "排版";
		chkLayout.UseVisualStyleBackColor = true;
		// 
		// lblBg
		// 
		lblBg.AutoSize = true;
		lblBg.Location = new Point(16, 146);
		lblBg.Name = "lblBg";
		lblBg.Size = new Size(56, 17);
		lblBg.TabIndex = 8;
		lblBg.Text = "底色:";
		// 
		// cmbBgColor
		// 
		cmbBgColor.DropDownStyle = ComboBoxStyle.DropDownList;
		cmbBgColor.FormattingEnabled = true;
			cmbBgColor.Items.AddRange(new object[] { "蓝色", "红色", "白色", "透明" });
		cmbBgColor.Location = new Point(64, 142);
		cmbBgColor.Name = "cmbBgColor";
		cmbBgColor.Size = new Size(90, 25);
		cmbBgColor.TabIndex = 9;
		// 
		// lblTolerance
		// 
		lblTolerance.AutoSize = true;
		lblTolerance.Location = new Point(170, 146);
		lblTolerance.Name = "lblTolerance";
		lblTolerance.Size = new Size(68, 17);
		lblTolerance.TabIndex = 10;
		lblTolerance.Text = "算法灵敏度";
		// 
		// trkTolerance
		// 
		trkTolerance.Location = new Point(240, 136);
		trkTolerance.Maximum = 150;
		trkTolerance.Name = "trkTolerance";
		trkTolerance.Size = new Size(220, 45);
		trkTolerance.TabIndex = 11;
		trkTolerance.Value = 60;
		trkTolerance.Scroll += trkTolerance_Scroll;
		// 
		// lblValue
		// 
		lblValue.AutoSize = true;
		lblValue.Location = new Point(466, 146);
		lblValue.Name = "lblValue";
		lblValue.Size = new Size(17, 17);
		lblValue.TabIndex = 12;
		lblValue.Text = "60";
		// 
		// cmbLayout
		// 
		cmbLayout.DropDownStyle = ComboBoxStyle.DropDownList;
		cmbLayout.FormattingEnabled = true;
		cmbLayout.Location = new Point(64, 182);
		cmbLayout.Name = "cmbLayout";
		cmbLayout.Size = new Size(180, 25);
		cmbLayout.TabIndex = 13;
		// 
		// progressBar
		// 
		progressBar.Location = new Point(16, 224);
		progressBar.Name = "progressBar";
		progressBar.Size = new Size(516, 20);
		progressBar.TabIndex = 14;
		// 
		// lblProgress
		// 
		lblProgress.AutoSize = true;
		lblProgress.Location = new Point(16, 252);
		lblProgress.Name = "lblProgress";
		lblProgress.Size = new Size(0, 17);
		lblProgress.TabIndex = 15;
		// 
		// btnStart
		// 
		btnStart.Location = new Point(356, 290);
		btnStart.Name = "btnStart";
		btnStart.Size = new Size(90, 34);
		btnStart.TabIndex = 16;
		btnStart.Text = "开始处理";
		btnStart.UseVisualStyleBackColor = true;
		btnStart.Click += BtnStart_Click;
		// 
		// btnCancel
		// 
		btnCancel.DialogResult = DialogResult.Cancel;
		btnCancel.Location = new Point(452, 290);
		btnCancel.Name = "btnCancel";
		btnCancel.Size = new Size(80, 34);
		btnCancel.TabIndex = 17;
		btnCancel.Text = "取消";
		btnCancel.UseVisualStyleBackColor = true;
		// 
		// BatchBox
		// 
		AutoScaleDimensions = new SizeF(7F, 17F);
		AutoScaleMode = AutoScaleMode.Font;
		CancelButton = btnCancel;
		ClientSize = new Size(548, 340);
		Controls.Add(btnCancel);
		Controls.Add(btnStart);
		Controls.Add(lblProgress);
		Controls.Add(progressBar);
		Controls.Add(cmbLayout);
		Controls.Add(lblValue);
		Controls.Add(trkTolerance);
		Controls.Add(lblTolerance);
		Controls.Add(cmbBgColor);
		Controls.Add(lblBg);
		Controls.Add(chkLayout);
		Controls.Add(chkChangeBg);
		Controls.Add(chkGrayscale);
		Controls.Add(chkCrop);
		Controls.Add(txtOutput);
		Controls.Add(btnOutput);
		Controls.Add(txtInput);
		Controls.Add(btnInput);
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		Name = "BatchBox";
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.CenterParent;
		Text = "文件夹批处理";
		Load += BatchBox_Load;
		((System.ComponentModel.ISupportInitialize)trkTolerance).EndInit();
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private Button btnInput;
	private TextBox txtInput;
	private Button btnOutput;
	private TextBox txtOutput;
	private CheckBox chkCrop;
	private CheckBox chkGrayscale;
	private CheckBox chkChangeBg;
	private CheckBox chkLayout;
	private ComboBox cmbBgColor;
	private Label lblBg;
	private TrackBar trkTolerance;
	private Label lblTolerance;
	private Label lblValue;
	private ComboBox cmbLayout;
	private ProgressBar progressBar;
	private Label lblProgress;
	private Button btnStart;
	private Button btnCancel;
}
