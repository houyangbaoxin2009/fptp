namespace fptp;

partial class KeySettingsBox
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
		dgvKeys = new System.Windows.Forms.DataGridView();
		colAction = new System.Windows.Forms.DataGridViewTextBoxColumn();
		colCombo = new System.Windows.Forms.DataGridViewTextBoxColumn();
		lblHint = new System.Windows.Forms.Label();
		btnReset = new System.Windows.Forms.Button();
		btnOk = new System.Windows.Forms.Button();
		btnCancel = new System.Windows.Forms.Button();
		((System.ComponentModel.ISupportInitialize)dgvKeys).BeginInit();
		SuspendLayout();
		//
		// dgvKeys
		//
		dgvKeys.AllowUserToAddRows = false;
		dgvKeys.AllowUserToDeleteRows = false;
		dgvKeys.AllowUserToResizeRows = false;
		dgvKeys.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		dgvKeys.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colAction, colCombo });
		dgvKeys.Location = new System.Drawing.Point(12, 12);
		dgvKeys.MultiSelect = false;
		dgvKeys.Name = "dgvKeys";
		dgvKeys.ReadOnly = true;
		dgvKeys.RowHeadersVisible = false;
		dgvKeys.RowTemplate.Height = 28;
		dgvKeys.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
		dgvKeys.Size = new System.Drawing.Size(376, 388);
		dgvKeys.TabIndex = 0;
		//
		// colAction
		//
		colAction.HeaderText = "操作";
		colAction.Name = "colAction";
		colAction.ReadOnly = true;
		colAction.Width = 170;
		//
		// colCombo
		//
		colCombo.HeaderText = "快捷键";
		colCombo.Name = "colCombo";
		colCombo.ReadOnly = true;
		colCombo.Width = 180;
		//
		// lblHint
		//
		lblHint.AutoSize = true;
		lblHint.Location = new System.Drawing.Point(12, 410);
		lblHint.Name = "lblHint";
		lblHint.Size = new System.Drawing.Size(200, 17);
		lblHint.TabIndex = 1;
		lblHint.Text = "点击操作行后按下新快捷键";
		//
		// btnReset
		//
		btnReset.Location = new System.Drawing.Point(12, 440);
		btnReset.Name = "btnReset";
		btnReset.Size = new System.Drawing.Size(90, 30);
		btnReset.TabIndex = 2;
		btnReset.Text = "恢复默认";
		btnReset.UseVisualStyleBackColor = true;
		btnReset.Click += BtnReset_Click;
		//
		// btnOk
		//
		btnOk.Location = new System.Drawing.Point(220, 440);
		btnOk.Name = "btnOk";
		btnOk.Size = new System.Drawing.Size(80, 30);
		btnOk.TabIndex = 3;
		btnOk.Text = "确定";
		btnOk.UseVisualStyleBackColor = true;
		btnOk.Click += BtnOk_Click;
		//
		// btnCancel
		//
		btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		btnCancel.Location = new System.Drawing.Point(308, 440);
		btnCancel.Name = "btnCancel";
		btnCancel.Size = new System.Drawing.Size(80, 30);
		btnCancel.TabIndex = 4;
		btnCancel.Text = "取消";
		btnCancel.UseVisualStyleBackColor = true;
		btnCancel.Click += BtnCancel_Click;
		//
		// KeySettingsBox
		//
		AcceptButton = btnOk;
		AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
		AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		CancelButton = btnCancel;
		ClientSize = new System.Drawing.Size(400, 482);
		Controls.Add(btnCancel);
		Controls.Add(btnOk);
		Controls.Add(btnReset);
		Controls.Add(lblHint);
		Controls.Add(dgvKeys);
		FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		Name = "KeySettingsBox";
		ShowIcon = false;
		ShowInTaskbar = false;
		StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		Text = "快捷键";
		((System.ComponentModel.ISupportInitialize)dgvKeys).EndInit();
		ResumeLayout(false);
		PerformLayout();
	}

	private System.Windows.Forms.DataGridView dgvKeys;
	private System.Windows.Forms.DataGridViewTextBoxColumn colAction;
	private System.Windows.Forms.DataGridViewTextBoxColumn colCombo;
	private System.Windows.Forms.Label lblHint;
	private System.Windows.Forms.Button btnReset;
	private System.Windows.Forms.Button btnOk;
	private System.Windows.Forms.Button btnCancel;
}
