namespace fptp;

partial class mainBox
{
	/// <summary>
	///  Required designer variable.
	/// </summary>
	private System.ComponentModel.IContainer components = null;

	/// <summary>
	///  Clean up any resources being used.
	/// </summary>
	/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
	protected override void Dispose(bool disposing)
	{
		if (disposing && (components != null))
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	#region Windows Form Designer generated code

	/// <summary>
	///  Required method for Designer support - do not modify
	///  the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent()
	{
		pictureBox1 = new PictureBox();
		btnLoad = new Button();
		btnBlackWhite = new Button();
		btnLayout5 = new Button();
		btnSave = new Button();
		btnSettings = new Button();
		lblInfo = new Label();
		btnAbout = new Button();
		groupBox1 = new GroupBox();
		btnLayout6 = new Button();
		groupBox2 = new GroupBox();
		btnAutoCrop = new Button();
		label1 = new Label();
		TrackBar = new TrackBar();
		btnChangeBg = new Button();
		cmbBgColor = new ComboBox();
		groupBox3 = new GroupBox();
		groupBox4 = new GroupBox();
		btnUnload = new Button();
		((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
		groupBox1.SuspendLayout();
		groupBox2.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)TrackBar).BeginInit();
		groupBox3.SuspendLayout();
		groupBox4.SuspendLayout();
		SuspendLayout();
		// 
		// pictureBox1
		// 
		pictureBox1.BackColor = SystemColors.ActiveCaption;
		pictureBox1.Location = new Point(12, 12);
		pictureBox1.Name = "pictureBox1";
		pictureBox1.Size = new Size(341, 476);
		pictureBox1.TabIndex = 0;
		pictureBox1.TabStop = false;
		// 
		// btnLoad
		// 
		btnLoad.Location = new Point(6, 22);
		btnLoad.Name = "btnLoad";
		btnLoad.Size = new Size(75, 29);
		btnLoad.TabIndex = 1;
		btnLoad.Text = "本地图片";
		btnLoad.UseVisualStyleBackColor = true;
		btnLoad.Click += BtnLoad_Click;
		// 
		// btnBlackWhite
		// 
		btnBlackWhite.Location = new Point(86, 22);
		btnBlackWhite.Name = "btnBlackWhite";
		btnBlackWhite.Size = new Size(75, 29);
		btnBlackWhite.TabIndex = 2;
		btnBlackWhite.Text = "变黑白";
		btnBlackWhite.UseVisualStyleBackColor = true;
		btnBlackWhite.Click += BtnBlackWhite_Click;
		// 
		// btnLayout5
		// 
		btnLayout5.Location = new Point(6, 22);
		btnLayout5.Name = "btnLayout5";
		btnLayout5.Size = new Size(75, 29);
		btnLayout5.TabIndex = 3;
		btnLayout5.Text = "5寸排版";
		btnLayout5.UseVisualStyleBackColor = true;
		btnLayout5.Click += BtnLayout5_Click;
		// 
		//
		// lblInfo
		//
		lblInfo.AutoSize = true;
		lblInfo.Location = new Point(387, 492);
		lblInfo.Name = "lblInfo";
		lblInfo.Size = new Size(0, 17);
		lblInfo.TabIndex = 5;
		//
		// groupBox1
		// 
		groupBox1.Controls.Add(btnLayout6);
		groupBox1.Controls.Add(btnLayout5);
		groupBox1.Location = new Point(373, 226);
		groupBox1.Name = "groupBox1";
		groupBox1.Size = new Size(299, 103);
		groupBox1.TabIndex = 7;
		groupBox1.TabStop = false;
		groupBox1.Text = "排版";
		// 
		// btnLayout6
		// 
		btnLayout6.Location = new Point(87, 22);
		btnLayout6.Name = "btnLayout6";
		btnLayout6.Size = new Size(75, 29);
		btnLayout6.TabIndex = 4;
		btnLayout6.Text = "6寸排版";
		btnLayout6.UseVisualStyleBackColor = true;
		btnLayout6.Click += BtnLayout6_Click;
		// 
		// groupBox2
		// 
		groupBox2.Controls.Add(btnAutoCrop);
		groupBox2.Controls.Add(label1);
		groupBox2.Controls.Add(TrackBar);
		groupBox2.Controls.Add(btnChangeBg);
		groupBox2.Controls.Add(cmbBgColor);
		groupBox2.Controls.Add(btnBlackWhite);
		groupBox2.Location = new Point(373, 78);
		groupBox2.Name = "groupBox2";
		groupBox2.Size = new Size(299, 142);
		groupBox2.TabIndex = 8;
		groupBox2.TabStop = false;
		groupBox2.Text = "预处理";
		groupBox2.Enter += groupBox2_Enter;
		// 
		// btnAutoCrop
		// 
		btnAutoCrop.Location = new Point(6, 22);
		btnAutoCrop.Name = "btnAutoCrop";
		btnAutoCrop.Size = new Size(75, 29);
		btnAutoCrop.TabIndex = 7;
		btnAutoCrop.Text = "智能裁剪";
		btnAutoCrop.UseVisualStyleBackColor = true;
		btnAutoCrop.Click += BtnAutoCrop_Click;
		// 
		// label1
		// 
		label1.AutoSize = true;
		label1.Location = new Point(12, 100);
		label1.Name = "label1";
		label1.Size = new Size(68, 17);
		label1.TabIndex = 6;
		label1.Text = "算法灵敏度";
		label1.Click += label1_Click;
		// 
		// TrackBar
		// 
		TrackBar.Location = new Point(87, 91);
		TrackBar.Maximum = 150;
		TrackBar.Name = "TrackBar";
		TrackBar.Size = new Size(104, 45);
		TrackBar.TabIndex = 5;
		TrackBar.Value = 60;
		TrackBar.Scroll += TrackBar_Scroll;
		// 
		// btnChangeBg
		// 
		btnChangeBg.Location = new Point(5, 57);
		btnChangeBg.Name = "btnChangeBg";
		btnChangeBg.Size = new Size(75, 29);
		btnChangeBg.TabIndex = 4;
		btnChangeBg.Text = "修改底色";
		btnChangeBg.UseVisualStyleBackColor = true;
		btnChangeBg.Click += BtnChangeBg_Click;
		// 
		// cmbBgColor
		// 
		cmbBgColor.FormattingEnabled = true;
		cmbBgColor.Items.AddRange(new object[] { "蓝色", "红色", "白色" });
		cmbBgColor.Location = new Point(86, 57);
		cmbBgColor.Name = "cmbBgColor";
		cmbBgColor.Size = new Size(121, 25);
		cmbBgColor.TabIndex = 3;
		cmbBgColor.Text = "蓝色";
		cmbBgColor.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
		// 
		// groupBox3
		// 
		groupBox3.Controls.Add(btnLoad);
		groupBox3.Location = new Point(372, 12);
		groupBox3.Name = "groupBox3";
		groupBox3.Size = new Size(300, 63);
		groupBox3.TabIndex = 9;
		groupBox3.TabStop = false;
		groupBox3.Text = "导入";
		groupBox3.Enter += groupBox3_Enter;
		// 
		// groupBox4
		// 
		groupBox4.Controls.Add(btnSettings);
		groupBox4.Controls.Add(btnUnload);
		groupBox4.Controls.Add(btnSave);
		groupBox4.Controls.Add(btnAbout);
		groupBox4.Location = new Point(372, 335);
		groupBox4.Name = "groupBox4";
		groupBox4.Size = new Size(300, 90);
		groupBox4.TabIndex = 10;
		groupBox4.TabStop = false;
		groupBox4.Text = "完成";
		groupBox4.Enter += groupBox4_Enter;
		// 
		// btnSave
		// 
		btnSave.Location = new Point(6, 22);
		btnSave.Name = "btnSave";
		btnSave.Size = new Size(138, 29);
		btnSave.TabIndex = 4;
		btnSave.Text = "导出本地";
		btnSave.UseVisualStyleBackColor = true;
		btnSave.Click += BtnSave_Click;
		// 
		// btnUnload
		// 
		btnUnload.Location = new Point(150, 22);
		btnUnload.Name = "btnUnload";
		btnUnload.Size = new Size(138, 29);
		btnUnload.TabIndex = 7;
		btnUnload.Text = "卸载图片";
		btnUnload.UseVisualStyleBackColor = true;
		btnUnload.Click += BtnUnload_Click;
		// 
		// btnAbout
		// 
		btnAbout.Location = new Point(6, 55);
		btnAbout.Name = "btnAbout";
		btnAbout.Size = new Size(138, 29);
		btnAbout.TabIndex = 6;
		btnAbout.Text = "关于";
		btnAbout.UseVisualStyleBackColor = true;
		btnAbout.Click += BtnAbout_Click;
		// 
		// btnSettings
		// 
		btnSettings.Location = new Point(150, 55);
		btnSettings.Name = "btnSettings";
		btnSettings.Size = new Size(138, 29);
		btnSettings.TabIndex = 8;
		btnSettings.Text = "设置";
		btnSettings.UseVisualStyleBackColor = true;
		btnSettings.Click += BtnSettings_Click;
		// 
		// btnUndo
		// 
		btnUndo = new Button();
		btnUndo.Enabled = false;
		btnUndo.Location = new Point(6, 22);
		btnUndo.Name = "btnUndo";
		btnUndo.Size = new Size(138, 29);
		btnUndo.TabIndex = 0;
		btnUndo.Text = "撤回";
		btnUndo.UseVisualStyleBackColor = true;
		btnUndo.Click += BtnUndo_Click;
		// 
		// btnReload
		// 
		btnReload = new Button();
		btnReload.Enabled = false;
		btnReload.Location = new Point(150, 22);
		btnReload.Name = "btnReload";
		btnReload.Size = new Size(138, 29);
		btnReload.TabIndex = 1;
		btnReload.Text = "重新开始";
		btnReload.UseVisualStyleBackColor = true;
		btnReload.Click += BtnReload_Click;
		// 
		// groupBox5
		// 
		groupBox5 = new GroupBox();
		groupBox5.Controls.Add(btnReload);
		groupBox5.Controls.Add(btnUndo);
		groupBox5.Location = new Point(372, 432);
		groupBox5.Name = "groupBox5";
		groupBox5.Size = new Size(300, 55);
		groupBox5.TabIndex = 11;
		groupBox5.TabStop = false;
		groupBox5.Text = "历史";
		// 
		// mainWindow
		// 
		AutoScaleDimensions = new SizeF(7F, 17F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(684, 510);
		FormBorderStyle = FormBorderStyle.FixedSingle;
		MaximizeBox = false;
		Controls.Add(groupBox5);
		Controls.Add(groupBox4);
		Controls.Add(groupBox3);
		Controls.Add(groupBox2);
		Controls.Add(groupBox1);
		Controls.Add(lblInfo);
		Controls.Add(pictureBox1);
		Name = "mainWindow";
		Load += Form1_Load_1;
		((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
		groupBox1.ResumeLayout(false);
		groupBox2.ResumeLayout(false);
		groupBox2.PerformLayout();
		((System.ComponentModel.ISupportInitialize)TrackBar).EndInit();
		groupBox3.ResumeLayout(false);
		groupBox4.ResumeLayout(false);
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private PictureBox pictureBox1;
	private Button btnLoad;
	private Button btnBlackWhite;
	private Button btnLayout5;
	private Button btnSave;
	private Label lblInfo;
	private Button btnAbout;
	private GroupBox groupBox1;
	private GroupBox groupBox2;
	private GroupBox groupBox3;
	private Button btnLayout6;
	private GroupBox groupBox4;
	private Button btnChangeBg;
	private ComboBox cmbBgColor;
	private TrackBar TrackBar;
	private Label label1;
	private Button btnAutoCrop;
	private Button btnUnload;
	private Button btnSettings;
	private GroupBox groupBox5;
	private Button btnUndo;
	private Button btnReload;
}
