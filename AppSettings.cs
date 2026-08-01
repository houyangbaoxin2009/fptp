using System.Text.Json.Serialization;

namespace fptp
{
	public class AppSettings
	{
		[JsonPropertyName("privacy")]
		public PrivacySettings Privacy { get; set; } = new();

		[JsonPropertyName("language")]
		public string Language { get; set; } = "zh-CN";

		[JsonPropertyName("autoUpdate")]
		public bool AutoUpdate { get; set; } = true;

		/// <summary>当前主题 id：green=护眼绿（默认），auto=跟随系统，light/dark/blue=内置，或导入主题包 id。</summary>
		[JsonPropertyName("themeId")]
		public string ThemeId { get; set; } = "green";

		/// <summary>处理中图片临时文件位置：memory=仅内存（隐私，不落盘），disk=写入 publish 目录。</summary>
		[JsonPropertyName("tempImageMode")]
		public string TempImageMode { get; set; } = "memory";
	}

	public class PrivacySettings
	{
		[JsonPropertyName("allowExternalAccess")]
		public bool AllowExternalAccess { get; set; } = true;
	}
}
