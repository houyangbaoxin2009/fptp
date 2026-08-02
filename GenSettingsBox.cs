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

		/// <summary>当前可用主题列表（对应 cmbTheme 下拉顺序）。</summary>
		private List<ThemeCon> _themeList = new List<ThemeCon>();

		/// <summary>防止加载时误触发主题切换的守卫。</summary>
		private bool _applyingUi;

		/// <summary>打开对话框时的原始主题 id，取消时用于回滚主题（含磁盘写入）。</summary>
		private readonly string _originalThemeId;

		public GenSettingsBox(GenSettings current)
		{
			InitializeComponent();
			Result = current;
			AppResult = Assalg.LoadAppSettings();
			_originalThemeId = AppResult.ThemeId;
		}

		private void SettingsBox_Load(object sender, EventArgs e)
		{
			Theme.Apply(this);
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
			labelTemp.Text = Lang.Get("settings.tempLocation");
			ReloadTempMode();
			groupLang.Text = Lang.Get("settings.language");
			btnLangImport.Text = Lang.Get("settings.lang.import");
			btnLangExport.Text = Lang.Get("settings.lang.export");
			groupTheme.Text = Lang.Get("settings.theme");
			btnThemeImport.Text = Lang.Get("settings.theme.import");
			btnThemeExport.Text = Lang.Get("settings.theme.export");
			groupUpdate.Text = Lang.Get("settings.autoUpdate");
			chkAutoUpdate.Text = Lang.Get("settings.autoUpdateDesc");
			groupKey.Text = Lang.Get("settings.key");
			btnKeySettings.Text = Lang.Get("settings.key.adjust");
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
			ReloadThemes();
			ReloadSizePresets();
			ReloadLayoutPresets();
			ReloadGuideLine();
		}

		/// <summary>
		/// 重填主题下拉：内置"跟随系统" + 设置文件中已导入的主题。
		/// </summary>
		private void ReloadThemes()
		{
			_applyingUi = true;
			try
			{
				// 先重建列表再计算选中项，避免用旧列表计算导致首次加载永远选中第一项
				_themeList = Theme.AvailableThemes();
				cmbTheme.Items.Clear();
				foreach (ThemeCon theme in _themeList)
					cmbTheme.Items.Add(theme.Name);
				int sel = _themeList.FindIndex(x => x.Id == Theme.CurrentId);
				cmbTheme.SelectedIndex = sel >= 0 ? sel : 0;
			}
			finally
			{
				_applyingUi = false;
			}
		}

		/// <summary>
		/// 重填语言下拉：内置语言 + 设置文件中已导入的语言，显示名用语言包的 name。
		/// </summary>
		private void ReloadLanguages()
		{
			// 先重建列表再计算选中项，避免用旧列表计算导致首次加载永远选中第一项
			_langList = Lang.AvailableLanguages();
			cmbLang.Items.Clear();
			foreach (LangCon lang in _langList)
				cmbLang.Items.Add(lang.Name);
			int sel = _langList.FindIndex(x => x.Id == AppResult.Language);
			cmbLang.SelectedIndex = sel >= 0 ? sel : 0;
		}

		/// <summary>重填临时文件位置下拉（内存/硬盘）。</summary>
		private void ReloadTempMode()
		{
			string[] keys = { "settings.temp.memory", "settings.temp.disk" };
			cmbTempMode.Items.Clear();
			foreach (string key in keys)
				cmbTempMode.Items.Add(Lang.Get(key));
			int sel = AppResult.TempImageMode == "disk" ? 1 : 0;
			cmbTempMode.SelectedIndex = sel;
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
			int fmtIdx = cmbSaveFormat.Items.IndexOf((Result.SaveFormat ?? "jpg").ToUpperInvariant());
			cmbSaveFormat.SelectedIndex = fmtIdx >= 0 ? fmtIdx : 0;
			int sizeIdx = Result.DefaultSize switch
			{
				2 => 1,
				3 => 2,
				_ => 0,
			};
			cmbSize.SelectedIndex = sizeIdx;
			cmbBgColor.SelectedIndex = ColorIndexFromStored(Result.BackgroundColor);
			// 防御性钳制：即使容差范围将来变化，非法导入值也不会让 trackBar 赋值崩溃
			trackBar.Value = Math.Max(trackBar.Minimum, Math.Min(trackBar.Maximum, Result.Tolerance));
			lblToleranceVal.Text = trackBar.Value.ToString();

			chkAllowExternal.Checked = AppResult.Privacy.AllowExternalAccess;
			ReloadTempMode();
			ReloadLanguages();
			chkAutoUpdate.Checked = AppResult.AutoUpdate;

			txtCustomW.Text = Result.CustomLayoutW.ToString();
			txtCustomH.Text = Result.CustomLayoutH.ToString();
			ReloadLayoutPresets();

			trackBarQuality.Value = Math.Max(70, Math.Min(100, Result.SaveQuality));
			lblQualityVal.Text = trackBarQuality.Value.ToString();
			ReloadGuideLine();
			ReloadThemes();
		}

		/// <summary>
		/// 将设置中存储的颜色值映射为下拉索引（0=蓝 1=红 2=白 3=透明）。
		/// </summary>
		private static int ColorIndexFromStored(string stored)
		{
			switch (stored)
			{
				case "蓝色": return 0;
				case "红色": return 1;
				case "透明": return 3;
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
					3 => "透明",
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
			AppResult.TempImageMode = cmbTempMode.SelectedIndex == 1 ? "disk" : "memory";
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
					// 空对象 {} 会反序列化为非 null 的默认包，先拦截，避免静默重置全部设置为默认值
					using (JsonDocument doc = JsonDocument.Parse(json))
					{
						if (doc.RootElement.ValueKind == JsonValueKind.Object
							&& !doc.RootElement.EnumerateObject().MoveNext())
						{
							MessageBox.Show(Lang.Get("msg.loadFailed", "invalid json"), Lang.Get("msg.error"),
								MessageBoxButtons.OK, MessageBoxIcon.Error);
							return;
						}
					}
					var pkg = SettingsPackage.FromJson(json);
					if (pkg == null || pkg.Gen == null || pkg.App == null)
					{
						MessageBox.Show(Lang.Get("msg.loadFailed", "invalid json"), Lang.Get("msg.error"),
							MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					// 容差等数值/枚举字段统一钳制到合法范围，坏文件不会让控件赋值崩溃
					Assalg.SanitizeGenSettings(pkg.Gen);
					// AppSettings 无公开清洗方法，此处内联兜底：null 与非法值回退默认
					pkg.App ??= new AppSettings();
					pkg.App.Privacy ??= new PrivacySettings();
					if (string.IsNullOrEmpty(pkg.App.Language)) pkg.App.Language = "zh-CN";
					if (string.IsNullOrEmpty(pkg.App.ThemeId)) pkg.App.ThemeId = "green";
					if (pkg.App.TempImageMode != "memory" && pkg.App.TempImageMode != "disk")
						pkg.App.TempImageMode = "memory";
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
							TempImageMode = cmbTempMode.SelectedIndex == 1 ? "disk" : "memory",
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
								3 => "透明",
								_ => "白色",
							},
							Tolerance = trackBar.Value,
							LayoutPreset = cmbLayoutPreset.SelectedIndex >= 0 ? cmbLayoutPreset.SelectedIndex : 0,
							CustomLayoutW = ParseCustomSize(txtCustomW.Text, Result.CustomLayoutW),
							CustomLayoutH = ParseCustomSize(txtCustomH.Text, Result.CustomLayoutH),
							GuideLineStyle = cmbGuideLine.SelectedIndex >= 0 ? cmbGuideLine.SelectedIndex : 0,
							SaveQuality = trackBarQuality.Value
						}
						// 语言包/主题包独立导入导出，不随设置包导出
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

		/// <summary>解析自定义尺寸文本框：非法值回退默认，100-10000 范围校验。</summary>
		private static int ParseCustomSize(string text, int fallback)
		{
			if (int.TryParse(text?.Trim(), out int v) && v >= 100 && v <= 10000)
				return v;
			return fallback;
		}

		/// <summary>软件目录（exe 所在目录）。</summary>
		private static string ExeDir =>
			Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) ?? ".";

		/// <summary>主题包目录（theme）。</summary>
		private static string ThemeDir
		{
			get
			{
				string dir = Path.Combine(ExeDir, "theme");
				if (!Directory.Exists(dir))
				{
					try { Directory.CreateDirectory(dir); } catch { }
				}
				return dir;
			}
		}

		/// <summary>语言包目录（lang）。</summary>
		private static string LangDir
		{
			get
			{
				string dir = Path.Combine(ExeDir, "lang");
				if (!Directory.Exists(dir))
				{
					try { Directory.CreateDirectory(dir); } catch { }
				}
				return dir;
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
				ofd.InitialDirectory = LangDir;
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
				sfd.InitialDirectory = LangDir;
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
		/// 主题下拉切换：记忆选择到 AppSettings.ThemeId 并立即应用（内置按 id，自定义从主题包加载）。
		/// </summary>
		private void CmbTheme_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_applyingUi) return;
			if (cmbTheme.SelectedIndex < 0 || cmbTheme.SelectedIndex >= _themeList.Count) return;

			ThemeCon con = _themeList[cmbTheme.SelectedIndex];
			// 记忆当前选择（内置 id 或自定义主题 id）
			Theme.SetCurrent(con.Id);
			// 同步到 AppResult，避免 OK 保存时用旧值覆盖磁盘上的主题
			AppResult.ThemeId = con.Id;
			Theme.Init();
			Theme.Apply(this);
		}

		/// <summary>
		/// 导入主题包：文件名须为 theme.{id}.{name}.json（id 为主题 id，name 为显示名），
		/// 内容为调色板本体（ass 结构，8 个键）。导入后注册到设置文件并立即应用。
		/// </summary>
		private void BtnThemeImport_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog ofd = new OpenFileDialog())
			{
				ofd.Filter = "主题包|theme.*.json|JSON 文件|*.json";
				ofd.Title = Lang.Get("settings.theme.import");
				ofd.InitialDirectory = ThemeDir;
				if (ofd.ShowDialog(this) != DialogResult.OK) return;

				try
				{
					// 从文件名提取主题 id 和显示名：theme.dark-blue.深蓝.json
					string fileName = Path.GetFileName(ofd.FileName);
					ThemeCon? info = ExtractThemeInfo(fileName);
					if (info == null)
					{
						MessageBox.Show(Lang.Get("msg.themeFileInvalid", fileName), Lang.Get("msg.error"),
							MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}

					string json = File.ReadAllText(ofd.FileName);
					var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
					if (dict == null || dict.Count == 0)
					{
						MessageBox.Show(Lang.Get("msg.loadFailed", "empty theme pack"), Lang.Get("msg.error"),
							MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					// 校验 8 个调色板键完整且颜色可解析，避免坏主题包写入设置文件
					if (!Theme.ValidatePalette(dict))
					{
						MessageBox.Show(Lang.Get("msg.loadFailed", "theme pack must contain 8 valid color keys"), Lang.Get("msg.error"),
							MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}

					Theme.Register(info.Id, info.Name, dict);
					Theme.Apply(this);
					ReloadThemes();
					MessageBox.Show(Lang.Get("msg.themeImported", info.Name), Lang.Get("msg.done"),
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
		/// 导出主题包：文件名 theme.{id}.{name}.json，内容为当前调色板本体（ass 结构，便于用户自行修改）。
		/// </summary>
		private void BtnThemeExport_Click(object sender, EventArgs e)
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "JSON 文件|*.json";
				sfd.Title = Lang.Get("settings.theme.export");
				sfd.InitialDirectory = ThemeDir;
				sfd.FileName = $"theme.{Theme.CurrentId}.{Theme.CurrentName}.json";
				if (sfd.ShowDialog(this) != DialogResult.OK) return;

				try
				{
					string json = JsonSerializer.Serialize(Theme.ExportTable(),
						new JsonSerializerOptions { WriteIndented = true });
					File.WriteAllText(sfd.FileName, json);
					MessageBox.Show(Lang.Get("msg.themeExported", sfd.FileName), Lang.Get("msg.done"),
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
		/// 从文件名提取主题信息：theme.dark-blue.深蓝.json → {id=dark-blue, name=深蓝}。
		/// 格式不符返回 null。
		/// </summary>
		private static ThemeCon? ExtractThemeInfo(string fileName)
		{
			if (string.IsNullOrEmpty(fileName)) return null;
			const string prefix = "theme.";
			if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
			if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return null;
			string middle = fileName.Substring(prefix.Length, fileName.Length - prefix.Length - ".json".Length);
			// middle = id.name，按第一个点分割，剩余部分作为显示名（显示名允许含点）
			int dot = middle.IndexOf('.');
			string id = dot >= 0 ? middle.Substring(0, dot) : middle;
			string name = dot >= 0 ? middle.Substring(dot + 1) : "";
			if (string.IsNullOrEmpty(id)) return null;
			if (string.IsNullOrEmpty(name)) name = id;
			return new ThemeCon { Id = id, Name = name };
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
			// 取消时回滚主题：Theme.SetCurrent 在切换时已写入磁盘，需恢复原始主题（预览与磁盘一并还原）
			if (Theme.CurrentId != _originalThemeId)
			{
				Theme.SetCurrent(_originalThemeId);
				Theme.Init();
				Theme.Apply(this);
			}
			DialogResult = DialogResult.Cancel;
			Close();
		}

		/// <summary>打开快捷键调整对话框。</summary>
		private void BtnKeySettings_Click(object sender, EventArgs e)
		{
			using (KeySettingsBox box = new KeySettingsBox())
			{
				box.ShowDialog(this);
			}
		}
	}
}
