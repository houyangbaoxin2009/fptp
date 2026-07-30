using System;
using System.Drawing;
using System.Windows.Forms;

namespace fptp
{
	static class Program
	{
		/// <summary>
		/// 应用程序的主入口点
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			// 【新增】检测是否有命令行参数
			if (args.Length > 0)
			{
				// 如果有参数，进入命令行模式（静默处理）
				int exitCode = RunCommandMode(args);
				Environment.Exit(exitCode); // 处理完直接退出
				return;
			}

			// 【原有】如果没有参数，正常启动 GUI 界面
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new mainBox());
		}

		/// <summary>
		/// 命令行处理逻辑
		/// </summary>
		/// <returns>0: 成功, 1: 失败</returns>
		static int RunCommandMode(string[] args)
		{
			// 默认参数
			string inputPath = "";
			string outputPath = "";
			string sizeType = "1"; // 1=一寸, 2=二寸

			try
			{
				// 简单的参数解析
				// 预期格式: fptp.exe -i "输入.jpg" -o "输出.jpg" -s "1"
				for (int i = 0; i < args.Length; i++)
				{
					switch (args[i].ToLower())
					{
						case "-i":
						case "--input":
							inputPath = args[i + 1];
							i++; break;
						case "-o":
						case "--output":
							outputPath = args[i + 1];
							i++; break;
						case "-s":
						case "--size":
							sizeType = args[i + 1];
							i++; break;
					}
				}

				// 1. 验证参数
				if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
				{
					Console.WriteLine("Error: Missing input or output path.");
					return 1;
				}

				// 2. 加载图片
				using (Bitmap source = new Bitmap(inputPath))
				{
					// 3. 确定尺寸 (调用 Basic 类)
					int targetW, targetH;
					if (sizeType == "2")
					{
						targetW = Basic.TWO_INCH_W;
						targetH = Basic.TWO_INCH_H;
					}
					else // 默认一寸
					{
						targetW = Basic.ONE_INCH_W;
						targetH = Basic.ONE_INCH_H;
					}

					// 4. 执行智能裁剪 (调用 Prepalg 类)
					using (Bitmap result = Prepalg.SmartCrop(source, targetW, targetH))
					{
						// 5. 高质量保存 (调用 Assalg 类)
						Assalg.SaveImage(result, outputPath);
					}
				}

				// 成功返回 0
				return 0;
			}
			catch (Exception ex)
			{
				// 错误时输出日志到控制台
				Console.WriteLine("Error: " + ex.Message);
				return 1;
			}
		}
	}
}
