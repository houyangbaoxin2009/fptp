using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;

namespace fptp
{
	static class Program
	{
		[DllImport("kernel32.dll")]
		private static extern bool AttachConsole(int dwProcessId);

		[DllImport("kernel32.dll")]
		private static extern bool FreeConsole();

		private const int ATTACH_PARENT_PROCESS = -1;

		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length > 0)
			{
				AttachConsole(ATTACH_PARENT_PROCESS);
				int exitCode = RunCommandMode(args);
				FreeConsole();
				Environment.Exit(exitCode);
				return;
			}

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new mainBox());
		}

		/// <summary>
		/// 命令行入口。子命令模式（不以 - 开头）或旧版参数兼容。
		/// 支持 --lang zh-CN|en-US 指定输出语言。
		/// </summary>
		static int RunCommandMode(string[] args)
		{
			// ── 语言参数（全局，任意位置）──
			string langCode = ParseArgValue(args, "--lang", "--lang") ?? "";
			if (langCode != "" && langCode != "zh-CN" && langCode != "en-US")
			{
				Console.WriteLine("Error: unknown language. Available: zh-CN en-US");
				return 1;
			}
			Lang.Load(langCode == "" ? "zh-CN" : langCode);

			// ── 子命令模式 ──
			if (args.Length > 0 && !args[0].StartsWith("-"))
				return RunSubCommand(args);

			// ── 旧版参数兼容 ──
			if (HasFlag(args, "-v", "--version"))
			{
				Console.WriteLine(Basic.GetAppTitle());
				return 0;
			}

			string inputPath = ParseArgValue(args, "-i", "--input") ?? "";
			string outputPath = ParseArgValue(args, "-o", "--output") ?? "";
			string sizeType = ParseArgValue(args, "-s", "--size") ?? "1";

			if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
			{
				Console.WriteLine(Lang.Get("cli.missingPath"));
				return 1;
			}

			try
			{
				using (Bitmap source = new Bitmap(inputPath))
				{
					int targetW, targetH;
					if (sizeType == "2")
					{
						targetW = Basic.TWO_INCH_W;
						targetH = Basic.TWO_INCH_H;
					}
					else
					{
						targetW = Basic.ONE_INCH_W;
						targetH = Basic.ONE_INCH_H;
					}

					using (Bitmap result = Prepalg.SmartCrop(source, targetW, targetH))
					{
						Assalg.SaveImage(result, outputPath);
					}
				}

				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return 1;
			}
		}

		// ═══════════════════════════════════════════
		//  子命令模式
		// ═══════════════════════════════════════════

		static int RunSubCommand(string[] args)
		{
			if (args.Length < 2)
			{
				PrintUsage();
				return 1;
			}

			string module = args[0].ToLower();
			string command = args[1].ToLower();

			// 提取模块参数（去掉前两个）
			string[] modArgs = new string[args.Length - 2];
			if (modArgs.Length > 0)
				Array.Copy(args, 2, modArgs, 0, modArgs.Length);

			return module switch
			{
				"basic" => RunBasicCommand(command, modArgs),
				"prep" => RunPrepCommand(command, modArgs),
				"ass" => RunAssCommand(command, modArgs),
				_ => UnknownModule(module)
			};
		}

		static void PrintUsage()
		{
			Console.WriteLine(Lang.Get("cli.usage"));
			Console.WriteLine("");
			Console.WriteLine(Lang.Get("cli.modules"));
			Console.WriteLine(Lang.Get("cli.modBasic"));
			Console.WriteLine(Lang.Get("cli.modPrep"));
			Console.WriteLine(Lang.Get("cli.modAss"));
			Console.WriteLine("");
			Console.WriteLine(Lang.Get("cli.examples"));
			Console.WriteLine("  fptp.exe basic info");
			Console.WriteLine("  fptp.exe basic version");
			Console.WriteLine("  fptp.exe prep crop -i in.jpg -o out.jpg -w 295 -h 413");
			Console.WriteLine("  fptp.exe prep grayscale -i in.jpg -o out.jpg");
			Console.WriteLine("  fptp.exe prep bgcolor -i in.jpg -o out.jpg -c white -t 40 -a");
			Console.WriteLine("  fptp.exe ass save -i in.jpg -o out.jpg");
			Console.WriteLine("  fptp.exe ass checkres -i in.jpg -w 295 -h 413");
			Console.WriteLine("  fptp.exe ass settings");
		}

		static int UnknownModule(string module)
		{
			Console.WriteLine(Lang.Get("cli.unknownModule", module));
			return 1;
		}

		// ── basic ──

		static int RunBasicCommand(string command, string[] args)
		{
			switch (command)
			{
				case "info":
					var info = new
					{
						appName = Basic.AppName,
						version = Basic.AppVersion,
						copyright = Basic.AppCopyright,
						company = Basic.AppCompany,
						sizes = new
						{
							oneInch = new { width = Basic.ONE_INCH_W, height = Basic.ONE_INCH_H },
							twoInch = new { width = Basic.TWO_INCH_W, height = Basic.TWO_INCH_H },
							passport = new { width = Basic.PASSPORT_W, height = Basic.PASSPORT_H }
						}
					};
					Console.WriteLine(JsonSerializer.Serialize(info, JsonOptions));
					return 0;

				case "version":
					Console.WriteLine(Basic.GetAppTitle());
					return 0;

				default:
					Console.WriteLine(Lang.Get("cli.unknownCommand", "basic", command, "info version"));
					return 1;
			}
		}

		// ── prep ──

		static int RunPrepCommand(string command, string[] args)
		{
			return command switch
			{
				"crop" => RunPrepCrop(args),
				"grayscale" => RunPrepGrayscale(args),
				"bgcolor" => RunPrepBgColor(args),
				_ => UnknownPrepCommand(command)
			};
		}

		static int UnknownPrepCommand(string command)
		{
			Console.WriteLine(Lang.Get("cli.unknownCommand", "prep", command, "crop grayscale bgcolor"));			return 1;
		}

		static int RunPrepCrop(string[] args)
		{
			string inputPath = ParseArgValue(args, "-i", "--input") ?? "";
			string outputPath = ParseArgValue(args, "-o", "--output") ?? "";

			if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
			{
				Console.WriteLine("用法: fptp.exe prep crop -i <input> -o <output> -w <width> -h <height>");
				return 1;
			}

			if (!TryParseInt(ParseArgValue(args, "-w", "--width"), out int width) || width <= 0)
			{
				Console.WriteLine(Lang.Get("cli.invalidWidth"));
				return 1;
			}

			if (!TryParseInt(ParseArgValue(args, "-h", "--height"), out int height) || height <= 0)
			{
				Console.WriteLine(Lang.Get("cli.invalidHeight"));
				return 1;
			}

			try
			{
				using (Bitmap source = new Bitmap(inputPath))
				using (Bitmap result = Prepalg.SmartCrop(source, width, height))
				{
					if (result == null) { Console.WriteLine(Lang.Get("cli.cropFailed")); return 1; }
					Assalg.SaveImage(result, outputPath);
				}
				Console.WriteLine(JsonSerializer.Serialize(new { success = true, output = outputPath, width, height }, JsonOptions));
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return 1;
			}
		}

		static int RunPrepGrayscale(string[] args)
		{
			string inputPath = ParseArgValue(args, "-i", "--input") ?? "";
			string outputPath = ParseArgValue(args, "-o", "--output") ?? "";

			if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
			{
				Console.WriteLine("用法: fptp.exe prep grayscale -i <input> -o <output>");
				return 1;
			}

			try
			{
				using (Bitmap source = new Bitmap(inputPath))
				using (Bitmap result = Prepalg.ToGrayscale(source))
				{
					Assalg.SaveImage(result, outputPath);
				}
				Console.WriteLine(JsonSerializer.Serialize(new { success = true, output = outputPath }, JsonOptions));
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return 1;
			}
		}

		static int RunPrepBgColor(string[] args)
		{
			string inputPath = ParseArgValue(args, "-i", "--input") ?? "";
			string outputPath = ParseArgValue(args, "-o", "--output") ?? "";
			string colorName = ParseArgValue(args, "-c", "--color") ?? "white";

			if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
			{
				Console.WriteLine("用法: fptp.exe prep bgcolor -i <input> -o <output> -c <color> -t <tolerance> [-a]");
				return 1;
			}

			Color bgColor = Color.FromName(colorName);
			if (!bgColor.IsKnownColor)
			{
				Console.WriteLine(Lang.Get("cli.unknownColor", colorName));
				return 1;
			}

			if (!TryParseInt(ParseArgValue(args, "-t", "--tolerance"), out int tolerance) || tolerance < 0 || tolerance > 150)
			{
				Console.WriteLine(Lang.Get("cli.invalidTolerance"));
				return 1;
			}

			// 动画模式：连通域洪泛填充，保护眼白等被主体包围的相似色区域
			bool anime = HasFlag(args, "-a", "--anime");

			try
			{
				using (Bitmap source = new Bitmap(inputPath))
				using (Bitmap result = anime
					? Prepalg.ReplaceBackgroundAnime(source, bgColor, tolerance)
					: Prepalg.ReplaceBackground(source, bgColor, tolerance))
				{
					Assalg.SaveImage(result, outputPath);
				}
				Console.WriteLine(JsonSerializer.Serialize(new { success = true, output = outputPath, anime }, JsonOptions));
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return 1;
			}
		}

		// ── ass ──

		static int RunAssCommand(string command, string[] args)
		{
			return command switch
			{
				"save" => RunAssSave(args),
				"checkres" => RunAssCheckRes(args),
				"settings" => RunAssSettings(args),
				_ => UnknownAssCommand(command)
			};
		}

		static int UnknownAssCommand(string command)
		{
			Console.WriteLine(Lang.Get("cli.unknownCommand", "ass", command, "save checkres settings"));
			return 1;
		}

		static int RunAssSave(string[] args)
		{
			string inputPath = ParseArgValue(args, "-i", "--input") ?? "";
			string outputPath = ParseArgValue(args, "-o", "--output") ?? "";

			if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
			{
				Console.WriteLine("用法: fptp.exe ass save -i <input> -o <output>");
				return 1;
			}

			try
			{
				using (Bitmap source = new Bitmap(inputPath))
				{
					Assalg.SaveImage(source, outputPath);
				}
				Console.WriteLine(JsonSerializer.Serialize(new { success = true, output = outputPath }, JsonOptions));
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return 1;
			}
		}

		static int RunAssCheckRes(string[] args)
		{
			string inputPath = ParseArgValue(args, "-i", "--input") ?? "";

			if (string.IsNullOrEmpty(inputPath))
			{
				Console.WriteLine("用法: fptp.exe ass checkres -i <input> -w <width> -h <height>");
				return 1;
			}

			if (!TryParseInt(ParseArgValue(args, "-w", "--width"), out int minW) || minW <= 0)
			{
				Console.WriteLine(Lang.Get("cli.invalidWidth"));
				return 1;
			}

			if (!TryParseInt(ParseArgValue(args, "-h", "--height"), out int minH) || minH <= 0)
			{
				Console.WriteLine(Lang.Get("cli.invalidHeight"));
				return 1;
			}

			try
			{
				using (Bitmap source = new Bitmap(inputPath))
				{
					bool ok = Assalg.CheckResolution(source, minW, minH);
					Console.WriteLine(JsonSerializer.Serialize(new
					{
						success = true,
						meetsRequirement = ok,
						imageWidth = source.Width,
						imageHeight = source.Height,
						minWidth = minW,
						minHeight = minH
					}, JsonOptions));
					return ok ? 0 : 1;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return 1;
			}
		}

		static int RunAssSettings(string[] args)
		{
			try
			{
				GenSettings settings = Assalg.LoadGenSettings();
				Console.WriteLine(JsonSerializer.Serialize(settings, JsonOptions));
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return 1;
			}
		}

		// ═══════════════════════════════════════════
		//  辅助方法
		// ═══════════════════════════════════════════

		private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
		{
			WriteIndented = true
		};

		/// <summary>检查参数列表中是否存在指定 flag。</summary>
		private static bool HasFlag(string[] args, string shortName, string longName)
		{
			foreach (string arg in args)
			{
				if (string.Equals(arg, shortName, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(arg, longName, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		/// <summary>解析 -k value 或 --key value 参数值。</summary>
		private static string? ParseArgValue(string[] args, string shortName, string longName)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (string.Equals(args[i], shortName, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(args[i], longName, StringComparison.OrdinalIgnoreCase))
				{
					if (i + 1 < args.Length)
						return args[i + 1];
					return null;
				}
			}
			return null;
		}

		private static bool TryParseInt(string? s, out int value)
		{
			if (s != null && int.TryParse(s, out value))
				return true;
			value = 0;
			return false;
		}
	}
}
