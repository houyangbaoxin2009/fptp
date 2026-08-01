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
		// GitCode API：获取仓库所有 Releases（GitCode 升序、GitHub 降序，取版本最大者）
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

			// 主源异常或返回空都要回退另一平台
			try
			{
				return TryFetch(primary) ?? TryFetch(fallback);
			}
			catch
			{
				return TryFetch(fallback);
			}
		}

		/// <summary>
		/// 从指定平台 API 获取版本号最大的 Release（GitCode 数组升序、GitHub 降序）。
		/// </summary>
		private static LatestRelease? TryFetch(string api)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(api);
			request.Method = "GET";
			request.Timeout = 10000;
			request.ReadWriteTimeout = 10000;   // 防响应体读取卡死（默认 5 分钟）
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

					// 两平台返回顺序不同，遍历取版本号最大者
					LatestRelease? best = null;
					Version? bestVersion = null;
					foreach (JsonElement release in doc.RootElement.EnumerateArray())
					{
						string? tag = release.TryGetProperty("tag_name", out JsonElement tagEl)
							? tagEl.GetString() : null;
						if (string.IsNullOrEmpty(tag)) continue;

						Version? ver = ParseTagVersion(tag!);
						if (ver == null || (bestVersion != null && ver <= bestVersion))
							continue;

						string? body = release.TryGetProperty("body", out JsonElement bodyEl)
							? bodyEl.GetString() : null;

						string? download = null;
						if (release.TryGetProperty("assets", out JsonElement assetsEl) &&
							assetsEl.ValueKind == JsonValueKind.Array)
						{
							foreach (JsonElement asset in assetsEl.EnumerateArray())
							{
								// GitCode 资产可能用 url 字段而非 GitHub 的 browser_download_url，两字段都兼容
								string? url = null;
								if (asset.TryGetProperty("browser_download_url", out JsonElement bUrl))
									url = bUrl.GetString();
								if (string.IsNullOrEmpty(url) && asset.TryGetProperty("url", out JsonElement aUrl))
									url = aUrl.GetString();

								if (!string.IsNullOrEmpty(url) &&
									url!.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
								{
									download = url;
									break;
								}
							}
						}

						best = new LatestRelease { TagName = tag, Body = body, DownloadUrl = download };
						bestVersion = ver;
					}
					return best;
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
				// 唯一临时名，避免与同时运行的其他实例/残留冲突
				string tmpPath = Path.Combine(Path.GetTempPath(),
					$"{Path.GetFileNameWithoutExtension(InstallerName)}-{Guid.NewGuid():N}.exe");
				DownloadFile(latest.DownloadUrl!, tmpPath);
				owner.Cursor = Cursors.Default;

				MessageBox.Show(owner, Lang.Get("update.downloaded"), Lang.Get("msg.tip"),
					MessageBoxButtons.OK, MessageBoxIcon.Information);

				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
				{
					FileName = tmpPath,
					// 静默安装：更新流程不再让用户手动走安装向导
					Arguments = "/VERYSILENT /NORESTART /SUPPRESSMSGBOXES",
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
		/// 下载文件到本地路径。HttpWebRequest 流式下载：支持超时，完成后校验
		/// 文件是有效 PE（MZ 头）而非错误页/空文件。
		/// </summary>
		private static void DownloadFile(string url, string savePath)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "GET";
			request.Timeout = 15000;
			request.ReadWriteTimeout = 60000;   // 下载 60 秒无数据传输即超时，避免界面假死
			request.UserAgent = "FPTP-Updater/1.0";

			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
			using (Stream src = response.GetResponseStream())
			using (FileStream dst = new FileStream(savePath, FileMode.Create, FileAccess.Write))
			{
				byte[] buffer = new byte[81920];
				int read;
				while ((read = src.Read(buffer, 0, buffer.Length)) > 0)
					dst.Write(buffer, 0, read);
			}

			// 校验：必须存在、非空、带 MZ 头（PE 可执行文件），否则视为下载失败
			FileInfo fi = new FileInfo(savePath);
			if (fi.Length < 1024 * 1024)
			{
				TryDelete(savePath);
				throw new IOException("downloaded file is too small to be a valid installer");
			}
			using (FileStream fs = new FileStream(savePath, FileMode.Open, FileAccess.Read))
			{
				byte[] header = new byte[2];
				if (fs.Read(header, 0, 2) != 2 ||
					header[0] != (byte)'M' || header[1] != (byte)'Z')
				{
					TryDelete(savePath);
					throw new IOException("downloaded file is not a valid executable");
				}
			}
		}

		private static void TryDelete(string path)
		{
			try { if (File.Exists(path)) File.Delete(path); } catch { }
		}
	}
}
