namespace fptp
{
	public class AppSettings
	{
		public PrivacySettings Privacy { get; set; } = new();
		public string Language { get; set; } = "zh-CN";
		public bool AutoUpdate { get; set; } = true;
	}

	public class PrivacySettings
	{
		public bool AllowExternalAccess { get; set; } = true;
	}
}
