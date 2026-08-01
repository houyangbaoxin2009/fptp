using System;
using System.Drawing;
using System.IO;
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
			Console.WriteLine("  fptp.exe prep batch -i C:\\in -o C:\\out -c blue -t 60 -a -l 0");
			Console.WriteLine("  fptp.exe prep batch -i C:\\in -o C:\\out --preset preset.json");
			Console.WriteLine("  fptp.exe ass save -i in.jpg -o out.jpg");
			Console.WriteLine("  fptp.exe ass checkres -i in.jpg -w 295 -h 413");
			Console.WriteLine("  fptp.exe ass settings");
			Console.WriteLine("  fptp.exe ass working -o out.jpg");
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
				"batch" => RunPrepBatch(args),
				_ => UnknownPrepCommand(command)
			};
		}

		static int UnknownPrepCommand(string command)
		{
			Console.WriteLine(Lang.Get("cli.unknownCommand", "prep", command, "crop grayscale bgcolor batch"));			return 1;
		}

		// ── prep batch：文件夹批处理（支持 --preset 预设文件 / 直接参数）──

		static int RunPrepBatch(string[] args)
		{
			string inputDir = ParseArgValue(args, "-i", "--input") ?? "";
			string outputDir = ParseArgValue(args, "-o", "--output") ?? "";

			if (string.IsNullOrEmpty(inputDir) || string.IsNullOrEmpty(outputDir))
			{
				Console.WriteLine("用法: fptp.exe prep batch -i <dir> -o <dir> [--preset <file.json>] [-t <tolerance>] [-a] [-l <layout>]");
				return 1;
			}

			if (!Directory.Exists(inputDir))
			{
				Console.WriteLine("Error: 输入目录不存在: " + inputDir);
				return 1;
			}

			// 参数来源：预设文件 > 命令行参数 > 默认值
			GenSettings gen = Assalg.LoadGenSettings();
			string presetFile = ParseArgValue(args, "--preset", "--preset") ?? "";
			if (!string.IsNullOrEmpty(presetFile))
			{
				if (!File.Exists(presetFile))
				{
					Console.WriteLine("Error: 预设文件不存在: " + presetFile);
					return 1;
				}
				try
				{
					gen = JsonSerializer.Deserialize<GenSettings>(File.ReadAllText(presetFile)) ?? gen;
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error: 预设文件解析失败: " + ex.Message);
					return 1;
				}
			}

			if (TryParseInt(ParseArgValue(args, "-t", "--tolerance"), out int t) && t >= 0 && t <= 150)
				gen.Tolerance = t;
			if (HasFlag(args, "-a", "--anime"))
				gen.AnimeMode = true;
			if (TryParseInt(ParseArgValue(args, "-l", "--layout"), out int l) && l >= 0 && l <= 4)
				gen.LayoutPreset = l;
			string colorName = ParseArgValue(args, "-c", "--color") ?? "";
			if (!string.IsNullOrEmpty(colorName))
			{
				Color c = Color.FromName(colorName);
				if (!c.IsKnownColor)
				{
					Console.WriteLine(Lang.Get("cli.unknownColor", colorName));
					return 1;
				}
				gen.BackgroundColor = MapColorToStored(colorName);
			}

			try
			{
				int total = BatchProcess(inputDir, outputDir, gen);
				Console.WriteLine(JsonSerializer.Serialize(new { success = true, processed = total, input = inputDir, output = outputDir }, JsonOptions));
				return total > 0 ? 0 : 1;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return 1;
			}
		}

		/// <summary>将 CLI 颜色名映射为存储的中文值。</summary>
		static string MapColorToStored(string colorName)
		{
			switch (colorName.ToLower())
			{
				case "blue": return "蓝色";
				case "red": return "红色";
				case "transparent": case "none": return "透明";
				default: return "白色";
			}
		}

		/// <summary>批量处理目录下全部图片：裁剪 + 换底色 + 排版（按预设勾选逻辑：默认全流程）。</summary>
		static int BatchProcess(string inputDir, string outputDir, GenSettings gen)
		{
			Directory.CreateDirectory(outputDir);
			string[] files = Directory.GetFiles(inputDir, "*.*", SearchOption.TopDirectoryOnly);
			int total = 0;

			foreach (string f in files)
			{
				string ext = Path.GetExtension(f).ToLower();
				if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".bmp") continue;

				using (Bitmap source = new Bitmap(f))
				{
					Bitmap cur = (Bitmap)source.Clone();
					try
					{
						int targetW = gen.DefaultSize switch { 2 => Basic.TWO_INCH_W, 3 => Basic.PASSPORT_W, _ => Basic.ONE_INCH_W };
						int targetH = gen.DefaultSize switch { 2 => Basic.TWO_INCH_H, 3 => Basic.PASSPORT_H, _ => Basic.ONE_INCH_H };
						Bitmap cropped = Prepalg.SmartCrop(cur, targetW, targetH);
						if (cropped != null) { cur.Dispose(); cur = cropped; }

						Color bg = gen.BackgroundColor switch
						{
							"蓝色" => Color.FromArgb(65, 105, 225),
							"红色" => Color.FromArgb(220, 20, 60),
							"透明" => Color.Transparent,
							_ => Color.White,
						};
						Bitmap bgDone = gen.AnimeMode
							? Prepalg.ReplaceBackgroundAnime(cur, bg, gen.Tolerance)
							: Prepalg.ReplaceBackground(cur, bg, gen.Tolerance);
						if (bgDone != null) { cur.Dispose(); cur = bgDone; }

						int pw, ph;
						switch (gen.LayoutPreset)
						{
							case 1: pw = Basic.LAYOUT_6INCH_W; ph = Basic.LAYOUT_6INCH_H; break;
							case 2: pw = Basic.LAYOUT_A4_W; ph = Basic.LAYOUT_A4_H; break;
							case 3: pw = Basic.LAYOUT_A5_W; ph = Basic.LAYOUT_A5_H; break;
							default: pw = Basic.LAYOUT_5INCH_W; ph = Basic.LAYOUT_5INCH_H; break;
						}
						Bitmap layout = MakeLayoutForCli(cur, pw, ph, gen);
						cur.Dispose();
						cur = layout;

						// 透明背景只能存 PNG
						string outExt = gen.BackgroundColor == "透明" ? ".png" : ".jpg";
						string outFile = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(f) + outExt);
						Assalg.SaveImage(cur, outFile, gen.SaveQuality);
						total++;
					}
					finally
					{
						cur.Dispose();
					}
				}
			}
			return total;
		}

		/// <summary>CLI 版排版生成。</summary>
		static Bitmap MakeLayoutForCli(Bitmap photo, int paperW, int paperH, GenSettings gen)
		{
			int gap = Basic.LAYOUT_GAP;
			int cols = Math.Max(1, (paperW + gap) / (photo.Width + gap));
			int rows = Math.Max(1, (paperH + gap) / (photo.Height + gap));
			int contentW = cols * photo.Width + (cols - 1) * gap;
			int contentH = rows * photo.Height + (rows - 1) * gap;
			int startX = (paperW - contentW) / 2;
			int startY = (paperH - contentH) / 2;

			Bitmap paper = new Bitmap(paperW, paperH);
			using (Graphics g = Graphics.FromImage(paper))
			{
				g.Clear(Color.White);
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				for (int r = 0; r < rows; r++)
					for (int c = 0; c < cols; c++)
						g.DrawImage(photo, startX + c * (photo.Width + gap), startY + r * (photo.Height + gap), photo.Width, photo.Height);
			}
			return paper;
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
					// 透明背景只能存 PNG
					if (bgColor.A == 0)
						outputPath = Path.ChangeExtension(outputPath, ".png");
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
				"working" => RunAssWorking(args),
				_ => UnknownAssCommand(command)
			};
		}

		static int UnknownAssCommand(string command)
		{
			Console.WriteLine(Lang.Get("cli.unknownCommand", "ass", command, "save checkres settings working"));
			return 1;
		}

		/// <summary>
		/// ass working：获取 GUI 正在处理中的图片（内存临时文件）。
		/// 向正在运行的 GUI 发送请求，GUI 把当前图片导出到 publish\working.png 后返回其路径。
		/// 用法：fptp.exe ass working [-o <out>]
		/// </summary>
		static int RunAssWorking(string[] args)
		{
			string outputPath = ParseArgValue(args, "-o", "--output") ?? "";
			string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? ".";
			string publishDir = Path.Combine(exeDir, "publish");
			string requestFile = Path.Combine(publishDir, "working.request");
			string resultFile = Path.Combine(publishDir, "working.png");

			try
			{
				Directory.CreateDirectory(publishDir);
				// 1. 删除上一轮残留结果，避免 GUI 未响应时误返回旧图
				try { if (File.Exists(resultFile)) File.Delete(resultFile); } catch { }
				// 2. 写入请求文件
				File.WriteAllText(requestFile, DateTime.Now.Ticks.ToString());

				// 2. 轮询等待 GUI 响应（最多 10 秒）
				DateTime deadline = DateTime.Now.AddSeconds(10);
				bool ok = false;
				while (DateTime.Now < deadline)
				{
					if (File.Exists(resultFile))
					{
						ok = true;
						break;
					}
					System.Threading.Thread.Sleep(200);
				}

				// 3. 清理请求文件（无论成败）
				try { if (File.Exists(requestFile)) File.Delete(requestFile); } catch { }

				if (!ok)
				{
					Console.WriteLine("Error: GUI 未运行或当前没有处理中的图片");
					return 1;
				}

				if (!string.IsNullOrEmpty(outputPath))
				{
					File.Copy(resultFile, outputPath, true);
					Console.WriteLine(JsonSerializer.Serialize(new { success = true, output = outputPath }, JsonOptions));
				}
				else
				{
					Console.WriteLine(JsonSerializer.Serialize(new { success = true, output = resultFile }, JsonOptions));
				}
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return 1;
			}
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
