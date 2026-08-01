namespace fptp;

partial class GenSettingsBox
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
		btnReset = new Button();
		btnImport = new Button();
		btnExport = new Button();
		btnOk = new Button();
		btnCancel = new Button();
		groupPrivacy = new GroupBox();
		chkAllowExternal = new CheckBox();
		labelTemp = new Label();
		cmbTempMode = new ComboBox();
		groupLang = new GroupBox();
		cmbLang = new ComboBox();
		btnLangImport = new Button();
		btnLangExport = new Button();
		groupTheme = new GroupBox();
		cmbTheme = new ComboBox();
		btnThemeImport = new Button();
		btnThemeExport = new Button();
		groupUpdate = new GroupBox();
		chkAutoUpdate = new CheckBox();
		groupKey = new GroupBox();
		btnKeySettings = new Button();
		groupExport = new GroupBox();
		label9 = new Label();
		lblQualityVal = new Label();
		trackBarQuality = new TrackBar();
		groupLayout = new GroupBox();
		label5 = new Label();
		cmbLayoutPreset = new ComboBox();
		label6 = new Label();
		txtCustomW = new TextBox();
		label7 = new Label();
		txtCustomH = new TextBox();
		label8 = new Label();
		cmbGuideLine = new ComboBox();
		((System.ComponentModel.ISupportInitialize)trackBar).BeginInit();
		((System.ComponentModel.ISupportInitialize)trackBarQuality).BeginInit();
		groupPrivacy.SuspendLayout();
		groupLang.SuspendLayout();
		groupTheme.SuspendLayout();
		groupUpdate.SuspendLayout();
		groupKey.SuspendLayout();
		groupExport.SuspendLayout();
		groupLayout.SuspendLayout();
		SuspendLayout();
		//
		// label1
		//
		label1.AutoSize = true;
		label1.Location = new Point(10, 26);
		label1.Name = "label1";
		label1.Size = new Size(68, 17);
		label1.TabIndex = 0;
		label1.Text = "保存格式";
		//
		// cmbSaveFormat
		//
		cmbSaveFormat.DropDownStyle = ComboBoxStyle.DropDownList;
		cmbSaveFormat.Items.AddRange(new object[] { "JPG", "PNG", "BMP", "TIFF", "GIF" });
		cmbSaveFormat.Location = new Point(110, 22);
		cmbSaveFormat.Name = "cmbSaveFormat";
		cmbSaveFormat.Size = new Size(180, 25);
		cmbSaveFormat.TabIndex = 1;
		//
		// label2
		//
		label2.AutoSize = true;
		label2.Location = new Point(20, 19);
		label2.Name = "label2";
		label2.Size = new Size(68, 17);
		label2.TabIndex = 2;
		label2.Text = "默认尺寸";
		//
		// cmbSize
		//
		cmbSize.DropDownStyle = ComboBoxStyle.DropDownList;
		cmbSize.Location = new Point(110, 16);
		cmbSize.Name = "cmbSize";
		cmbSize.Size = new Size(121, 25);
		cmbSize.TabIndex = 3;
		//
		// label3
		//
		label3.AutoSize = true;
		label3.Location = new Point(20, 53);
		label3.Name = "label3";
		label3.Size = new Size(56, 17);
		label3.TabIndex = 4;
		label3.Text = "默认底色";
		//
		// cmbBgColor
		//
		cmbBgColor.DropDownStyle = ComboBoxStyle.DropDownList;
		cmbBgColor.Items.AddRange(new object[] { "白色", "蓝色", "红色", "透明" });
		cmbBgColor.Location = new Point(110, 50);
		cmbBgColor.Name = "cmbBgColor";
		cmbBgColor.Size = new Size(121, 25);
		cmbBgColor.TabIndex = 5;
		//
		// label4
		//
		label4.AutoSize = true;
		label4.Location = new Point(20, 91);
		label4.Name = "label4";
		label4.Size = new Size(68, 17);
		label4.TabIndex = 6;
		label4.Text = "默认容差";
		//
		// trackBar
		//
		trackBar.Location = new Point(110, 87);
		trackBar.Maximum = 150;
		trackBar.Name = "trackBar";
		trackBar.Size = new Size(160, 45);
		trackBar.TabIndex = 7;
		trackBar.Scroll += trackBar_Scroll;
		//
		// lblToleranceVal
		//
		lblToleranceVal.AutoSize = true;
		lblToleranceVal.Location = new Point(276, 91);
		lblToleranceVal.Name = "lblToleranceVal";
		lblToleranceVal.Size = new Size(16, 17);
		lblToleranceVal.TabIndex = 8;
		lblToleranceVal.Text = "0";
		//
		// groupPrivacy
		//
		groupPrivacy.Controls.Add(cmbTempMode);
		groupPrivacy.Controls.Add(labelTemp);
		groupPrivacy.Controls.Add(chkAllowExternal);
		groupPrivacy.Location = new Point(12, 128);
		groupPrivacy.Name = "groupPrivacy";
		groupPrivacy.Size = new Size(300, 80);
		groupPrivacy.TabIndex = 11;
		groupPrivacy.TabStop = false;
		groupPrivacy.Text = "隐私";
		//
		// chkAllowExternal
		//
		chkAllowExternal.Location = new Point(10, 20);
		chkAllowExternal.Name = "chkAllowExternal";
		chkAllowExternal.Size = new Size(280, 25);
		chkAllowExternal.TabIndex = 0;
		chkAllowExternal.Text = "允许外部访问数据";
		chkAllowExternal.UseVisualStyleBackColor = true;
		//
		// labelTemp
		//
		labelTemp.AutoSize = true;
		labelTemp.Location = new Point(10, 55);
		labelTemp.Name = "labelTemp";
		labelTemp.Size = new Size(68, 17);
		labelTemp.TabIndex = 1;
		labelTemp.Text = "临时文件";
		//
		// cmbTempMode
		//
		cmbTempMode.DropDownStyle = ComboBoxStyle.DropDownList;
		cmbTempMode.Location = new Point(110, 51);
		cmbTempMode.Name = "cmbTempMode";
		cmbTempMode.Size = new Size(180, 25);
		cmbTempMode.TabIndex = 2;
		//
		// groupLang
		//
		groupLang.Controls.Add(btnLangExport);
		groupLang.Controls.Add(btnLangImport);
		groupLang.Controls.Add(cmbLang);
		groupLang.Location = new Point(12, 214);
		groupLang.Name = "groupLang";
		groupLang.Size = new Size(300, 85);
		groupLang.TabIndex = 15;
		groupLang.TabStop = false;
		groupLang.Text = "界面语言";
		//
		// cmbLang
		//
		cmbLang.DropDownStyle = ComboBoxStyle.DropDownList;
		cmbLang.Location = new Point(10, 20);
		cmbLang.Name = "cmbLang";
		cmbLang.Size = new Size(280, 25);
		cmbLang.TabIndex = 0;
		//
		// btnLangImport
		//
		btnLangImport.Location = new Point(10, 51);
		btnLangImport.Name = "btnLangImport";
		btnLangImport.Size = new Size(135, 30);
		btnLangImport.TabIndex = 1;
		btnLangImport.Text = "导入语言包";
		btnLangImport.UseVisualStyleBackColor = true;
		btnLangImport.Click += BtnLangImport_Click;
		//
		// btnLangExport
		//
		btnLangExport.Location = new Point(155, 51);
		btnLangExport.Name = "btnLangExport";
		btnLangExport.Size = new Size(135, 30);
		btnLangExport.TabIndex = 2;
		btnLangExport.Text = "导出语言包";
		btnLangExport.UseVisualStyleBackColor = true;
		btnLangExport.Click += BtnLangExport_Click;
		//
		// groupTheme
		//
		groupTheme.Controls.Add(btnThemeExport);
		groupTheme.Controls.Add(btnThemeImport);
		groupTheme.Controls.Add(cmbTheme);
		groupTheme.Location = new Point(12, 305);
		groupTheme.Name = "groupTheme";
		groupTheme.Size = new Size(300, 85);
		groupTheme.TabIndex = 20;
		groupTheme.TabStop = false;
		groupTheme.Text = "界面主题";
		//
		// cmbTheme
		//
		cmbTheme.DropDownStyle = ComboBoxStyle.DropDownList;
		cmbTheme.Location = new Point(10, 20);
		cmbTheme.Name = "cmbTheme";
		cmbTheme.Size = new Size(280, 25);
		cmbTheme.TabIndex = 0;
		cmbTheme.SelectedIndexChanged += CmbTheme_SelectedIndexChanged;
		//
		// btnThemeImport
		//
		btnThemeImport.Location = new Point(10, 51);
		btnThemeImport.Name = "btnThemeImport";
		btnThemeImport.Size = new Size(135, 30);
		btnThemeImport.TabIndex = 1;
		btnThemeImport.Text = "导入主题包";
		btnThemeImport.UseVisualStyleBackColor = true;
		btnThemeImport.Click += BtnThemeImport_Click;
		//
		// btnThemeExport
		//
		btnThemeExport.Location = new Point(155, 51);
		btnThemeExport.Name = "btnThemeExport";
		btnThemeExport.Size = new Size(135, 30);
		btnThemeExport.TabIndex = 2;
		btnThemeExport.Text = "导出主题包";
		btnThemeExport.UseVisualStyleBackColor = true;
		btnThemeExport.Click += BtnThemeExport_Click;
		//
		// groupUpdate
		//
		groupUpdate.Controls.Add(chkAutoUpdate);
		groupUpdate.Location = new Point(12, 396);
		groupUpdate.Name = "groupUpdate";
		groupUpdate.Size = new Size(300, 50);
		groupUpdate.TabIndex = 16;
		groupUpdate.TabStop = false;
		groupUpdate.Text = "自动更新";
		//
		// chkAutoUpdate
		//
		chkAutoUpdate.Location = new Point(10, 20);
		chkAutoUpdate.Name = "chkAutoUpdate";
		chkAutoUpdate.Size = new Size(280, 25);
		chkAutoUpdate.TabIndex = 0;
		chkAutoUpdate.Text = "启动时自动检查更新";
		chkAutoUpdate.UseVisualStyleBackColor = true;
		//
		// groupKey
		//
		groupKey.Controls.Add(btnKeySettings);
		groupKey.Location = new Point(12, 452);
		groupKey.Name = "groupKey";
		groupKey.Size = new Size(300, 60);
		groupKey.TabIndex = 19;
		groupKey.TabStop = false;
		groupKey.Text = "快捷键";
		//
		// btnKeySettings
		//
		btnKeySettings.Location = new Point(10, 22);
		btnKeySettings.Name = "btnKeySettings";
		btnKeySettings.Size = new Size(280, 30);
		btnKeySettings.TabIndex = 0;
		btnKeySettings.Text = "调整快捷键…";
		btnKeySettings.UseVisualStyleBackColor = true;
		btnKeySettings.Click += BtnKeySettings_Click;
		//
		// groupExport
		//
		groupExport.Controls.Add(label9);
		groupExport.Controls.Add(lblQualityVal);
		groupExport.Controls.Add(trackBarQuality);
		groupExport.Controls.Add(label1);
		groupExport.Controls.Add(cmbSaveFormat);
		groupExport.Location = new Point(12, 518);
		groupExport.Name = "groupExport";
		groupExport.Size = new Size(300, 95);
		groupExport.TabIndex = 18;
		groupExport.TabStop = false;
		groupExport.Text = "导出";
		//
		// label9
		//
		label9.AutoSize = true;
		label9.Location = new Point(10, 61);
		label9.Name = "label9";
		label9.Size = new Size(68, 17);
		label9.TabIndex = 8;
		label9.Text = "JPEG质量";
		//
		// lblQualityVal
		//
		lblQualityVal.AutoSize = true;
		lblQualityVal.Location = new Point(276, 61);
		lblQualityVal.Name = "lblQualityVal";
		lblQualityVal.Size = new Size(25, 17);
		lblQualityVal.TabIndex = 10;
		lblQualityVal.Text = "100";
		//
		// trackBarQuality
		//
		trackBarQuality.Location = new Point(110, 56);
		trackBarQuality.Maximum = 100;
		trackBarQuality.Minimum = 70;
		trackBarQuality.Name = "trackBarQuality";
		trackBarQuality.Size = new Size(160, 45);
		trackBarQuality.TabIndex = 9;
		trackBarQuality.Value = 100;
		trackBarQuality.Scroll += trackBarQuality_Scroll;
		//
		// groupLayout
		//
		groupLayout.Controls.Add(label8);
		groupLayout.Controls.Add(cmbGuideLine);
		groupLayout.Controls.Add(label7);
		groupLayout.Controls.Add(txtCustomH);
		groupLayout.Controls.Add(label6);
		groupLayout.Controls.Add(txtCustomW);
		groupLayout.Controls.Add(cmbLayoutPreset);
		groupLayout.Controls.Add(label5);
		groupLayout.Location = new Point(12, 619);
		groupLayout.Name = "groupLayout";
		groupLayout.Size = new Size(300, 155);
		groupLayout.TabIndex = 17;
		groupLayout.TabStop = false;
		groupLayout.Text = "排版尺寸";
		//
		// label5
		//
		label5.AutoSize = true;
		label5.Location = new Point(10, 24);
		label5.Name = "label5";
		label5.Size = new Size(56, 17);
		label5.TabIndex = 0;
		label5.Text = "预设尺寸";
		//
		// cmbLayoutPreset
		//
		cmbLayoutPreset.DropDownStyle = ComboBoxStyle.DropDownList;
		cmbLayoutPreset.Location = new Point(110, 20);
		cmbLayoutPreset.Name = "cmbLayoutPreset";
		cmbLayoutPreset.Size = new Size(180, 25);
		cmbLayoutPreset.TabIndex = 1;
		//
		// label6
		//
		label6.AutoSize = true;
		label6.Location = new Point(10, 57);
		label6.Name = "label6";
		label6.Size = new Size(56, 17);
		label6.TabIndex = 2;
		label6.Text = "自定义宽";
		//
		// txtCustomW
		//
		txtCustomW.Location = new Point(110, 54);
		txtCustomW.Name = "txtCustomW";
		txtCustomW.Size = new Size(180, 25);
		txtCustomW.TabIndex = 3;
		txtCustomW.Text = "1500";
		//
		// label7
		//
		label7.AutoSize = true;
		label7.Location = new Point(10, 92);
		label7.Name = "label7";
		label7.Size = new Size(56, 17);
		label7.TabIndex = 4;
		label7.Text = "自定义高";
		//
		// txtCustomH
		//
		txtCustomH.Location = new Point(110, 89);
		txtCustomH.Name = "txtCustomH";
		txtCustomH.Size = new Size(180, 25);
		txtCustomH.TabIndex = 5;
		txtCustomH.Text = "1050";
		//
		// label8
		//
		label8.AutoSize = true;
		label8.Location = new Point(10, 127);
		label8.Name = "label8";
		label8.Size = new Size(56, 17);
		label8.TabIndex = 6;
		label8.Text = "辅助线";
		//
		// cmbGuideLine
		//
		cmbGuideLine.DropDownStyle = ComboBoxStyle.DropDownList;
		cmbGuideLine.Location = new Point(110, 123);
		cmbGuideLine.Name = "cmbGuideLine";
		cmbGuideLine.Size = new Size(180, 25);
		cmbGuideLine.TabIndex = 7;
		//
		// btnReset
		//
		btnReset.Location = new Point(12, 784);
		btnReset.Name = "btnReset";
		btnReset.Size = new Size(96, 29);
		btnReset.TabIndex = 12;
		btnReset.Text = "重置设置";
		btnReset.UseVisualStyleBackColor = true;
		btnReset.Click += BtnReset_Click;
		//
		// btnImport
		//
		btnImport.Location = new Point(114, 784);
		btnImport.Name = "btnImport";
		btnImport.Size = new Size(96, 29);
		btnImport.TabIndex = 13;
		btnImport.Text = "导入设置";
		btnImport.UseVisualStyleBackColor = true;
		btnImport.Click += BtnImport_Click;
		//
		// btnExport
		//
		btnExport.Location = new Point(216, 784);
		btnExport.Name = "btnExport";
		btnExport.Size = new Size(96, 29);
		btnExport.TabIndex = 14;
		btnExport.Text = "导出设置";
		btnExport.UseVisualStyleBackColor = true;
		btnExport.Click += BtnExport_Click;
		//
		// btnOk
		//
		btnOk.Location = new Point(110, 824);
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
		btnCancel.Location = new Point(210, 824);
		btnCancel.Name = "btnCancel";
		btnCancel.Size = new Size(85, 30);
		btnCancel.TabIndex = 10;
		btnCancel.Text = "取消";
		btnCancel.UseVisualStyleBackColor = true;
		btnCancel.Click += btnCancel_Click;
		//
		// 控件添加到窗体
		//
		Controls.Add(btnExport);
		Controls.Add(btnImport);
		Controls.Add(btnReset);
		Controls.Add(groupLayout);
		Controls.Add(groupExport);
		Controls.Add(groupUpdate);
		Controls.Add(groupKey);
		Controls.Add(groupTheme);
		Controls.Add(groupLang);
		Controls.Add(groupPrivacy);
		Controls.Add(btnCancel);
		Controls.Add(btnOk);
		Controls.Add(lblToleranceVal);
		Controls.Add(trackBar);
		Controls.Add(cmbBgColor);
		Controls.Add(label4);
		Controls.Add(cmbSize);
		Controls.Add(label3);
		Controls.Add(label2);
		//
		// SettingsBox
		//
		AcceptButton = btnOk;
		AutoScaleDimensions = new SizeF(7F, 17F);
		AutoScaleMode = AutoScaleMode.Font;
		CancelButton = btnCancel;
		ClientSize = new Size(324, 882);
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
		((System.ComponentModel.ISupportInitialize)trackBarQuality).EndInit();
		groupPrivacy.ResumeLayout(false);
		groupLang.ResumeLayout(false);
		groupTheme.ResumeLayout(false);
		groupUpdate.ResumeLayout(false);
		groupKey.ResumeLayout(false);
		groupExport.ResumeLayout(false);
		groupExport.PerformLayout();
		groupLayout.ResumeLayout(false);
		groupLayout.PerformLayout();
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
	private GroupBox groupPrivacy;
	private CheckBox chkAllowExternal;
	private Label labelTemp;
	private ComboBox cmbTempMode;
	private Button btnReset;
	private Button btnImport;
	private Button btnExport;
	private GroupBox groupLang;
	private ComboBox cmbLang;
	private Button btnLangImport;
	private Button btnLangExport;
	private GroupBox groupTheme;
	private ComboBox cmbTheme;
	private Button btnThemeImport;
	private Button btnThemeExport;
	private GroupBox groupUpdate;
	private CheckBox chkAutoUpdate;
	private GroupBox groupKey;
	private Button btnKeySettings;
	private GroupBox groupExport;
	private GroupBox groupLayout;
	private Label label5;
	private ComboBox cmbLayoutPreset;
	private Label label6;
	private TextBox txtCustomW;
	private Label label7;
	private TextBox txtCustomH;
	private Label label8;
	private ComboBox cmbGuideLine;
	private Label label9;
	private TrackBar trackBarQuality;
	private Label lblQualityVal;
}
