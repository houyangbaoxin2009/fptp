using System;
using System.Drawing;
using System.Windows.Forms;

namespace fptp
{
	/// <summary>
	/// 简单文本输入框（预设命名等场景用）。
	/// </summary>
	public partial class InputBox : Form
	{
		private readonly TextBox txtValue;
		private readonly Button btnOk;
		private readonly Button btnCancel;

		public string Value => txtValue.Text;

		public InputBox(string label, string defaultValue)
		{
			Text = Lang.Get("msg.presetNameTitle");
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			StartPosition = FormStartPosition.CenterParent;
			ShowInTaskbar = false;
			ClientSize = new Size(320, 110);

			var lbl = new Label
			{
				AutoSize = true,
				Location = new Point(14, 14),
				Text = label,
			};
			txtValue = new TextBox
			{
				Location = new Point(14, 38),
				Size = new Size(292, 23),
				Text = defaultValue,
			};
			btnOk = new Button
			{
				DialogResult = DialogResult.OK,
				Location = new Point(156, 72),
				Size = new Size(72, 28),
				Text = Lang.Get("settings.ok"),
			};
			btnCancel = new Button
			{
				DialogResult = DialogResult.Cancel,
				Location = new Point(234, 72),
				Size = new Size(72, 28),
				Text = Lang.Get("settings.cancel"),
			};
			// 控件创建后才能赋值，否则回车/Esc 无效
			AcceptButton = btnOk;
			CancelButton = btnCancel;

			Controls.Add(lbl);
			Controls.Add(txtValue);
			Controls.Add(btnOk);
			Controls.Add(btnCancel);

			Shown += (_, _) => txtValue.Focus();
		}
	}
}
