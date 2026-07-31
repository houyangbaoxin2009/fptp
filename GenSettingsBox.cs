using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace fptp
{
	public partial class GenSettingsBox : Form
	{
		public GenSettings Result { get; private set; }
		public AppSettings AppResult { get; private set; }

		public GenSettingsBox(GenSettings current)
		{
			InitializeComponent();
			Result = current;
			AppResult = Assalg.LoadAppSettings();
		}

		private void SettingsBox_Load(object sender, EventArgs e)
		{
			ApplyLang();
			ApplyToUI();
		}

		private void ApplyLang()
		{
			Text = Lang.Get("settings.title");
			label1.Text = Lang.Get("settings.saveFormat");
			label2.Text = Lang.Get("settings.defaultSize");
			label3.Text = Lang.Get("settings.defaultBg");
			label4.Text = Lang.Get("settings.defaultTolerance");
			groupPrivacy.Text = Lang.Get("settings.privacy");
			chkAllowExternal.Text = Lang.Get("settings.allowExternal");
			groupLang.Text = Lang.Get("settings.language");
			groupUpdate.Text = Lang.Get("settings.autoUpdate");
			chkAutoUpdate.Text = Lang.Get("settings.autoUpdateDesc");
			groupLayout.Text = Lang.Get("settings.layoutSize");
			label5.Text = Lang.Get("settings.layoutPreset");
			label6.Text = Lang.Get("settings.customW");
			label7.Text = Lang.Get("settings.customH");
			label8.Text = Lang.Get("settings.guideLine");
			label9.Text = Lang.Get("settings.jpgQuality");
			groupExport.Text = Lang.Get("settings.export");
			btnReset.Text = Lang.Get("settings.reset");
			btnImport.Text = Lang.Get("settings.import");
			btnExport.Text = Lang.Get("settings.export");
			btnOk.Text = Lang.Get("settings.ok");
			btnCancel.Text = Lang.Get("settings.cancel");

			cmbLang.Items.Clear();
			cmbLang.Items.Add(Lang.Get("settings.lang.zh"));
			cmbLang.Items.Add(Lang.Get("settings.lang.en"));

			ReloadSizePresets();
			ReloadLayoutPresets();
			ReloadGuideLine();
		}

		/// <summary>重填默认尺寸下拉（翻译文本）。</summary>
		private void ReloadSizePresets()
		{
			string[] keys = { "size.one", "size.two", "size.passport" };
			int sel = cmbSize.SelectedIndex >= 0 ? cmbSize.SelectedIndex : Result.DefaultSize - 1;
			cmbSize.Items.Clear();
			foreach (string key in keys)
				cmbSize.Items.Add(Lang.Get(key));
			if (sel >= 0 && sel < cmbSize.Items.Count)
				cmbSize.SelectedIndex = sel;
			else
				cmbSize.SelectedIndex = Result.DefaultSize - 1;
		}

		private void ReloadGuideLine()
		{
			string[] keys = { "settings.guideLine.dashed", "settings.guideLine.solid", "settings.guideLine.none" };
			int sel = cmbGuideLine.SelectedIndex >= 0 ? cmbGuideLine.SelectedIndex : Result.GuideLineStyle;
			cmbGuideLine.Items.Clear();
			foreach (string key in keys)
				cmbGuideLine.Items.Add(Lang.Get(key));
			if (sel >= 0 && sel < cmbGuideLine.Items.Count)
				cmbGuideLine.SelectedIndex = sel;
			else
				cmbGuideLine.SelectedIndex = Result.GuideLineStyle;
		}

		private void ReloadLayoutPresets()
		{
			string[] keys = { "layout.preset5", "layout.preset6", "layout.presetA4", "layout.presetA5", "layout.custom" };
			int sel = cmbLayoutPreset.SelectedIndex >= 0 ? cmbLayoutPreset.SelectedIndex : Result.LayoutPreset;
			cmbLayoutPreset.Items.Clear();
			foreach (string key in keys)
				cmbLayoutPreset.Items.Add(Lang.Get(key));
			if (sel >= 0 && sel < cmbLayoutPreset.Items.Count)
				cmbLayoutPreset.SelectedIndex = sel;
			else
				cmbLayoutPreset.SelectedIndex = Result.LayoutPreset;
		}

		private void ApplyToUI()
		{
			int fmtIdx = cmbSaveFormat.Items.IndexOf(Result.SaveFormat.ToUpperInvariant());
			cmbSaveFormat.SelectedIndex = fmtIdx >= 0 ? fmtIdx : 0;
			int sizeIdx = Result.DefaultSize switch
			{
				2 => 1,
				3 => 2,
				_ => 0,
			};
			cmbSize.SelectedIndex = sizeIdx;
			cmbBgColor.SelectedIndex = ColorIndexFromStored(Result.BackgroundColor);
			trackBar.Value = Result.Tolerance;
			lblToleranceVal.Text = Result.Tolerance.ToString();

			chkAllowExternal.Checked = AppResult.Privacy.AllowExternalAccess;
			cmbLang.SelectedIndex = AppResult.Language == "en-US" ? 1 : 0;
			chkAutoUpdate.Checked = AppResult.AutoUpdate;

			txtCustomW.Text = Result.CustomLayoutW.ToString();
			txtCustomH.Text = Result.CustomLayoutH.ToString();
			ReloadLayoutPresets();

			trackBarQuality.Value = Math.Max(70, Math.Min(100, Result.SaveQuality));
			lblQualityVal.Text = trackBarQuality.Value.ToString();
			ReloadGuideLine();
		}

		/// <summary>
		/// 将设置中存储的颜色值映射为下拉索引（0=蓝 1=红 2=白）。
		/// </summary>
		private static int ColorIndexFromStored(string stored)
		{
			switch (stored)
			{
				case "蓝色": return 0;
				case "红色": return 1;
				default: return 2;
			}
		}

		private void trackBar_Scroll(object sender, EventArgs e)
		{
			lblToleranceVal.Text = trackBar.Value.ToString();
		}

		private void trackBarQuality_Scroll(object sender, EventArgs e)
		{
			lblQualityVal.Text = trackBarQuality.Value.ToString();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			Result.SaveFormat = cmbSaveFormat.Text.ToLowerInvariant();
			Result.DefaultSize = cmbSize.SelectedIndex switch
			{
				1 => 2,
				2 => 3,
				_ => 1,
			};
			Result.BackgroundColor = cmbBgColor.SelectedIndex switch
			{
				0 => "蓝色",
				1 => "红色",
				_ => "白色",
			};
			Result.Tolerance = trackBar.Value;

			Result.LayoutPreset = cmbLayoutPreset.SelectedIndex >= 0 ? cmbLayoutPreset.SelectedIndex : 0;
			int.TryParse(txtCustomW.Text.Trim(), out int w);
			int.TryParse(txtCustomH.Text.Trim(), out int h);
			if (w >= 100 && w <= 10000) Result.CustomLayoutW = w;
			if (h >= 100 && h <= 10000) Result.CustomLayoutH = h;

			Result.GuideLineStyle = cmbGuideLine.SelectedIndex >= 0 ? cmbGuideLine.SelectedIndex : 0;
			Result.SaveQuality = trackBarQuality.Value;

			AppResult.Privacy.AllowExternalAccess = chkAllowExternal.Checked;
			AppResult.Language = cmbLang.SelectedIndex == 1 ? "en-US" : "zh-CN";
			AppResult.AutoUpdate = chkAutoUpdate.Checked;
			Assalg.SaveAppSettings(AppResult);

			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnReset_Click(object sender, EventArgs e)
		{
			var dr = MessageBox.Show(Lang.Get("settings.resetConfirm"), Lang.Get("settings.resetTitle"),
				MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (dr != DialogResult.Yes) return;

			Result = new GenSettings();
			AppResult = new AppSettings();
			ApplyLang();
			ApplyToUI();
		}

		private void BtnImport_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog ofd = new OpenFileDialog())
			{
				ofd.Filter = "JSON 文件|*.json";
				ofd.Title = Lang.Get("settings.import");
				if (ofd.ShowDialog(this) != DialogResult.OK) return;

				try
				{
					string json = File.ReadAllText(ofd.FileName);
					var pkg = SettingsPackage.FromJson(json);
					if (pkg == null || pkg.Gen == null || pkg.App == null)
					{
						MessageBox.Show(Lang.Get("msg.loadFailed", "invalid json"), Lang.Get("msg.error"),
							MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					Result = pkg.Gen;
					AppResult = pkg.App;
					ApplyLang();
					ApplyToUI();
				}
				catch (Exception ex)
				{
					MessageBox.Show(Lang.Get("msg.loadFailed", ex.Message), Lang.Get("msg.error"),
						MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void BtnExport_Click(object sender, EventArgs e)
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "JSON 文件|*.json";
				sfd.Title = Lang.Get("settings.export");
				sfd.FileName = "fptp-settings.json";
				if (sfd.ShowDialog(this) != DialogResult.OK) return;

				try
				{
					var pkg = new SettingsPackage
					{
						App = new AppSettings
						{
							Privacy = new PrivacySettings
							{
								AllowExternalAccess = chkAllowExternal.Checked
							},
							Language = cmbLang.SelectedIndex == 1 ? "en-US" : "zh-CN",
							AutoUpdate = chkAutoUpdate.Checked
						},
						Gen = new GenSettings
						{
							SaveFormat = cmbSaveFormat.Text.ToLowerInvariant(),
							DefaultSize = cmbSize.SelectedIndex switch
							{
								1 => 2,
								2 => 3,
								_ => 1,
							},
							BackgroundColor = cmbBgColor.SelectedIndex switch
							{
								0 => "蓝色",
								1 => "红色",
								_ => "白色",
							},
							Tolerance = trackBar.Value,
							LayoutPreset = cmbLayoutPreset.SelectedIndex >= 0 ? cmbLayoutPreset.SelectedIndex : 0,
							CustomLayoutW = Result.CustomLayoutW,
							CustomLayoutH = Result.CustomLayoutH,
							GuideLineStyle = cmbGuideLine.SelectedIndex >= 0 ? cmbGuideLine.SelectedIndex : 0,
							SaveQuality = trackBarQuality.Value
						}
					};
					File.WriteAllText(sfd.FileName, pkg.ToJson());
				}
				catch (Exception ex)
				{
					MessageBox.Show(Lang.Get("msg.saveFailed", ex.Message), Lang.Get("msg.error"),
						MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
