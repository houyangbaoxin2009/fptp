using System;
using System.Windows.Forms;

namespace fptp
{
	public partial class GenSettingsBox : Form
	{
		public GenSettings Result { get; private set; }

		public GenSettingsBox(GenSettings current)
		{
			InitializeComponent();
			Result = current;
		}

		private void SettingsBox_Load(object sender, EventArgs e)
		{
			cmbSaveFormat.Text = Result.SaveFormat.ToUpperInvariant();
			cmbSize.Text = Result.DefaultSize switch { 2 => "二寸", 3 => "小二寸", _ => "一寸" };
			cmbBgColor.Text = Result.BackgroundColor;
			trackBar.Value = Result.Tolerance;
			lblToleranceVal.Text = Result.Tolerance.ToString();
		}

		private void trackBar_Scroll(object sender, EventArgs e)
		{
			lblToleranceVal.Text = trackBar.Value.ToString();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			Result.SaveFormat = cmbSaveFormat.Text.ToLowerInvariant();
			Result.DefaultSize = cmbSize.Text switch
			{
				"二寸" => 2,
				"小二寸" => 3,
				_ => 1,
			};
			Result.BackgroundColor = cmbBgColor.Text;
			Result.Tolerance = trackBar.Value;
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
