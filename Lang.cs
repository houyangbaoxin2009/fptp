using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace fptp
{
	/// <summary>
	/// 多语言支持。加载嵌入的 JSON 语言包，通过 key 获取翻译文本。
	/// </summary>
	public static class Lang
	{
		/// <summary>当前语言代码（zh-CN / en-US）。</summary>
		public static string Current { get; private set; } = "zh-CN";

		private static Dictionary<string, string> _table = new Dictionary<string, string>();

		/// <summary>
		/// 加载指定语言的翻译表。失败时回退中文。
		/// </summary>
		public static void Load(string code)
		{
			string target = code == "en-US" ? "en-US" : "zh-CN";
			string resName = $"fptp.Resources.lang.{target}.json";
			try
			{
				using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName))
				{
					if (stream == null)
					{
						FallbackToZh();
						return;
					}
					using (StreamReader reader = new StreamReader(stream))
					{
						var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadToEnd());
						if (dict == null)
						{
							FallbackToZh();
							return;
						}
						_table = dict;
						Current = target;
					}
				}
			}
			catch
			{
				FallbackToZh();
			}
		}

		private static void FallbackToZh()
		{
			_table = new Dictionary<string, string>();
			Current = "zh-CN";
		}

		/// <summary>
		/// 获取指定 key 的翻译文本，未找到时返回 key 本身。
		/// </summary>
		public static string Get(string key)
		{
			if (_table.TryGetValue(key, out string text))
				return text;
			return key;
		}

		/// <summary>
		/// 获取带占位符的翻译文本（string.Format）。
		/// </summary>
		public static string Get(string key, params object[] args)
		{
			return string.Format(Get(key), args);
		}
	}
}
