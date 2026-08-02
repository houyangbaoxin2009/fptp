using System;
using System.IO;
using System.Net;
using System.Text.Json;

namespace fptp
{
	/// <summary>
	/// 用户地区检测。通过 IP 定位服务判断用户所在国家，供自动更新选择源。
	/// </summary>
	public static class RegionDetector
	{
		// IP 定位服务列表（依次尝试，取第一个成功结果）
		private static readonly string[] GeoApis =
		{
			"https://api.ip.sb/geoip",  // country_code（国内外均可用）
			"http://ip-api.com/json/"   // countryCode（国外稳定，免费版仅 HTTP）
		};

		private static bool? _isChina;
		private static readonly object _lock = new object();

		/// <summary>
		/// 是否中国用户（进程内缓存，只缓存成功结果）。全部服务失败时默认中国（国内用户为主），
		/// 失败不缓存，下次调用重新检测，避免首次网络失败把非中国用户永久判定为国内。
		/// </summary>
		public static bool IsChina()
		{
			if (_isChina.HasValue) return _isChina.Value;
			bool? detected = Detect();               // 网络 I/O 在锁外执行，不阻塞其他调用方
			if (detected.HasValue)
			{
				lock (_lock)
				{
					if (!_isChina.HasValue) _isChina = detected;
				}
			}
			return _isChina ?? true;                 // 失败不缓存，下次重试；默认中国
		}

		/// <summary>
		/// 依次请求 IP 定位服务解析国家。全部失败返回 null。
		/// </summary>
		private static bool? Detect()
		{
			foreach (string url in GeoApis)
			{
				try
				{
					bool? result = ParseChina(Request(url));
					if (result.HasValue) return result;
				}
				catch
				{
					// 单个服务失败继续尝试下一个
				}
			}
			return null;
		}

		/// <summary>
		/// 请求 URL 返回响应文本。
		/// </summary>
		private static string Request(string url)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "GET";
			request.Timeout = 3000;
			request.ReadWriteTimeout = 3000;   // 响应体读取同样限时，防 ReadToEnd 卡死
			request.UserAgent = "FPTP-Updater/1.0";

			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
			using (Stream stream = response.GetResponseStream())
			using (StreamReader reader = new StreamReader(stream))
			{
				return reader.ReadToEnd();
			}
		}

		/// <summary>
		/// 从定位服务 JSON 解析是否中国。无法识别返回 null。
		/// </summary>
		private static bool? ParseChina(string json)
		{
			using (JsonDocument doc = JsonDocument.Parse(json))
			{
				JsonElement root = doc.RootElement;
				if (root.TryGetProperty("data", out JsonElement data) &&
					data.ValueKind == JsonValueKind.Object)
				{
					return EvalCountry(data);
				}
				return EvalCountry(root);
			}
		}

		/// <summary>
		/// 评估国家字段：CN/China/中国 为中国，其它国家为国外，未识别返回 null。
		/// </summary>
		private static bool? EvalCountry(JsonElement el)
		{
			foreach (string key in new[] { "country_code", "countryCode", "country" })
			{
				if (!el.TryGetProperty(key, out JsonElement value) ||
					value.ValueKind != JsonValueKind.String)
					continue;

				string country = value.GetString() ?? "";
				if (country.Equals("CN", StringComparison.OrdinalIgnoreCase) ||
					country.Equals("China", StringComparison.OrdinalIgnoreCase) ||
					country.Contains("中国"))
					return true;
				if (country.Length > 0) return false;
			}
			return null;
		}
	}
}
