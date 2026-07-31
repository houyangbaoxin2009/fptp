using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

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
	/// 设置包：app（应用设置）、gen（生成设置）、lang（语言包）三部分。
	/// </summary>
	public class SettingsPackage
	{
		[JsonPropertyName("app")]
		public AppSettings App { get; set; } = new();

		[JsonPropertyName("gen")]
		public GenSettings Gen { get; set; } = new();

		[JsonPropertyName("lang")]
		public LangPackage Lang { get; set; } = new();

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
