namespace fptp;

partial class SettingsBox
{
	private System.ComponentModel.IContainer components = null;

	protected override void Dispose(bool disposing)
	{
		if (disposing && (components != null))
			components.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		label1 = new Label();
		cmbSaveFormat = new ComboBox();
		label2 = new Label();
		cmbSize = new ComboBox();
		label3 = new Label();
		cmbBgColor = new ComboBox();
		label4 = new Label();
		trackBar = new TrackBar();
		lblToleranceVal = new Label();
		btnOk = new Button();
		btnCancel = new Button();
		((System.ComponentModel.ISupportInitialize)trackBar).BeginInit();
		SuspendLayout();
		//
		// label1
		//
		label1.AutoSize = true;
		label1.Location = new Point(20, 22);
		label1.Name = "label1";
		label1.Size = new Size(68, 17);
		label1.TabIndex = 0;
		label1.Text = "保存格式";
		//
		// cmbSaveFormat
		//
		cmbSaveFormat.DropDownStyle = ComboBoxStyle.DropDownList;
		cmbSaveFormat.Items.AddRange(new object[] { "JPG", "PNG" });
		cmbSaveFormat.Location = new Point(110, 19);
		cmbSaveFormat.Name = "cmbSaveFormat";
		cmbSaveFormat.Size = new Size(121, 25);
		cmbSaveFormat.TabIndex = 1;
		//
		// label2
		//
		label2.AutoSize = true;
		label2.Location = new Point(20, 56);
		label2.Name = "label2";
		label2.Size = new Size(68, 17);
		label2.TabIndex = 2;
		label2.Text = "默认尺寸";
		//
		// cmbSize
		//
		cmbSize.DropDownStyle = ComboBoxStyle.DropDownList;
		cmbSize.Items.AddRange(new object[] { "一寸", "二寸", "小二寸" });
		cmbSize.Location = new Point(110, 53);
		cmbSize.Name = "cmbSize";
		cmbSize.Size = new Size(121, 25);
		cmbSize.TabIndex = 3;
		//
		// label3
		//
		label3.AutoSize = true;
		label3.Location = new Point(20, 90);
		label3.Name = "label3";
		label3.Size = new Size(56, 17);
		label3.TabIndex = 4;
		label3.Text = "默认底色";
		//
		// cmbBgColor
		//
		cmbBgColor.DropDownStyle = ComboBoxStyle.DropDownList;
		cmbBgColor.Items.AddRange(new object[] { "白色", "蓝色", "红色" });
		cmbBgColor.Location = new Point(110, 87);
		cmbBgColor.Name = "cmbBgColor";
		cmbBgColor.Size = new Size(121, 25);
		cmbBgColor.TabIndex = 5;
		//
		// label4
		//
		label4.AutoSize = true;
		label4.Location = new Point(20, 128);
		label4.Name = "label4";
		label4.Size = new Size(68, 17);
		label4.TabIndex = 6;
		label4.Text = "默认容差";
		//
		// trackBar
		//
		trackBar.Location = new Point(110, 124);
		trackBar.Maximum = 150;
		trackBar.Name = "trackBar";
		trackBar.Size = new Size(160, 45);
		trackBar.TabIndex = 7;
		trackBar.Scroll += trackBar_Scroll;
		//
		// lblToleranceVal
		//
		lblToleranceVal.AutoSize = true;
		lblToleranceVal.Location = new Point(276, 128);
		lblToleranceVal.Name = "lblToleranceVal";
		lblToleranceVal.Size = new Size(16, 17);
		lblToleranceVal.TabIndex = 8;
		lblToleranceVal.Text = "0";
		//
		// btnOk
		//
		btnOk.Location = new Point(110, 180);
		btnOk.Name = "btnOk";
		btnOk.Size = new Size(85, 30);
		btnOk.TabIndex = 9;
		btnOk.Text = "确定";
		btnOk.UseVisualStyleBackColor = true;
		btnOk.Click += btnOk_Click;
		//
		// btnCancel
		//
		btnCancel.DialogResult = DialogResult.Cancel;
		btnCancel.Location = new Point(210, 180);
		btnCancel.Name = "btnCancel";
		btnCancel.Size = new Size(85, 30);
		btnCancel.TabIndex = 10;
		btnCancel.Text = "取消";
		btnCancel.UseVisualStyleBackColor = true;
		btnCancel.Click += btnCancel_Click;
		//
		// SettingsBox
		//
		AcceptButton = btnOk;
		AutoScaleDimensions = new SizeF(7F, 17F);
		AutoScaleMode = AutoScaleMode.Font;
		CancelButton = btnCancel;
		ClientSize = new Size(324, 230);
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		Name = "SettingsBox";
		ShowIcon = false;
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.CenterParent;
		Text = "设置";
		Load += SettingsBox_Load;
		((System.ComponentModel.ISupportInitialize)trackBar).EndInit();
		ResumeLayout(false);
		PerformLayout();
	}

	private Label label1;
	private ComboBox cmbSaveFormat;
	private Label label2;
	private ComboBox cmbSize;
	private Label label3;
	private ComboBox cmbBgColor;
	private Label label4;
	private TrackBar trackBar;
	private Label lblToleranceVal;
	private Button btnOk;
	private Button btnCancel;
}
