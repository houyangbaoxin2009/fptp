using System;
using System.Drawing;
using System.Runtime.InteropServices;
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

		static int RunCommandMode(string[] args)
		{
			foreach (string arg in args)
			{
				if (string.Equals(arg, "-v", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(arg, "--version", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine(Basic.GetAppTitle());
					return 0;
				}
			}

			string inputPath = "";
			string outputPath = "";
			string sizeType = "1";

			try
			{
				for (int i = 0; i < args.Length; i++)
				{
					switch (args[i].ToLower())
					{
						case "-i":
						case "--input":
							if (i + 1 >= args.Length) { Console.WriteLine("Error: Missing value for -i/--input."); return 1; }
							inputPath = args[i + 1];
							i++; break;
						case "-o":
						case "--output":
							if (i + 1 >= args.Length) { Console.WriteLine("Error: Missing value for -o/--output."); return 1; }
							outputPath = args[i + 1];
							i++; break;
						case "-s":
						case "--size":
							if (i + 1 >= args.Length) { Console.WriteLine("Error: Missing value for -s/--size."); return 1; }
							sizeType = args[i + 1];
							i++; break;
					}
				}

				if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
				{
					Console.WriteLine("Error: Missing input or output path.");
					return 1;
				}

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
	}
}
