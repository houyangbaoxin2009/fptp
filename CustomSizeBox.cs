using System;
using System.Windows.Forms;

namespace fptp
{
	/// <summary>
	/// 自定义排版尺寸输入对话框。
	/// </summary>
	public partial class CustomSizeBox : Form
	{
		public int WidthValue { get; private set; }
		public int HeightValue { get; private set; }

		public CustomSizeBox(int width, int height)
		{
			InitializeComponent();
			txtWidth.Text = width.ToString();
			txtHeight.Text = height.ToString();
			ApplyLang();
		}

		private void ApplyLang()
		{
			Text = Lang.Get("msg.customSizeTitle");
			lblPrompt.Text = Lang.Get("msg.customSizePrompt");
			lblWidth.Text = Lang.Get("settings.customW") + ":";
			lblHeight.Text = Lang.Get("settings.customH") + ":";
			btnOk.Text = Lang.Get("settings.ok");
			btnCancel.Text = Lang.Get("settings.cancel");
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			if (int.TryParse(txtWidth.Text.Trim(), out int w) &&
				int.TryParse(txtHeight.Text.Trim(), out int h) &&
				w >= 100 && w <= 10000 && h >= 100 && h <= 10000)
			{
				WidthValue = w;
				HeightValue = h;
				DialogResult = DialogResult.OK;
				Close();
			}
			else
			{
				MessageBox.Show(Lang.Get("msg.customSizeInvalid"), Lang.Get("msg.error"),
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
