namespace fptp
{
	public class AppSettings
	{
		public PrivacySettings Privacy { get; set; } = new();
		public string Language { get; set; } = "zh-CN";
		public bool AutoUpdate { get; set; } = true;

		/// <summary>当前主题 id：auto=跟随系统，light/dark/green/blue=内置，或导入主题包 id。</summary>
		public string ThemeId { get; set; } = "auto";

		/// <summary>处理中图片临时文件位置：memory=仅内存（隐私，不落盘），disk=写入 publish 目录。</summary>
		public string TempImageMode { get; set; } = "memory";
	}

	public class PrivacySettings
	{
		public bool AllowExternalAccess { get; set; } = true;
	}
}
