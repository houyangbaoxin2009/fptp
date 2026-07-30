using System.Text.Json;
using System.Text.Json.Serialization;

namespace fptp
{
	public class SettingsPackage
	{
		[JsonPropertyName("app")]
		public AppSettings App { get; set; } = new();

		[JsonPropertyName("gen")]
		public GenSettings Gen { get; set; } = new();

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
