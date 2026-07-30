using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace fptp
{
	public class AppSettings
	{
		public string SaveFormat { get; set; } = "jpg";
		public int DefaultSize { get; set; } = 1;
		public string BackgroundColor { get; set; } = "蓝色";
		public int Tolerance { get; set; } = 60;
	}

	public static class SettingsManager
	{
		private static readonly string SettingsFile = Path.Combine(
			Path.GetDirectoryName(Application.ExecutablePath), "gen_setting.json");

		public static AppSettings Load()
		{
			try
			{
				if (File.Exists(SettingsFile))
				{
					string json = File.ReadAllText(SettingsFile);
					return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
				}
			}
			catch
			{
			}
			return new AppSettings();
		}

		public static void Save(AppSettings settings)
		{
			try
			{
				string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(SettingsFile, json);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"保存设置失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
}
