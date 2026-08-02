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
		// per_page=100 分页拉全，避免 release 多时只取到第一页漏掉最新版本
		private const string GitCodeReleasesApi =
			"https://api.gitcode.com/api/v5/repos/jiro2025/fptp/releases?per_page=100";

		// GitHub API：结构与 GitCode 一致，供国外用户使用
		private const string GitHubReleasesApi =
			"https://api.github.com/repos/houyangbaoxin2009/fptp/releases?per_page=100";

		private const string InstallerName = "FPTP-Setup.exe";

		// 防重入标记：同一时刻只允许一个更新检查（启动检查 + 手动检查互斥）
		private static int _checking;

		/// <summary>安全回 UI 线程：owner 已销毁/句柄不可用时直接放弃（避免 BeginInvoke 抛 ObjectDisposedException）。</summary>
		private static bool SafeBeginInvoke(Form owner, Action action)
		{
			if (owner == null || owner.IsDisposed || !owner.IsHandleCreated) return false;
			try { owner.BeginInvoke(action); return true; }
			catch { return false; }
		}

		/// <summary>
		/// 启动时静默检查（后台线程）。发现新版本才弹窗。
		/// </summary>
		public static void CheckSilent(Form owner)
		{
			ThreadPool.QueueUserWorkItem(_ =>
			{
				// 防重入：同一时刻只允许一个更新检查，避免并发重复请求
				if (Interlocked.CompareExchange(ref _checking, 1, 0) != 0) return;
				try
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
					}				}
				finally
				{
					Interlocked.Exchange(ref _checking, 0);
				}
			});
		}

		/// <summary>
		/// 手动检查更新（后台线程，有结果反馈，结果回 UI 线程弹窗）。
		/// </summary>
		public static void CheckManual(Form owner)
		{
			ThreadPool.QueueUserWorkItem(_ =>
			{
				// 防重入：同一时刻只允许一个更新检查
				if (Interlocked.CompareExchange(ref _checking, 1, 0) != 0) return;
				try
				{
					try
					{
						LatestRelease? latest = FetchLatestRelease();
						if (latest == null)
						{
							SafeBeginInvoke(owner, () => MessageBox.Show(owner,
								Lang.Get("update.none"), Lang.Get("msg.tip"),
								MessageBoxButtons.OK, MessageBoxIcon.Information));
							return;
						}

						Version current = new Version(Basic.AppVersion);
						Version? remote = ParseTagVersion(latest.TagName ?? "");
						if (remote == null || remote <= current)
						{
							SafeBeginInvoke(owner, () => MessageBox.Show(owner,
								Lang.Get("update.none"), Lang.Get("msg.tip"),
								MessageBoxButtons.OK, MessageBoxIcon.Information));
							return;
						}

						SafeBeginInvoke(owner, () => PromptUpdate(owner, latest));
					}
					catch (Exception ex)
					{
						// 回 UI 线程弹错误。catch 内不再直接 BeginInvoke（owner 已销毁时
						// 二次 BeginInvoke 会再次抛出，成为线程池未处理异常导致进程崩溃），
						// 一律走 SafeBeginInvoke 安全兜底。
						SafeBeginInvoke(owner, () => MessageBox.Show(owner,
							Lang.Get("update.failed", ex.Message), Lang.Get("msg.error"),
							MessageBoxButtons.OK, MessageBoxIcon.Error));
					}
				}
				finally
				{
					Interlocked.Exchange(ref _checking, 0);
				}
			});
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
						// 预发布（RC/Beta）不作为稳定更新提供
						if (release.TryGetProperty("prerelease", out JsonElement prEl) &&
							prEl.ValueKind == JsonValueKind.True)
							continue;

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
							// 优先匹配安装包名（InstallerName），避免同一 Release 附带
							// 多个 .exe（如便携版）时选中错误文件；无匹配再退化任意 .exe
							foreach (JsonElement asset in assetsEl.EnumerateArray())
							{
								// GitCode 资产可能用 url 字段而非 GitHub 的 browser_download_url，两字段都兼容
								string? url = null;
								if (asset.TryGetProperty("browser_download_url", out JsonElement bUrl))
									url = bUrl.GetString();
								if (string.IsNullOrEmpty(url) && asset.TryGetProperty("url", out JsonElement aUrl))
									url = aUrl.GetString();

								if (!string.IsNullOrEmpty(url) &&
									url!.EndsWith(InstallerName, StringComparison.OrdinalIgnoreCase))
								{
									download = url;
									break;
								}
							}
							if (download == null)
							{
								foreach (JsonElement asset in assetsEl.EnumerateArray())
								{
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
						}

						// 无安装包资产的 Release 不能作为更新，跳过（避免“提示更新但下载失败”）
						if (string.IsNullOrEmpty(download)) continue;

						best = new LatestRelease { TagName = tag, Body = body, DownloadUrl = download };
						bestVersion = ver;
					}
					// 全部 Release 都没有安装包资产时返回 null，由调用方回退另一平台
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

			// 唯一临时名，避免与同时运行的其他实例/残留冲突
			string tmpPath = Path.Combine(Path.GetTempPath(),
				$"{Path.GetFileNameWithoutExtension(InstallerName)}-{Guid.NewGuid():N}.exe");

			// 下载在后台线程执行，UI 线程不阻塞；完成后回 UI 线程弹窗并启动安装
			ThreadPool.QueueUserWorkItem(_ =>
			{
				try
				{
					DownloadFile(latest.DownloadUrl!, tmpPath);
					SafeBeginInvoke(owner, () =>
					{
						MessageBox.Show(owner, Lang.Get("update.downloaded"), Lang.Get("msg.tip"),
							MessageBoxButtons.OK, MessageBoxIcon.Information);

						// 启动安装器失败（杀软隔离、文件损坏等）不崩溃：清理临时文件并提示
						try
						{
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
							TryDelete(tmpPath);
							MessageBox.Show(owner, Lang.Get("update.downloadFailed", ex.Message), Lang.Get("msg.error"),
								MessageBoxButtons.OK, MessageBoxIcon.Error);
						}
					});
				}
				catch (Exception ex)
				{
					// 下载失败清理临时文件，避免残留 %TEMP%
					TryDelete(tmpPath);
					SafeBeginInvoke(owner, () => MessageBox.Show(owner,
						Lang.Get("update.downloadFailed", ex.Message), Lang.Get("msg.error"),
						MessageBoxButtons.OK, MessageBoxIcon.Error));
				}
			});
		}

		/// <summary>
		/// 下载文件到本地路径。HttpWebRequest 流式下载：支持超时，完成后校验
		/// 文件是有效 PE（MZ 头）而非错误页/空文件；中途异常清理临时文件。
		/// </summary>
		private static void DownloadFile(string url, string savePath)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "GET";
			request.Timeout = 15000;
			request.ReadWriteTimeout = 60000;   // 下载 60 秒无数据传输即超时，避免界面假死
			request.UserAgent = "FPTP-Updater/1.0";

			try
			{
				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				using (Stream src = response.GetResponseStream())
				using (FileStream dst = new FileStream(savePath, FileMode.Create, FileAccess.Write))
				{
					long expected = response.ContentLength;   // 服务器提供的 Content-Length，分块传输时为 -1
					byte[] buffer = new byte[81920];
					long written = 0;
					int read;
					while ((read = src.Read(buffer, 0, buffer.Length)) > 0)
					{
						dst.Write(buffer, 0, read);
						written += read;
					}

					// 完整性校验：服务器给了 Content-Length 时必须一致，防中途断流截断
					if (expected >= 0 && written != expected)
					{
						TryDelete(savePath);
						throw new IOException("downloaded file size mismatch");
					}
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
			catch
			{
				// 下载中途任何异常（网络断开等）都清理临时文件，避免残留 %TEMP%
				TryDelete(savePath);
				throw;
			}
		}

		private static void TryDelete(string path)
		{
			try { if (File.Exists(path)) File.Delete(path); } catch { }
		}
	}
}
