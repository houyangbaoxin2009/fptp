using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace fptp
{
	/// <summary>
	/// 自动更新。按用户地区从 GitCode 或 GitHub Releases 检查最新版本并下载安装。
	/// 中国用户走 GitCode，国外用户走 GitHub。
	/// </summary>
	public static class Updater
	{
		// GitCode API：获取仓库所有 Releases（数组，首个为最新）
		private const string GitCodeReleasesApi =
			"https://api.gitcode.com/api/v5/repos/jiro2025/fptp/releases";

		// GitHub API：结构与 GitCode 一致，供国外用户使用
		private const string GitHubReleasesApi =
			"https://api.github.com/repos/houyangbaoxin2009/fptp/releases";

		private const string InstallerName = "FPTP-Setup.exe";

		/// <summary>
		/// 启动时静默检查（后台线程）。发现新版本才弹窗。
		/// </summary>
		public static void CheckSilent(Form owner)
		{
			ThreadPool.QueueUserWorkItem(_ =>
			{
				try
				{
					LatestRelease? latest = FetchLatestRelease();
					if (latest == null) return;

					Version current = new Version(Basic.AppVersion);
					Version? remote = ParseTagVersion(latest.TagName ?? "");
					if (remote == null || remote <= current) return;

					owner.BeginInvoke(new Action(() => PromptUpdate(owner, latest)));
				}
				catch
				{
					// 静默检查失败不打扰用户
				}
			});
		}

		/// <summary>
		/// 手动检查更新（前台线程，有结果反馈）。
		/// </summary>
		public static void CheckManual(Form owner)
		{
			try
			{
				LatestRelease? latest = FetchLatestRelease();
				if (latest == null)
				{
					MessageBox.Show(owner, Lang.Get("update.none"), Lang.Get("msg.tip"),
						MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}

				Version current = new Version(Basic.AppVersion);
				Version? remote = ParseTagVersion(latest.TagName ?? "");
				if (remote == null || remote <= current)
				{
					MessageBox.Show(owner, Lang.Get("update.none"), Lang.Get("msg.tip"),
						MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}

				PromptUpdate(owner, latest);
			}
			catch (Exception ex)
			{
				MessageBox.Show(owner, Lang.Get("update.failed", ex.Message), Lang.Get("msg.error"),
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private class LatestRelease
		{
			public string? TagName { get; set; }
			public string? Body { get; set; }
			public string? DownloadUrl { get; set; }
		}

		/// <summary>
		/// 按地区选择源获取最新 Release。所选源请求失败时自动回退另一平台。
		/// </summary>
		private static LatestRelease? FetchLatestRelease()
		{
			string primary = RegionDetector.IsChina() ? GitCodeReleasesApi : GitHubReleasesApi;
			string fallback = primary == GitCodeReleasesApi ? GitHubReleasesApi : GitCodeReleasesApi;

			try
			{
				return TryFetch(primary);
			}
			catch
			{
				return TryFetch(fallback);
			}
		}

		/// <summary>
		/// 从指定平台 API 获取最新 Release 的 tag 与首个安装包下载地址。
		/// </summary>
		private static LatestRelease? TryFetch(string api)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(api);
			request.Method = "GET";
			request.Timeout = 10000;
			request.UserAgent = "FPTP-Updater/1.0";

			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
			using (Stream stream = response.GetResponseStream())
			using (StreamReader reader = new StreamReader(stream))
			{
				string json = reader.ReadToEnd();
				using (JsonDocument doc = JsonDocument.Parse(json))
				{
					if (doc.RootElement.ValueKind != JsonValueKind.Array ||
						doc.RootElement.GetArrayLength() == 0)
						return null;

					JsonElement first = doc.RootElement[0];
					string? tag = first.TryGetProperty("tag_name", out JsonElement tagEl)
						? tagEl.GetString() : null;
					string? body = first.TryGetProperty("body", out JsonElement bodyEl)
						? bodyEl.GetString() : null;

					string? download = null;
					if (first.TryGetProperty("assets", out JsonElement assetsEl) &&
						assetsEl.ValueKind == JsonValueKind.Array)
					{
						foreach (JsonElement asset in assetsEl.EnumerateArray())
						{
							if (asset.TryGetProperty("browser_download_url", out JsonElement urlEl))
							{
								string? url = urlEl.GetString();
								if (!string.IsNullOrEmpty(url) &&
									url!.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
								{
									download = url;
									break;
								}
							}
						}
					}

					if (string.IsNullOrEmpty(tag)) return null;
					return new LatestRelease { TagName = tag, Body = body, DownloadUrl = download };
				}
			}
		}

		/// <summary>
		/// 解析 tag（如 v1.2.0.0）为 Version。
		/// </summary>
		private static Version? ParseTagVersion(string tag)
		{
			string clean = tag.TrimStart('v', 'V');
			if (Version.TryParse(clean, out Version v)) return v;
			return null;
		}

		/// <summary>
		/// 弹窗询问是否下载更新，确认后下载并启动安装程序。
		/// </summary>
		private static void PromptUpdate(Form owner, LatestRelease latest)
		{
			string message = Lang.Get("update.available", latest.TagName ?? "?", Basic.AppVersion);
			if (!string.IsNullOrEmpty(latest.Body))
				message += "\n\n" + latest.Body!.Trim();

			DialogResult dr = MessageBox.Show(owner, message, Lang.Get("update.title"),
				MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (dr != DialogResult.Yes) return;

			if (string.IsNullOrEmpty(latest.DownloadUrl))
			{
				MessageBox.Show(owner, Lang.Get("update.downloadFailed", "no installer asset"),
					Lang.Get("msg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			try
			{
				owner.Cursor = Cursors.WaitCursor;
				string tmpPath = Path.Combine(Path.GetTempPath(), InstallerName);
				DownloadFile(latest.DownloadUrl!, tmpPath);
				owner.Cursor = Cursors.Default;

				MessageBox.Show(owner, Lang.Get("update.downloaded"), Lang.Get("msg.tip"),
					MessageBoxButtons.OK, MessageBoxIcon.Information);

				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
				{
					FileName = tmpPath,
					UseShellExecute = true
				});
				owner.Close();
			}
			catch (Exception ex)
			{
				owner.Cursor = Cursors.Default;
				MessageBox.Show(owner, Lang.Get("update.downloadFailed", ex.Message),
					Lang.Get("msg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// 下载文件到本地路径。
		/// </summary>
		private static void DownloadFile(string url, string savePath)
		{
			using (WebClient client = new WebClient())
			{
				client.Headers.Add("User-Agent", "FPTP-Updater/1.0");
				client.DownloadFile(url, savePath);
			}
		}
	}
}
