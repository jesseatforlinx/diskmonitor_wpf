using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace DiskMonitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex? _mutex;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        protected override void OnStartup(StartupEventArgs e)
        {
            const string AppName = "Disk Manager";
            bool createdNew;

            _mutex = new Mutex(true, AppName, out createdNew);
            if (!createdNew)
            {
                // 找到已存在的进程
                Process current = Process.GetCurrentProcess();
                Process? existing = Process.GetProcessesByName(current.ProcessName)
                    .FirstOrDefault(p => p.Id != current.Id);

                if (existing != null)
                {
                    // 激活已有窗口
                    SetForegroundWindow(existing.MainWindowHandle);
                }

                Current.Shutdown();
                return;
            }

            base.OnStartup(e);
        }
    }

}
