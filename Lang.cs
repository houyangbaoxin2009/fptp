using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace fptp
{
	/// <summary>
	/// 多语言支持。语言包来源优先级：
	/// 1. 设置文件中的语言包（Assalg.LoadLangPackage，用户导入/翻译）
	/// 2. 程序集内嵌 JSON 语言包（内置 zh-CN / en-US）
	/// </summary>
	public static class Lang
	{
		/// <summary>当前语言代码（如 zh-CN）。</summary>
		public static string Current { get; private set; } = "zh-CN";

		/// <summary>当前语言显示名（如 简体中文）。</summary>
		public static string CurrentName { get; private set; } = "简体中文";

		private static Dictionary<string, string> _table = new Dictionary<string, string>();

		/// <summary>内置语言 id。</summary>
		public static readonly string[] BuiltInLangs = { "zh-CN", "en-US" };

		/// <summary>
		/// 加载指定语言的翻译表。优先级：
		/// 1. 设置文件中的语言包（Assalg.LoadLangPackage，用户导入/翻译）
		/// 2. exe\lang\ 目录语言包文件（可编辑，编译后随软件分发）
		/// 3. 程序集内嵌 JSON 语言包（内置 zh-CN / en-US）
		/// 4. 最终回退内置中文
		/// </summary>
		public static void Load(string code)
		{
			string target = string.IsNullOrEmpty(code) ? "zh-CN" : code;

			// 1. 设置文件中的语言包（con.id 匹配目标语言）
			LangPackage? pkg = Assalg.LoadLangPackage();
			if (pkg != null && pkg.Con != null && pkg.Con.Id == target && pkg.Ass != null && pkg.Ass.Count > 0)
			{
				_table = new Dictionary<string, string>(pkg.Ass);
				Current = target;
				CurrentName = string.IsNullOrEmpty(pkg.Con.Name) ? target : pkg.Con.Name;
				return;
			}

			// 2. exe\lang\ 目录语言包文件
			if (TryLoadFile(target))
				return;

			// 3. 内置嵌入资源
			if (TryLoadEmbedded(target))
				return;

			// 4. 最终回退内置中文
			TryLoadEmbedded("zh-CN");
		}

		/// <summary>从 exe\lang\ 目录加载语言包文件（lang.{code}.json 或 lang.{code}.{name}.json）。</summary>
		private static bool TryLoadFile(string code)
		{
			try
			{
				string dir = Path.Combine(
					Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) ?? ".", "lang");
				if (!Directory.Exists(dir)) return false;

				// 兼容导出文件名 lang.{id}.{name}.json 与标准 lang.{id}.json
				string path = Path.Combine(dir, $"lang.{code}.json");
				if (!File.Exists(path))
				{
					string prefix = $"lang.{code}.";
					path = Directory.GetFiles(dir, "*.json")
						.FirstOrDefault(f => Path.GetFileName(f).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
					if (path == null) return false;
				}

				var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
				if (dict == null || dict.Count == 0) return false;
				_table = dict;
				Current = code;
				string nameKey = code == "zh-CN" ? "settings.lang.zh" : "settings.lang.en";
				CurrentName = _table.TryGetValue(nameKey, out string name) ? name : code;
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>从程序集嵌入资源加载语言包。</summary>
		private static bool TryLoadEmbedded(string code)
		{
			string resName = $"fptp.Resources.lang.{code}.json";
			try
			{
				using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName))
				{
					if (stream == null) return false;
					using (StreamReader reader = new StreamReader(stream))
					{
						var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadToEnd());
						if (dict == null) return false;
						_table = dict;
						Current = code;
						// 显示名取语言包自身的 settings.lang.zh/en
						string nameKey = code == "zh-CN" ? "settings.lang.zh" : "settings.lang.en";
						CurrentName = _table.TryGetValue(nameKey, out string name) ? name : code;
						return true;
					}
				}
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// 注册（导入）语言包并写入设置文件。id 为语言 id，name 为显示名，table 为语言包本体。
		/// </summary>
		public static void Register(string id, string name, Dictionary<string, string> table)
		{
			Assalg.SaveLangPackage(new LangPackage
			{
				Con = new LangCon { Id = id, Name = string.IsNullOrEmpty(name) ? id : name },
				Ass = table
			});
		}

		/// <summary>获取当前语言包字典（导出用，返回副本）。</summary>
		public static Dictionary<string, string> ExportTable()
		{
			return new Dictionary<string, string>(_table);
		}

		/// <summary>当前语言 id（如 zh-CN）。</summary>
		public static string CurrentId => Current;

		/// <summary>当前语言显示名（如 简体中文）。</summary>
		public static string CurrentDisplayName => CurrentName;

		/// <summary>
		/// 可用语言列表：内置语言 + 设置文件中已导入的语言（含显示名）。
		/// </summary>
		public static List<LangCon> AvailableLanguages()
		{
			var list = new List<LangCon>
			{
				new LangCon { Id = "zh-CN", Name = Get("settings.lang.zh") },
				new LangCon { Id = "en-US", Name = Get("settings.lang.en") }
			};
			LangPackage? pkg = Assalg.LoadLangPackage();
			if (pkg != null && pkg.Con != null && !string.IsNullOrEmpty(pkg.Con.Id) &&
				!pkg.Con.Id.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) &&
				!pkg.Con.Id.Equals("en-US", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(new LangCon
				{
					Id = pkg.Con.Id,
					Name = string.IsNullOrEmpty(pkg.Con.Name) ? pkg.Con.Id : pkg.Con.Name
				});
			}
			return list;
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
		/// 译文含非法占位符（如多余的括号）时回退原文，避免损坏的语言包导致崩溃。
		/// </summary>
		public static string Get(string key, params object[] args)
		{
			try
			{
				return string.Format(Get(key), args);
			}
			catch (FormatException)
			{
				return Get(key);
			}
		}
	}
}
