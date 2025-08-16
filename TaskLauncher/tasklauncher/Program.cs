using System;
using System.Windows.Forms;

namespace TaskLauncher
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // High DPI awareness (crisp UI on 125%/150%/200% etc.)
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
