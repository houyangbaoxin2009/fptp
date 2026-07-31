using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace fptp
{
	public partial class GenSettingsBox : Form
	{
		public GenSettings Result { get; private set; }
		public AppSettings AppResult { get; private set; }

		/// <summary>当前可用语言列表（对应 cmbLang 下拉顺序）。</summary>
		private List<LangCon> _langList = new List<LangCon>();

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
			btnLangImport.Text = Lang.Get("settings.lang.import");
			btnLangExport.Text = Lang.Get("settings.lang.export");
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

			ReloadLanguages();
			ReloadSizePresets();
			ReloadLayoutPresets();
			ReloadGuideLine();
		}

		/// <summary>
		/// 重填语言下拉：内置语言 + 设置文件中已导入的语言，显示名用语言包的 name。
		/// </summary>
		private void ReloadLanguages()
		{
			int sel = cmbLang.SelectedIndex >= 0 && cmbLang.SelectedIndex < _langList.Count
				? cmbLang.SelectedIndex
				: _langList.FindIndex(x => x.Id == AppResult.Language);
			_langList = Lang.AvailableLanguages();
			cmbLang.Items.Clear();
			foreach (LangCon lang in _langList)
				cmbLang.Items.Add(lang.Name);
			cmbLang.SelectedIndex = sel >= 0 ? sel : 0;
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
			ReloadLanguages();
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
			AppResult.Language = cmbLang.SelectedIndex >= 0 && cmbLang.SelectedIndex < _langList.Count
				? _langList[cmbLang.SelectedIndex].Id : "zh-CN";
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
					// 设置包内含语言包则注册并切换到该语言
					if (pkg.Lang != null && pkg.Lang.Ass != null && pkg.Lang.Ass.Count > 0)
					{
						Lang.Register(pkg.Lang.Con.Id, pkg.Lang.Con.Name, pkg.Lang.Ass);
						Lang.Load(pkg.Lang.Con.Id);
						AppResult.Language = pkg.Lang.Con.Id;
					}
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
							Language = cmbLang.SelectedIndex >= 0 && cmbLang.SelectedIndex < _langList.Count
								? _langList[cmbLang.SelectedIndex].Id : "zh-CN",
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
						},
						// 语言包随设置包导出
						Lang = new LangPackage
						{
							Con = new LangCon
							{
								Id = Lang.CurrentId,
								Name = Lang.CurrentDisplayName
							},
							Ass = Lang.ExportTable()
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

		/// <summary>
		/// 导入语言包：文件名须为 lang.{id}.{name}.json（id 为语言 id，name 为显示名），
		/// 内容为语言包本体（ass 结构）。导入后注册到设置文件并立即切换语言。
		/// </summary>
		private void BtnLangImport_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog ofd = new OpenFileDialog())
			{
				ofd.Filter = "语言包|lang.*.json|JSON 文件|*.json";
				ofd.Title = Lang.Get("settings.lang.import");
				if (ofd.ShowDialog(this) != DialogResult.OK) return;

				try
				{
					// 从文件名提取语言 id 和显示名：lang.zh-CN.简体中文.json
					string fileName = Path.GetFileName(ofd.FileName);
					LangCon? info = ExtractLangInfo(fileName);
					if (info == null)
					{
						MessageBox.Show(Lang.Get("msg.langFileInvalid", fileName), Lang.Get("msg.error"),
							MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}

					string json = File.ReadAllText(ofd.FileName);
					var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
					if (dict == null || dict.Count == 0)
					{
						MessageBox.Show(Lang.Get("msg.loadFailed", "empty lang pack"), Lang.Get("msg.error"),
							MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}

					Lang.Register(info.Id, info.Name, dict);
					Lang.Load(info.Id);
					AppResult.Language = info.Id;
					ApplyLang();
					ApplyToUI();
					MessageBox.Show(Lang.Get("msg.langImported", info.Name), Lang.Get("msg.done"),
						MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show(Lang.Get("msg.loadFailed", ex.Message), Lang.Get("msg.error"),
						MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		/// <summary>
		/// 导出语言包：文件名 lang.{id}.{name}.json，内容为当前语言包本体（ass 结构，便于用户自行翻译）。
		/// </summary>
		private void BtnLangExport_Click(object sender, EventArgs e)
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "JSON 文件|*.json";
				sfd.Title = Lang.Get("settings.lang.export");
				sfd.FileName = $"lang.{Lang.CurrentId}.{Lang.CurrentDisplayName}.json";
				if (sfd.ShowDialog(this) != DialogResult.OK) return;

				try
				{
					string json = JsonSerializer.Serialize(Lang.ExportTable(),
						new JsonSerializerOptions { WriteIndented = true });
					File.WriteAllText(sfd.FileName, json);
					MessageBox.Show(Lang.Get("msg.langExported", sfd.FileName), Lang.Get("msg.done"),
						MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show(Lang.Get("msg.saveFailed", ex.Message), Lang.Get("msg.error"),
						MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		/// <summary>
		/// 从文件名提取语言信息：lang.zh-CN.简体中文.json → {id=zh-CN, name=简体中文}。
		/// 格式不符返回 null。
		/// </summary>
		private static LangCon? ExtractLangInfo(string fileName)
		{
			if (string.IsNullOrEmpty(fileName)) return null;
			const string prefix = "lang.";
			if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
			if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return null;
			string middle = fileName.Substring(prefix.Length, fileName.Length - prefix.Length - ".json".Length);
			// middle = id.name，按第一个点分割，剩余部分作为显示名（显示名允许含点）
			int dot = middle.IndexOf('.');
			string id = dot >= 0 ? middle.Substring(0, dot) : middle;
			string name = dot >= 0 ? middle.Substring(dot + 1) : "";
			if (string.IsNullOrEmpty(id)) return null;
			if (string.IsNullOrEmpty(name)) name = id;
			return new LangCon { Id = id, Name = name };
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
