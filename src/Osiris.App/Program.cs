using System;
using System.Windows.Forms;

namespace Osiris.App
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
#if NETFRAMEWORK
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#else
            ApplicationConfiguration.Initialize();
#endif
            Application.Run(new MainForm());
        }
    }
}
