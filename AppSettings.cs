namespace fptp
{
	public class AppSettings
	{
		public PrivacySettings Privacy { get; set; } = new();
	}

	public class PrivacySettings
	{
		public bool AllowExternalAccess { get; set; } = true;
	}
}
