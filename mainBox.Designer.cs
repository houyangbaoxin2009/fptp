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
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.btnLoad = new System.Windows.Forms.Button();
			this.btnBlackWhite = new System.Windows.Forms.Button();
			this.btnLayout = new System.Windows.Forms.Button();
			this.cmbLayout = new System.Windows.Forms.ComboBox();
			this.btnSave = new System.Windows.Forms.Button();
			this.btnSettings = new System.Windows.Forms.Button();
			this.lblInfo = new System.Windows.Forms.Label();
			this.btnAbout = new System.Windows.Forms.Button();
			this.chkAnimeMode = new System.Windows.Forms.CheckBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.btnAutoCrop = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.TrackBar = new System.Windows.Forms.TrackBar();
			this.btnChangeBg = new System.Windows.Forms.Button();
			this.cmbBgColor = new System.Windows.Forms.ComboBox();
			this.btnPresetDel = new System.Windows.Forms.Button();
			this.btnPresetSave = new System.Windows.Forms.Button();
			this.cmbPreset = new System.Windows.Forms.ComboBox();
			this.btnReload = new System.Windows.Forms.Button();
			this.btnUndo = new System.Windows.Forms.Button();
			this.btnUnload = new System.Windows.Forms.Button();
			this.btnPrint = new System.Windows.Forms.Button();
			this.btnBatch = new System.Windows.Forms.Button();
			this.pnlMain = new System.Windows.Forms.Panel();
			this.pnlSide = new System.Windows.Forms.Panel();
			this.pnlExport = new System.Windows.Forms.Panel();
			this.pnlLayout = new System.Windows.Forms.Panel();
			this.pnlImport = new System.Windows.Forms.Panel();
			this.pnlTop = new System.Windows.Forms.Panel();
			this.pnlStatus = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.TrackBar)).BeginInit();
			this.pnlMain.SuspendLayout();
			this.pnlSide.SuspendLayout();
			this.pnlExport.SuspendLayout();
			this.pnlLayout.SuspendLayout();
			this.pnlImport.SuspendLayout();
			this.pnlTop.SuspendLayout();
			this.pnlStatus.SuspendLayout();
			this.SuspendLayout();
			// 
			// pictureBox1
			// 
			this.pictureBox1.BackColor = System.Drawing.SystemColors.ControlDark;
			this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pictureBox1.Location = new System.Drawing.Point(0, 0);
			this.pictureBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(366, 348);
			this.pictureBox1.TabIndex = 0;
			this.pictureBox1.TabStop = false;
			// 
			// btnLoad
			// 
			this.btnLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnLoad.Location = new System.Drawing.Point(9, 9);
			this.btnLoad.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.btnLoad.Name = "btnLoad";
			this.btnLoad.Size = new System.Drawing.Size(108, 23);
			this.btnLoad.TabIndex = 0;
			this.btnLoad.Text = "本地图片";
			this.btnLoad.UseVisualStyleBackColor = true;
			this.btnLoad.Click += new System.EventHandler(this.BtnLoad_Click);
			// 
			// btnBlackWhite
			// 
			this.btnBlackWhite.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnBlackWhite.Location = new System.Drawing.Point(87, 40);
			this.btnBlackWhite.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.btnBlackWhite.Name = "btnBlackWhite";
			this.btnBlackWhite.Size = new System.Drawing.Size(74, 23);
			this.btnBlackWhite.TabIndex = 4;
			this.btnBlackWhite.Text = "变黑白";
			this.btnBlackWhite.UseVisualStyleBackColor = true;
			this.btnBlackWhite.Click += new System.EventHandler(this.BtnBlackWhite_Click);
			// 
			// btnLayout
			// 
			this.btnLayout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnLayout.Location = new System.Drawing.Point(180, 9);
			this.btnLayout.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.btnLayout.Name = "btnLayout";
			this.btnLayout.Size = new System.Drawing.Size(60, 23);
			this.btnLayout.TabIndex = 2;
			this.btnLayout.Text = "排版";
			this.btnLayout.UseVisualStyleBackColor = true;
			this.btnLayout.Click += new System.EventHandler(this.BtnLayout_Click);
			// 
			// cmbLayout
			// 
			this.cmbLayout.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbLayout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.cmbLayout.Location = new System.Drawing.Point(9, 11);
			this.cmbLayout.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.cmbLayout.Name = "cmbLayout";
			this.cmbLayout.Size = new System.Drawing.Size(102, 20);
			this.cmbLayout.TabIndex = 0;
			// 
			// btnSave
			// 
			this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnSave.Location = new System.Drawing.Point(9, 9);
			this.btnSave.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(74, 23);
			this.btnSave.TabIndex = 0;
			this.btnSave.Text = "导出本地";
			this.btnSave.UseVisualStyleBackColor = true;
			this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
			// 
			// btnSettings
			// 
			this.btnSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnSettings.Location = new System.Drawing.Point(449, 4);
			this.btnSettings.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.btnSettings.Name = "btnSettings";
			this.btnSettings.Size = new System.Drawing.Size(74, 25);
			this.btnSettings.TabIndex = 2;
			this.btnSettings.Text = "设置";
			this.btnSettings.UseVisualStyleBackColor = true;
			this.btnSettings.Click += new System.EventHandler(this.BtnSettings_Click);
			// 
			// lblInfo
			// 
			this.lblInfo.AutoSize = true;
			this.lblInfo.Location = new System.Drawing.Point(12, 4);
			this.lblInfo.Name = "lblInfo";
			this.lblInfo.Size = new System.Drawing.Size(0, 12);
			this.lblInfo.TabIndex = 0;
			// 
			// btnAbout
			// 
			this.btnAbout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnAbout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnAbout.Location = new System.Drawing.Point(529, 4);
			this.btnAbout.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.btnAbout.Name = "btnAbout";
			this.btnAbout.Size = new System.Drawing.Size(74, 25);
			this.btnAbout.TabIndex = 1;
			this.btnAbout.Text = "关于";
			this.btnAbout.UseVisualStyleBackColor = true;
			this.btnAbout.Click += new System.EventHandler(this.BtnAbout_Click);
			// 
			// chkAnimeMode
			// 
			this.chkAnimeMode.AutoSize = true;
			this.chkAnimeMode.Location = new System.Drawing.Point(9, 102);
			this.chkAnimeMode.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.chkAnimeMode.Name = "chkAnimeMode";
			this.chkAnimeMode.Size = new System.Drawing.Size(72, 16);
			this.chkAnimeMode.TabIndex = 9;
			this.chkAnimeMode.Text = "动画模式";
			this.chkAnimeMode.UseVisualStyleBackColor = true;
			this.chkAnimeMode.CheckedChanged += new System.EventHandler(this.chkAnimeMode_CheckedChanged);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.chkAnimeMode);
			this.groupBox2.Controls.Add(this.btnAutoCrop);
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Controls.Add(this.TrackBar);
			this.groupBox2.Controls.Add(this.btnChangeBg);
			this.groupBox2.Controls.Add(this.cmbBgColor);
			this.groupBox2.Controls.Add(this.btnBlackWhite);
			this.groupBox2.Controls.Add(this.btnPresetDel);
			this.groupBox2.Controls.Add(this.btnPresetSave);
			this.groupBox2.Controls.Add(this.cmbPreset);
			this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBox2.Location = new System.Drawing.Point(0, 41);
			this.groupBox2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.groupBox2.Size = new System.Drawing.Size(249, 307);
			this.groupBox2.TabIndex = 1;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "工具箱";
			this.groupBox2.Enter += new System.EventHandler(this.groupBox2_Enter);
			// 
			// btnAutoCrop
			// 
			this.btnAutoCrop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnAutoCrop.Location = new System.Drawing.Point(9, 40);
			this.btnAutoCrop.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.btnAutoCrop.Name = "btnAutoCrop";
			this.btnAutoCrop.Size = new System.Drawing.Size(74, 23);
			this.btnAutoCrop.TabIndex = 3;
			this.btnAutoCrop.Text = "智能裁剪";
			this.btnAutoCrop.UseVisualStyleBackColor = true;
			this.btnAutoCrop.Click += new System.EventHandler(this.BtnAutoCrop_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(79, 70);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(65, 12);
			this.label1.TabIndex = 7;
			this.label1.Text = "算法灵敏度";
			this.label1.Click += new System.EventHandler(this.label1_Click);
			// 
			// TrackBar
			// 
			this.TrackBar.Location = new System.Drawing.Point(130, 64);
			this.TrackBar.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.TrackBar.Maximum = 150;
			this.TrackBar.Name = "TrackBar";
			this.TrackBar.Size = new System.Drawing.Size(110, 45);
			this.TrackBar.TabIndex = 8;
			this.TrackBar.Value = 60;
			this.TrackBar.Scroll += new System.EventHandler(this.TrackBar_Scroll);
			// 
			// btnChangeBg
			// 
			this.btnChangeBg.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnChangeBg.Location = new System.Drawing.Point(166, 40);
			this.btnChangeBg.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.btnChangeBg.Name = "btnChangeBg";
			this.btnChangeBg.Size = new System.Drawing.Size(74, 23);
			this.btnChangeBg.TabIndex = 5;
			this.btnChangeBg.Text = "修改底色";
			this.btnChangeBg.UseVisualStyleBackColor = true;
			this.btnChangeBg.Click += new System.EventHandler(this.BtnChangeBg_Click);
			// 
			// cmbBgColor
			// 
			this.cmbBgColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbBgColor.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.cmbBgColor.FormattingEnabled = true;
			this.cmbBgColor.Items.AddRange(new object[] {
            "蓝色",
            "红色",
            "白色",
            "透明"});
			this.cmbBgColor.Location = new System.Drawing.Point(9, 66);
			this.cmbBgColor.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.cmbBgColor.Name = "cmbBgColor";
			this.cmbBgColor.Size = new System.Drawing.Size(64, 20);
			this.cmbBgColor.TabIndex = 6;
			this.cmbBgColor.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
			// 
			// btnPresetDel
			// 
			this.btnPresetDel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnPresetDel.Location = new System.Drawing.Point(165, 13);
			this.btnPresetDel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.btnPresetDel.Name = "btnPresetDel";
			this.btnPresetDel.Size = new System.Drawing.Size(75, 23);
			this.btnPresetDel.TabIndex = 2;
			this.btnPresetDel.Text = "删除";
			this.btnPresetDel.UseVisualStyleBackColor = true;
			this.btnPresetDel.Click += new System.EventHandler(this.BtnPresetDel_Click);
			// 
			// btnPresetSave
			// 
			this.btnPresetSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnPresetSave.Location = new System.Drawing.Point(87, 13);
			this.btnPresetSave.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.btnPresetSave.Name = "btnPresetSave";
			this.btnPresetSave.Size = new System.Drawing.Size(72, 23);
			this.btnPresetSave.TabIndex = 1;
			this.btnPresetSave.Text = "保存预设";
			this.btnPresetSave.UseVisualStyleBackColor = true;
			this.btnPresetSave.Click += new System.EventHandler(this.BtnPresetSave_Click);
			// 
			// cmbPreset
			// 
			this.cmbPreset.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbPreset.Location = new System.Drawing.Point(9, 14);
			this.cmbPreset.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.cmbPreset.Name = "cmbPreset";
			this.cmbPreset.Size = new System.Drawing.Size(74, 20);
			this.cmbPreset.TabIndex = 0;
			this.cmbPreset.SelectedIndexChanged += new System.EventHandler(this.cmbPreset_SelectedIndexChanged);
			// 
			// btnReload
			// 
			this.btnReload.Enabled = false;
			this.btnReload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnReload.Location = new System.Drawing.Point(289, 4);
			this.btnReload.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.btnReload.Name = "btnReload";
			this.btnReload.Size = new System.Drawing.Size(74, 25);
			this.btnReload.TabIndex = 11;
			this.btnReload.Text = "重新开始";
			this.btnReload.UseVisualStyleBackColor = true;
			this.btnReload.Click += new System.EventHandler(this.BtnReload_Click);
			// 
			// btnUndo
			// 
			this.btnUndo.Enabled = false;
			this.btnUndo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnUndo.Location = new System.Drawing.Point(369, 4);
			this.btnUndo.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.btnUndo.Name = "btnUndo";
			this.btnUndo.Size = new System.Drawing.Size(74, 25);
			this.btnUndo.TabIndex = 10;
			this.btnUndo.Text = "撤回";
			this.btnUndo.UseVisualStyleBackColor = true;
			this.btnUndo.Click += new System.EventHandler(this.BtnUndo_Click);
			// 
			// btnUnload
			// 
			this.btnUnload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnUnload.Location = new System.Drawing.Point(166, 9);
			this.btnUnload.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.btnUnload.Name = "btnUnload";
			this.btnUnload.Size = new System.Drawing.Size(74, 23);
			this.btnUnload.TabIndex = 2;
			this.btnUnload.Text = "卸载图片";
			this.btnUnload.UseVisualStyleBackColor = true;
			this.btnUnload.Click += new System.EventHandler(this.BtnUnload_Click);
			// 
			// btnPrint
			// 
			this.btnPrint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnPrint.Location = new System.Drawing.Point(87, 9);
			this.btnPrint.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.btnPrint.Name = "btnPrint";
			this.btnPrint.Size = new System.Drawing.Size(74, 23);
			this.btnPrint.TabIndex = 1;
			this.btnPrint.Text = "打印";
			this.btnPrint.UseVisualStyleBackColor = true;
			this.btnPrint.Click += new System.EventHandler(this.BtnPrint_Click);
			// 
			// btnBatch
			// 
			this.btnBatch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnBatch.Location = new System.Drawing.Point(122, 9);
			this.btnBatch.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.btnBatch.Name = "btnBatch";
			this.btnBatch.Size = new System.Drawing.Size(118, 23);
			this.btnBatch.TabIndex = 1;
			this.btnBatch.Text = "文件夹批处理";
			this.btnBatch.UseVisualStyleBackColor = true;
			this.btnBatch.Click += new System.EventHandler(this.BtnBatch_Click);
			// 
			// pnlMain
			// 
			this.pnlMain.Controls.Add(this.pictureBox1);
			this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pnlMain.Location = new System.Drawing.Point(0, 33);
			this.pnlMain.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.pnlMain.Name = "pnlMain";
			this.pnlMain.Size = new System.Drawing.Size(366, 348);
			this.pnlMain.TabIndex = 0;
			// 
			// pnlSide
			// 
			this.pnlSide.Controls.Add(this.pnlLayout);
			this.pnlSide.Controls.Add(this.pnlExport);
			this.pnlSide.Controls.Add(this.groupBox2);
			this.pnlSide.Controls.Add(this.pnlImport);
			this.pnlSide.Dock = System.Windows.Forms.DockStyle.Right;
			this.pnlSide.Location = new System.Drawing.Point(366, 33);
			this.pnlSide.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.pnlSide.Name = "pnlSide";
			this.pnlSide.Size = new System.Drawing.Size(249, 348);
			this.pnlSide.TabIndex = 0;
			// 
			// pnlExport
			// 
			this.pnlExport.Controls.Add(this.btnUnload);
			this.pnlExport.Controls.Add(this.btnPrint);
			this.pnlExport.Controls.Add(this.btnSave);
			this.pnlExport.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.pnlExport.Location = new System.Drawing.Point(0, 307);
			this.pnlExport.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.pnlExport.Name = "pnlExport";
			this.pnlExport.Padding = new System.Windows.Forms.Padding(9, 0, 9, 0);
			this.pnlExport.Size = new System.Drawing.Size(249, 41);
			this.pnlExport.TabIndex = 3;
			// 
			// pnlLayout
			// 
			this.pnlLayout.Controls.Add(this.btnLayout);
			this.pnlLayout.Controls.Add(this.cmbLayout);
			this.pnlLayout.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.pnlLayout.Location = new System.Drawing.Point(0, 266);
			this.pnlLayout.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.pnlLayout.Name = "pnlLayout";
			this.pnlLayout.Padding = new System.Windows.Forms.Padding(9, 0, 9, 0);
			this.pnlLayout.Size = new System.Drawing.Size(249, 41);
			this.pnlLayout.TabIndex = 2;
			// 
			// pnlImport
			// 
			this.pnlImport.Controls.Add(this.btnBatch);
			this.pnlImport.Controls.Add(this.btnLoad);
			this.pnlImport.Dock = System.Windows.Forms.DockStyle.Top;
			this.pnlImport.Location = new System.Drawing.Point(0, 0);
			this.pnlImport.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.pnlImport.Name = "pnlImport";
			this.pnlImport.Padding = new System.Windows.Forms.Padding(9, 0, 9, 0);
			this.pnlImport.Size = new System.Drawing.Size(249, 41);
			this.pnlImport.TabIndex = 0;
			// 
			// pnlTop
			// 
			this.pnlTop.Controls.Add(this.btnReload);
			this.pnlTop.Controls.Add(this.btnSettings);
			this.pnlTop.Controls.Add(this.btnUndo);
			this.pnlTop.Controls.Add(this.btnAbout);
			this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
			this.pnlTop.Location = new System.Drawing.Point(0, 0);
			this.pnlTop.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.pnlTop.Name = "pnlTop";
			this.pnlTop.Size = new System.Drawing.Size(615, 33);
			this.pnlTop.TabIndex = 0;
			// 
			// pnlStatus
			// 
			this.pnlStatus.Controls.Add(this.lblInfo);
			this.pnlStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.pnlStatus.Location = new System.Drawing.Point(0, 381);
			this.pnlStatus.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.pnlStatus.Name = "pnlStatus";
			this.pnlStatus.Size = new System.Drawing.Size(615, 18);
			this.pnlStatus.TabIndex = 0;
			// 
			// mainBox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(615, 399);
			this.Controls.Add(this.pnlMain);
			this.Controls.Add(this.pnlSide);
			this.Controls.Add(this.pnlStatus);
			this.Controls.Add(this.pnlTop);
			this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.MinimumSize = new System.Drawing.Size(551, 329);
			this.Name = "mainBox";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Load += new System.EventHandler(this.Form1_Load_1);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.TrackBar)).EndInit();
			this.pnlMain.ResumeLayout(false);
			this.pnlSide.ResumeLayout(false);
			this.pnlExport.ResumeLayout(false);
			this.pnlLayout.ResumeLayout(false);
			this.pnlImport.ResumeLayout(false);
			this.pnlTop.ResumeLayout(false);
			this.pnlStatus.ResumeLayout(false);
			this.pnlStatus.PerformLayout();
			this.ResumeLayout(false);

	}

	#endregion

	private PictureBox pictureBox1;
	private Button btnLoad;
	private Button btnBlackWhite;
	private ComboBox cmbLayout;
	private Button btnLayout;
	private Button btnSave;
	private Label lblInfo;
	private Button btnAbout;
	private GroupBox groupBox2;
	private Button btnChangeBg;
	private ComboBox cmbBgColor;
	private TrackBar TrackBar;
	private Label label1;
	private Button btnAutoCrop;
	private CheckBox chkAnimeMode;
	private Button btnUnload;
	private Button btnSettings;
	private Button btnPrint;
	private Button btnUndo;
	private Button btnReload;
	private Button btnBatch;
	private ComboBox cmbPreset;
	private Button btnPresetSave;
	private Button btnPresetDel;
	private Panel pnlMain;
	private Panel pnlSide;
	private Panel pnlImport;
	private Panel pnlLayout;
	private Panel pnlExport;
	private Panel pnlTop;
	private Panel pnlStatus;
}


