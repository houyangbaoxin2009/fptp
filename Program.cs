using System;
using System.Collections.Generic;
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
			// 未处理异常兜底：任何 UI 线程 / 非 UI 线程异常都弹窗提示，绝不无声崩溃
			Application.ThreadException += (s, ex) => ShowFatal(ex.Exception);
			AppDomain.CurrentDomain.UnhandledException += (s, ex) => ShowFatal(ex.ExceptionObject as Exception);
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
			Application.Run(new mainBox());
		}

		/// <summary>未处理异常兜底：尽量弹窗提示错误，内部自兜底避免崩溃循环。</summary>
		private static void ShowFatal(Exception ex)
		{
			try
			{
				MessageBox.Show(ex?.Message ?? "Unknown error", Lang.Get("msg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			catch
			{
				// 弹窗本身失败也绝不再次抛出（防止崩溃循环）
			}
		}

		/// <summary>
		/// 命令行入口。子命令模式（不以 - 开头）或旧版参数兼容。
		/// 支持 --lang zh-CN|en-US 指定输出语言。
		/// </summary>
		static int RunCommandMode(string[] args)
		{
			// 先加载默认语言，保证 --lang 解析过程中的错误提示也能正常翻译（后续再按 --lang 切换）
			Lang.Load("zh-CN");

			// ── 语言参数（全局，任意位置）── 先剔除 --lang 及其值，避免干扰后续分支判断
			string langCode = "";
			var filtered = new List<string>();
			for (int i = 0; i < args.Length; i++)
			{
				if (string.Equals(args[i], "--lang", StringComparison.OrdinalIgnoreCase))
				{
					// 下一个 token 是值（非 - 开头）则跳过；否则视为缺值并报错
					if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
					{
						langCode = args[i + 1];
						i++;
					}
					else
					{
						Console.WriteLine(Lang.Get("cli.missingLangValue"));
						return 1;
					}
					continue;
				}
				filtered.Add(args[i]);
			}
			args = filtered.ToArray();

			if (langCode != "" && langCode != "zh-CN" && langCode != "en-US")
			{
				Console.WriteLine(Lang.Get("cli.unknownLanguage", langCode));
				return 1;
			}
			if (langCode != "")
				Lang.Load(langCode);

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
			string sizeType = "1";
			if (HasArg(args, "-s", "--size"))
			{
				string sv = ParseArgValue(args, "-s", "--size");
				if (sv == null)
				{
					Console.WriteLine(Lang.Get("cli.missingSizeValue"));
					return 1;
				}
				sizeType = sv;
			}

			// 旧版模式不允许未知选项或子命令混用：非 - 开头的 token（如子命令名）一律报错
			foreach (string arg in args)
			{
				if (!arg.StartsWith("-"))
				{
					Console.WriteLine(Lang.Get("cli.mixedArgs", arg));
					return 1;
				}
			}
			if (!CheckUnknownOptions(args, new[] { "-i", "--input", "-o", "--output", "-s", "--size", "-v", "--version" }))
				return 1;

			if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
			{
				Console.WriteLine(Lang.Get("cli.missingPath"));
				return 1;
			}

			if (sizeType != "1" && sizeType != "2")
			{
				Console.WriteLine(Lang.Get("cli.invalidSize"));
				return 1;
			}

			try
			{
				using (Bitmap source = LoadBitmapUnlocked(inputPath))
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
			// info / version 不接受任何参数
			if (!CheckUnknownOptions(args, new string[0]))
				return 1;

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
			if (!CheckUnknownOptions(args, new[] { "-i", "--input", "-o", "--output", "-c", "--color", "-t", "--tolerance", "-l", "--layout", "-a", "--anime", "--preset" }))
				return 1;

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
					Assalg.SanitizeGenSettings(gen);   // 预设文件可能手改出非法值，统一钳制
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error: 预设文件解析失败: " + ex.Message);
					return 1;
				}
			}

			// -t/--tolerance 与 -l/--layout：若选项出现但缺值或越界，严格报错退出（不再静默忽略）
			if (HasArg(args, "-t", "--tolerance"))
			{
				if (!TryParseInt(ParseArgValue(args, "-t", "--tolerance"), out int t) || t < 0 || t > 150)
				{
					Console.WriteLine(Lang.Get("cli.invalidTolerance"));
					return 1;
				}
				gen.Tolerance = t;
			}
			if (HasArg(args, "-a", "--anime"))
				gen.AnimeMode = true;
			if (HasArg(args, "-l", "--layout"))
			{
				if (!TryParseInt(ParseArgValue(args, "-l", "--layout"), out int l) || l < 0 || l > 4)
				{
					Console.WriteLine(Lang.Get("cli.invalidLayout"));
					return 1;
				}
				gen.LayoutPreset = l;
			}
			string colorName = ParseArgValue(args, "-c", "--color") ?? "";
			if (!string.IsNullOrEmpty(colorName))
			{
				// 白名单与 bgcolor 命令一致：white/blue/red/transparent/none，避免系统色名（如 Control）被误判为合法
				switch (colorName.ToLower())
				{
					case "white":
					case "blue":
					case "red":
					case "transparent":
					case "none":
						gen.BackgroundColor = MapColorToStored(colorName);
						break;
					default:
						Console.WriteLine(Lang.Get("cli.unknownColor", colorName));
						return 1;
				}
			}

			try
			{
				int total = BatchProcess(inputDir, outputDir, gen);
				Console.WriteLine(JsonSerializer.Serialize(new { success = total > 0, processed = total, input = inputDir, output = outputDir }, JsonOptions));
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

				// 单张失败不中断整批（损坏/被占用图片跳过，其余继续）
				try
				{
					using (Bitmap source = LoadBitmapUnlocked(f))
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
								case 4: pw = gen.CustomLayoutW; ph = gen.CustomLayoutH; break;
								default: pw = Basic.LAYOUT_5INCH_W; ph = Basic.LAYOUT_5INCH_H; break;
							}
							Bitmap layout = MakeLayoutForCli(cur, pw, ph, gen);
							cur.Dispose();
							cur = layout;

							// 透明背景只能存 PNG
							string outExt = (gen.BackgroundColor == "透明" || Assalg.HasAlpha(cur)) ? ".png" : ".jpg";
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
				catch
				{
					// 跳过损坏/被占用文件，不中断整批
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
			if (!CheckUnknownOptions(args, new[] { "-i", "--input", "-o", "--output", "-w", "--width", "-h", "--height" }))
				return 1;

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
				using (Bitmap source = LoadBitmapUnlocked(inputPath))
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
			if (!CheckUnknownOptions(args, new[] { "-i", "--input", "-o", "--output" }))
				return 1;

			string inputPath = ParseArgValue(args, "-i", "--input") ?? "";
			string outputPath = ParseArgValue(args, "-o", "--output") ?? "";

			if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
			{
				Console.WriteLine("用法: fptp.exe prep grayscale -i <input> -o <output>");
				return 1;
			}

			try
			{
				using (Bitmap source = LoadBitmapUnlocked(inputPath))
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
			if (!CheckUnknownOptions(args, new[] { "-i", "--input", "-o", "--output", "-c", "--color", "-t", "--tolerance", "-a", "--anime" }))
				return 1;

			string inputPath = ParseArgValue(args, "-i", "--input") ?? "";
			string outputPath = ParseArgValue(args, "-o", "--output") ?? "";
			string colorName = ParseArgValue(args, "-c", "--color") ?? "white";

			if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
			{
				Console.WriteLine("用法: fptp.exe prep bgcolor -i <input> -o <output> -c <color> -t <tolerance> [-a]");
				return 1;
			}

			Color bgColor;
			switch (colorName.ToLower())
			{
				case "blue": bgColor = Color.FromArgb(65, 105, 225); break;
				case "red": bgColor = Color.FromArgb(220, 20, 60); break;
				case "transparent":
				case "none": bgColor = Color.Transparent; break;
				case "white": bgColor = Color.White; break;
				default:
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
				using (Bitmap source = LoadBitmapUnlocked(inputPath))
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
			if (!CheckUnknownOptions(args, new[] { "-o", "--output" }))
				return 1;

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
			if (!CheckUnknownOptions(args, new[] { "-i", "--input", "-o", "--output" }))
				return 1;

			string inputPath = ParseArgValue(args, "-i", "--input") ?? "";
			string outputPath = ParseArgValue(args, "-o", "--output") ?? "";

			if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
			{
				Console.WriteLine("用法: fptp.exe ass save -i <input> -o <output>");
				return 1;
			}

			try
			{
				using (Bitmap source = LoadBitmapUnlocked(inputPath))
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
			if (!CheckUnknownOptions(args, new[] { "-i", "--input", "-w", "--width", "-h", "--height" }))
				return 1;

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
				using (Bitmap source = LoadBitmapUnlocked(inputPath))
				{
					bool ok = Assalg.CheckResolution(source, minW, minH);
					Console.WriteLine(JsonSerializer.Serialize(new
					{
						success = ok,
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
			// settings 不接受任何参数
			if (!CheckUnknownOptions(args, new string[0]))
				return 1;

			try
			{
				GenSettings settings = Assalg.LoadGenSettings();
				Console.WriteLine(JsonSerializer.Serialize(new { success = true, settings }, JsonOptions));
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
			WriteIndented = true,
			// 统一 camelCase 输出：GenSettings 等 PascalCase 属性转小写，与匿名对象字段风格一致
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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

		/// <summary>
		/// 判断参数列表中是否出现指定选项（-x / --xx 均可）。
		/// 与 HasFlag 相同，用于区分「选项完全未出现」与「选项出现但缺值」（后者 ParseArgValue 也返回 null）。
		/// </summary>
		private static bool HasArg(string[] args, string shortName, string longName)
		{
			return HasFlag(args, shortName, longName);
		}

		/// <summary>
		/// 校验参数列表中是否存在白名单之外的未知选项（- 开头的 token）。
		/// 遇到未知选项时打印错误并返回 false。
		/// </summary>
		private static bool CheckUnknownOptions(string[] args, string[] allowedFlags)
		{
			foreach (string arg in args)
			{
				if (!arg.StartsWith("-")) continue;   // 值 token，跳过
				bool known = false;
				foreach (string allowed in allowedFlags)
				{
					if (string.Equals(arg, allowed, StringComparison.OrdinalIgnoreCase))
					{
						known = true;
						break;
					}
				}
				if (!known)
				{
					Console.WriteLine(Lang.Get("cli.unknownOption", arg));
					return false;
				}
			}
			return true;
		}

		/// 以可共享读取方式加载位图并生成独立副本，立即释放文件锁，支持 in-place 保存（输出=输入）。
		/// 注意：不能用 tmp.Clone()——从流创建的 Bitmap 在流关闭后访问像素会抛 GDI+ 一般性错误。
		private static Bitmap LoadBitmapUnlocked(string path)
		{
			using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (Bitmap tmp = new Bitmap(fs))
				return new Bitmap(tmp);
		}

		/// <summary>解析 -k value 或 --key value 参数值。</summary>
		private static string? ParseArgValue(string[] args, string shortName, string longName)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (string.Equals(args[i], shortName, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(args[i], longName, StringComparison.OrdinalIgnoreCase))
				{
					// 缺值（下一个是 flag 或已越界）返回 null，避免把 -o 等 flag 当值吞掉
					if (i + 1 >= args.Length || args[i + 1].StartsWith("-"))
						return null;
					return args[i + 1];
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
