using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.Utils
{
    internal class NativeMessageBox
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        public static void Show(string message)
        {
            MessageBox(IntPtr.Zero, message, "提示", 0);
        }

        public static void Show(string message, string title)
        {
            MessageBox(IntPtr.Zero, message, title, 0);
        }
    }
}
