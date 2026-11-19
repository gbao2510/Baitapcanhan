using System;
using System.Windows.Forms;

namespace Bai2_BroadcastServerGUI
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // cau hinh chung cho ung dung Windows Forms (font, DPI, visual style)
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
