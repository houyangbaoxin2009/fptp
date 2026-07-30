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
			cmbSaveFormat.Text = Result.SaveFormat.ToUpperInvariant();
			cmbSize.Text = Result.DefaultSize switch { 2 => "二寸", 3 => "小二寸", _ => "一寸" };
			cmbBgColor.Text = Result.BackgroundColor;
			trackBar.Value = Result.Tolerance;
			lblToleranceVal.Text = Result.Tolerance.ToString();

			chkAllowExternal.Checked = AppResult.Privacy.AllowExternalAccess;
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

			AppResult.Privacy.AllowExternalAccess = chkAllowExternal.Checked;
			Assalg.SaveAppSettings(AppResult);

			DialogResult = DialogResult.OK;
			Close();
		}

		private void ApplyToUI()
		{
			cmbSaveFormat.Text = Result.SaveFormat.ToUpperInvariant();
			cmbSize.Text = Result.DefaultSize switch { 2 => "二寸", 3 => "小二寸", _ => "一寸" };
			cmbBgColor.Text = Result.BackgroundColor;
			trackBar.Value = Result.Tolerance;
			lblToleranceVal.Text = Result.Tolerance.ToString();
			chkAllowExternal.Checked = AppResult.Privacy.AllowExternalAccess;
		}

		private void BtnReset_Click(object sender, EventArgs e)
		{
			var dr = MessageBox.Show("确认恢复所有设置为默认值？", "重置设置",
				MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (dr != DialogResult.Yes) return;

			Result = new GenSettings();
			AppResult = new AppSettings();
			ApplyToUI();
		}

		private void BtnImport_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog ofd = new OpenFileDialog())
			{
				ofd.Filter = "JSON 文件|*.json";
				ofd.Title = "导入设置";
				if (ofd.ShowDialog(this) != DialogResult.OK) return;

				try
				{
					string json = File.ReadAllText(ofd.FileName);

					// 先校验 JSON 根结构
					using (JsonDocument doc = JsonDocument.Parse(json))
					{
						JsonElement root = doc.RootElement;

						if (root.ValueKind != JsonValueKind.Object)
						{
							MessageBox.Show("根节点必须是对象。", "导入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
							return;
						}

						if (!root.TryGetProperty("app", out _))
						{
							MessageBox.Show("缺少 app 节。", "导入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
							return;
						}

						if (!root.TryGetProperty("gen", out JsonElement genEl))
						{
							MessageBox.Show("缺少 gen 节。", "导入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
							return;
						}

						// 校验 gen 中各字段
						if (!genEl.TryGetProperty("saveFormat", out JsonElement fmtEl) ||
							(fmtEl.GetString() != "jpg" && fmtEl.GetString() != "png"))
						{
							MessageBox.Show("gen.saveFormat 无效，应为 jpg 或 png。", "导入失败",
								MessageBoxButtons.OK, MessageBoxIcon.Error);
							return;
						}

						if (!genEl.TryGetProperty("defaultSize", out JsonElement sizeEl) ||
							sizeEl.GetInt32() < 1 || sizeEl.GetInt32() > 3)
						{
							MessageBox.Show("gen.defaultSize 无效，应为 1（一寸）、2（二寸）或 3（小二寸）。", "导入失败",
								MessageBoxButtons.OK, MessageBoxIcon.Error);
							return;
						}

						if (!genEl.TryGetProperty("backgroundColor", out JsonElement bgEl) ||
							string.IsNullOrEmpty(bgEl.GetString()))
						{
							MessageBox.Show("gen.backgroundColor 无效或为空。", "导入失败",
								MessageBoxButtons.OK, MessageBoxIcon.Error);
							return;
						}

						if (!genEl.TryGetProperty("tolerance", out JsonElement tolEl) ||
							tolEl.GetInt32() < 0 || tolEl.GetInt32() > 150)
						{
							MessageBox.Show("gen.tolerance 无效，应在 0-150 之间。", "导入失败",
								MessageBoxButtons.OK, MessageBoxIcon.Error);
							return;
						}
					}

					// 校验通过，反序列化应用
					var pkg = JsonSerializer.Deserialize<SettingsPackage>(json);
					Result = pkg.Gen;
					AppResult = pkg.App;
					ApplyToUI();
				}
				catch (JsonException)
				{
					MessageBox.Show("文件不是有效的 JSON 格式。", "导入失败",
						MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void BtnExport_Click(object sender, EventArgs e)
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "JSON 文件|*.json";
				sfd.Title = "导出设置";
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
							}
						},
						Gen = new GenSettings
						{
							SaveFormat = cmbSaveFormat.Text.ToLowerInvariant(),
							DefaultSize = cmbSize.Text switch
							{
								"二寸" => 2,
								"小二寸" => 3,
								_ => 1,
							},
							BackgroundColor = cmbBgColor.Text,
							Tolerance = trackBar.Value
						}
					};
					File.WriteAllText(sfd.FileName, pkg.ToJson());
				}
				catch (Exception ex)
				{
					MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
