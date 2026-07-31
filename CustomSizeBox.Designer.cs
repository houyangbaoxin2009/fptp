namespace fptp;

partial class CustomSizeBox
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

	private void InitializeComponent()
	{
		lblPrompt = new System.Windows.Forms.Label();
		lblWidth = new System.Windows.Forms.Label();
		txtWidth = new System.Windows.Forms.TextBox();
		lblHeight = new System.Windows.Forms.Label();
		txtHeight = new System.Windows.Forms.TextBox();
		btnOk = new System.Windows.Forms.Button();
		btnCancel = new System.Windows.Forms.Button();
		SuspendLayout();
		//
		// lblPrompt
		//
		lblPrompt.AutoSize = true;
		lblPrompt.Location = new System.Drawing.Point(16, 16);
		lblPrompt.Name = "lblPrompt";
		lblPrompt.Size = new System.Drawing.Size(200, 17);
		lblPrompt.TabIndex = 0;
		lblPrompt.Text = "请输入排版宽度和高度（像素）：";
		//
		// lblWidth
		//
		lblWidth.AutoSize = true;
		lblWidth.Location = new System.Drawing.Point(16, 52);
		lblWidth.Name = "lblWidth";
		lblWidth.Size = new System.Drawing.Size(68, 17);
		lblWidth.TabIndex = 1;
		lblWidth.Text = "宽度:";
		//
		// txtWidth
		//
		txtWidth.Location = new System.Drawing.Point(120, 49);
		txtWidth.Name = "txtWidth";
		txtWidth.Size = new System.Drawing.Size(120, 25);
		txtWidth.TabIndex = 2;
		txtWidth.Text = "1500";
		//
		// lblHeight
		//
		lblHeight.AutoSize = true;
		lblHeight.Location = new System.Drawing.Point(16, 90);
		lblHeight.Name = "lblHeight";
		lblHeight.Size = new System.Drawing.Size(68, 17);
		lblHeight.TabIndex = 3;
		lblHeight.Text = "高度:";
		//
		// txtHeight
		//
		txtHeight.Location = new System.Drawing.Point(120, 87);
		txtHeight.Name = "txtHeight";
		txtHeight.Size = new System.Drawing.Size(120, 25);
		txtHeight.TabIndex = 4;
		txtHeight.Text = "1050";
		//
		// btnOk
		//
		btnOk.Location = new System.Drawing.Point(70, 132);
		btnOk.Name = "btnOk";
		btnOk.Size = new System.Drawing.Size(85, 30);
		btnOk.TabIndex = 5;
		btnOk.Text = "确定";
		btnOk.UseVisualStyleBackColor = true;
		btnOk.Click += BtnOk_Click;
		//
		// btnCancel
		//
		btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		btnCancel.Location = new System.Drawing.Point(170, 132);
		btnCancel.Name = "btnCancel";
		btnCancel.Size = new System.Drawing.Size(85, 30);
		btnCancel.TabIndex = 6;
		btnCancel.Text = "取消";
		btnCancel.UseVisualStyleBackColor = true;
		btnCancel.Click += BtnCancel_Click;
		//
		// CustomSizeBox
		//
		AcceptButton = btnOk;
		AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
		AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		CancelButton = btnCancel;
		ClientSize = new System.Drawing.Size(284, 181);
		Controls.Add(btnCancel);
		Controls.Add(btnOk);
		Controls.Add(txtHeight);
		Controls.Add(lblHeight);
		Controls.Add(txtWidth);
		Controls.Add(lblWidth);
		Controls.Add(lblPrompt);
		FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		Name = "CustomSizeBox";
		ShowIcon = false;
		ShowInTaskbar = false;
		StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		Text = "自定义排版尺寸";
		ResumeLayout(false);
		PerformLayout();
	}

	private System.Windows.Forms.Label lblPrompt;
	private System.Windows.Forms.Label lblWidth;
	private System.Windows.Forms.TextBox txtWidth;
	private System.Windows.Forms.Label lblHeight;
	private System.Windows.Forms.TextBox txtHeight;
	private System.Windows.Forms.Button btnOk;
	private System.Windows.Forms.Button btnCancel;
}
