using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace fptp
{
	/// <summary>
	/// 语言信息：id 为语言 id（如 zh-CN），name 为显示名（如 简体中文）。
	/// </summary>
	public class LangCon
	{
		[JsonPropertyName("id")]
		public string Id { get; set; } = "zh-CN";

		[JsonPropertyName("name")]
		public string Name { get; set; } = "简体中文";
	}

	/// <summary>
	/// 语言包条目：con 为语言信息（id + 显示名），ass 为语言包本体（key → 译文）。
	/// </summary>
	public class LangPackage
	{
		[JsonPropertyName("con")]
		public LangCon Con { get; set; } = new LangCon();

		[JsonPropertyName("ass")]
		public Dictionary<string, string> Ass { get; set; } = new Dictionary<string, string>();
	}

	/// <summary>
	/// 主题信息：id 为主题 id（如 dark-blue），name 为显示名（如 深蓝）。
	/// </summary>
	public class ThemeCon
	{
		[JsonPropertyName("id")]
		public string Id { get; set; } = "dark";

		[JsonPropertyName("name")]
		public string Name { get; set; } = "深色";
	}

	/// <summary>
	/// 主题包条目：con 为主题信息（id + 显示名），ass 为主题本体（调色板，key → 颜色值）。
	/// 调色板键：windowBg panelBg textColor subText accent border buttonBg previewBg。
	/// </summary>
	public class ThemePackage
	{
		[JsonPropertyName("con")]
		public ThemeCon Con { get; set; } = new ThemeCon();

		[JsonPropertyName("ass")]
		public Dictionary<string, string> Ass { get; set; } = new Dictionary<string, string>();
	}

	/// <summary>
	/// 隐藏设置：安装程序写入、应用读取的不可见参数。
	/// 不在设置面板显示，不参与导入导出。
	/// </summary>
	public class HighSettings
	{
		/// <summary>文档格式：md / pdf / none（安装时选择）。</summary>
		[JsonPropertyName("docsFormat")]
		public string DocsFormat { get; set; } = "md";

		/// <summary>安装器语言：zh-CN / en-US，空表示未记录。</summary>
		[JsonPropertyName("installLang")]
		public string InstallLang { get; set; } = "";
	}

	/// <summary>
	/// 快捷键设置：动作名 → 组合键字符串（如 "Ctrl+R"）。
	/// </summary>
	public class KeySettings
	{
		[JsonPropertyName("actions")]
		public Dictionary<string, string> Actions { get; set; } = new Dictionary<string, string>
		{
			["reload"] = "Ctrl+R",        // 重新开始
			["undo"] = "Ctrl+Z",          // 撤回
			["settings"] = "Ctrl+,",      // 设置（Ctrl+C 与复制冲突，改用 Ctrl+,）
			["about"] = "Ctrl+A",         // 关于
			["load"] = "Ctrl+O",          // 加载图片
			["unload"] = "Ctrl+W",        // 卸载图片
			["crop"] = "Ctrl+Shift+C",    // 智能裁剪
			["grayscale"] = "Ctrl+G",     // 变黑白
			["changeBg"] = "Ctrl+B",      // 修改底色
			["layout"] = "Ctrl+L",        // 排版
			["save"] = "Ctrl+S",          // 导出
			["print"] = "Ctrl+P",         // 打印
			["batch"] = "Ctrl+Shift+B",   // 文件夹批处理
		};

		/// <summary>将 Keys 组合键格式化为字符串（如 Ctrl+Shift+C）。</summary>
		public static string FormatKeys(Keys keyData)
		{
			var parts = new List<string>();
			if ((keyData & Keys.Control) != 0) parts.Add("Ctrl");
			if ((keyData & Keys.Alt) != 0) parts.Add("Alt");
			if ((keyData & Keys.Shift) != 0) parts.Add("Shift");

			Keys key = keyData & Keys.KeyCode;
			if (key == Keys.None) return "";
			parts.Add(key == Keys.Oemcomma ? "," : key.ToString());
			return string.Join("+", parts);
		}

		/// <summary>将快捷键字符串解析为 Keys 组合（供 ProcessCmdKey 比对）。解析失败返回 None。</summary>
		public static Keys ParseKeys(string combo)
		{
			if (string.IsNullOrWhiteSpace(combo)) return Keys.None;
			Keys result = Keys.None;
			string[] parts = combo.Split('+');
			foreach (string part in parts)
			{
				string p = part.Trim();
				if (p.Equals("Ctrl", System.StringComparison.OrdinalIgnoreCase)) { result |= Keys.Control; continue; }
				if (p.Equals("Alt", System.StringComparison.OrdinalIgnoreCase)) { result |= Keys.Alt; continue; }
				if (p.Equals("Shift", System.StringComparison.OrdinalIgnoreCase)) { result |= Keys.Shift; continue; }
				if (p == ",") { result |= Keys.Oemcomma; continue; }
				if (System.Enum.TryParse(p, true, out Keys k))
					result |= k;
			}
			return result;
		}
	}

	/// <summary>
	/// 设置包：app（应用设置）、gen（生成设置）、lang（语言包）、high（隐藏设置）四部分。
	/// </summary>
	public class SettingsPackage
	{
		[JsonPropertyName("app")]
		public AppSettings App { get; set; } = new();

		[JsonPropertyName("gen")]
		public GenSettings Gen { get; set; } = new();

		[JsonPropertyName("lang")]
		public LangPackage Lang { get; set; } = new();

		[JsonPropertyName("theme")]
		public ThemePackage Theme { get; set; } = new();

		[JsonPropertyName("high")]
		public HighSettings High { get; set; } = new();

		[JsonPropertyName("key")]
		public KeySettings Key { get; set; } = new();

		public string ToJson()
		{
			return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
		}

		public static SettingsPackage? FromJson(string json)
		{
			return JsonSerializer.Deserialize<SettingsPackage>(json);
		}
	}
}
