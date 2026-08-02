using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace fptp
{
	/// <summary>
	/// 快捷键调整对话框：列出全部动作与当前快捷键，点击行后按下新组合键即可修改。
	/// </summary>
	public partial class KeySettingsBox : Form
	{
		/// <summary>动作显示顺序（固定，不随字典遍历顺序变化）。</summary>
		private static readonly string[] ActionOrder =
		{
			"reload", "undo", "settings", "about",
			"load", "unload", "crop", "grayscale", "changeBg",
			"layout", "save", "print", "batch",
		};

		private readonly Dictionary<string, string> current;

		public KeySettingsBox()
		{
			InitializeComponent();
			Theme.Apply(this); // 应用主题，避免深色/自定义主题下对话框保持默认浅色
			KeyPreview = true;
			current = new Dictionary<string, string>(Assalg.LoadKeySettings().Actions);
			ApplyLang();
			ReloadGrid();
		}

		private void ApplyLang()
		{
			Text = Lang.Get("key.title");
			lblHint.Text = Lang.Get("key.hint");
			colAction.HeaderText = Lang.Get("key.column.action");
			colCombo.HeaderText = Lang.Get("key.column.combo");
			btnReset.Text = Lang.Get("key.reset");
			btnOk.Text = Lang.Get("settings.ok");
			btnCancel.Text = Lang.Get("settings.cancel");
		}

		/// <summary>按固定顺序填充表格。</summary>
		private void ReloadGrid()
		{
			dgvKeys.Rows.Clear();
			foreach (string action in ActionOrder)
			{
				string label = Lang.Get("key.action." + action);
				string combo = current.TryGetValue(action, out string? c) ? c : "";
				dgvKeys.Rows.Add(label, combo);
				dgvKeys.Rows[dgvKeys.Rows.Count - 1].Tag = action;
			}
			dgvKeys.ClearSelection();
		}

		/// <summary>点击行进入录制：按下新组合键时写入该行。</summary>
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			bool hasModifier = (keyData & (Keys.Control | Keys.Alt | Keys.Shift)) != 0;

			// 纯导航键（方向键/翻页/Tab 等，无修饰键）不录制，放行给 DataGridView 用于移动选中行
			if (!hasModifier)
			{
				Keys key = keyData & Keys.KeyCode;
				if (key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right ||
					key == Keys.PageUp || key == Keys.PageDown || key == Keys.Home || key == Keys.End ||
					key == Keys.Tab || key == Keys.Escape || key == Keys.Enter)
					return base.ProcessCmdKey(ref msg, keyData);
				// 无修饰键的普通按键不录制，防止误把单键绑定成快捷键
				return base.ProcessCmdKey(ref msg, keyData);
			}

			string combo = KeySettings.FormatKeys(keyData);
			if (combo != "" && dgvKeys.SelectedRows.Count > 0)
			{
				DataGridViewRow row = dgvKeys.SelectedRows[0];
				if (row.Tag is string action)
				{
					// 组合键已被其他动作占用时提示并放弃覆盖（等于当前动作自身绑定则放行，无副作用）
					foreach (var kv in current)
					{
						if (kv.Key != action && kv.Value == combo)
						{
							MessageBox.Show(this, Lang.Get("key.hint"), Lang.Get("msg.tip"),
								MessageBoxButtons.OK, MessageBoxIcon.Warning);
							return true;
						}
					}
					current[action] = combo;
					row.Cells[1].Value = combo;
					return true;
				}
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void BtnReset_Click(object sender, EventArgs e)
		{
			current.Clear();
			foreach (var kv in new KeySettings().Actions)
				current[kv.Key] = kv.Value;
			ReloadGrid();
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			Assalg.SaveKeySettings(new KeySettings { Actions = current });
			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
