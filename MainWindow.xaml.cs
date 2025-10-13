using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Ookii.Dialogs.Wpf;
using System.Collections.Specialized;

namespace DiskMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, (ProgressBar Bar, TextBlock Label, StackPanel Panel)> driveWidgets = new Dictionary<string, (ProgressBar, TextBlock, StackPanel)>();
        private DispatcherTimer timer;
        private string configFile = "drives.txt";

        public MainWindow()
        {
            InitializeComponent();

            // 从 Settings 读取盘符列表
            if (Properties.Settings.Default.Drives != null)
            {
                foreach (string drive in Properties.Settings.Default.Drives)
                {
                    AddDrive(drive);
                }
            }

            // 定时刷新
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) => UpdateDrives();
            timer.Start();
        }

        private void SaveDrivesToSettings()
        {
            var drives = new StringCollection();
            foreach (var item in DriveList.Items)
                drives.Add(item.ToString());

            Properties.Settings.Default.Drives = drives;
            Properties.Settings.Default.Save();
        }

        private void UpdateDrives()
        {
            foreach (var drive in driveWidgets.Keys)
            {
                UpdateDrive(drive);
            }
        }

        private void AddDrive(string drive)
        {
            if (driveWidgets.ContainsKey(drive)) return;

            DriveList.Items.Add(drive);

            // 创建每个盘符的 Panel
            var sp = new StackPanel();

            var bar = new ProgressBar() { Height = 13, Minimum = 0, Maximum = 100 };
            var label = new TextBlock() { Text = drive, FontSize = 12, Foreground = System.Windows.Media.Brushes.Black, Margin = new Thickness(0,2,0,2)};

            sp.Children.Add(label);
            sp.Children.Add(bar);

            PanelStack.Children.Add(sp);

            driveWidgets[drive] = (bar, label, sp);

            UpdateDrive(drive);
        }

        private void UpdateDrive(string drive)
        {
            try
            {
                var info = new DriveInfo(drive);
                if (!info.IsReady) throw new Exception();

                double percent = (double)(info.TotalSize - info.TotalFreeSpace) / info.TotalSize * 100;
                var tuple = driveWidgets[drive];

                tuple.Bar.Value = percent;
                tuple.Label.Text = $"{drive} 剩 {FormatSize(info.AvailableFreeSpace)} / 共 {FormatSize(info.TotalSize)} ({percent:F1}% 已用)";

                // 根据使用率设置颜色
                if (percent >= 90)
                {
                    tuple.Bar.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
                }
                else
                {
                    tuple.Bar.Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 215));
                }
                tuple.Bar.Background = Brushes.LightGray;
            }
            catch
            {
                if (driveWidgets.TryGetValue(drive, out var tuple))
                {
                    tuple.Bar.Value = 0;
                    tuple.Bar.Foreground = Brushes.Gray;
                    tuple.Label.Text = $"{drive} 无法读取";
                }
            }
        }

        private string FormatSize(long bytes)
        {
            double size = bytes;
            if (size < 1024 * 1024 * 1024)
                return $"{size / (1024 * 1024):F1} MB";
            else if (size < 1024L * 1024 * 1024 * 1024)
                return $"{size / (1024 * 1024 * 1024):F1} GB";
            else
                return $"{size / (1024L * 1024 * 1024 * 1024):F2} TB";
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
            {
                Description = "请选择一个文件夹",
                UseDescriptionForTitle = true
            };

            if (dlg.ShowDialog(this) == true)
            {
                string drive = System.IO.Path.GetPathRoot(dlg.SelectedPath);
                AddDrive(drive);
                SaveDrivesToSettings();
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DriveList.SelectedItem == null) return;
            string drive = DriveList.SelectedItem.ToString();

            if (driveWidgets.TryGetValue(drive, out var tuple))
            {
                PanelStack.Children.Remove(tuple.Panel);
                driveWidgets.Remove(drive);
            }

            DriveList.Items.Remove(drive);
            SaveDrivesToSettings();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 从设置加载上次位置和大小
            if (Properties.Settings.Default.WindowLeft >= 0 &&
                Properties.Settings.Default.WindowTop >= 0)
            {
                this.Left = Properties.Settings.Default.WindowLeft;
                this.Top = Properties.Settings.Default.WindowTop;
            }            

            // Avoid error when switching display
            var screenWidth = SystemParameters.VirtualScreenWidth;
            var screenHeight = SystemParameters.VirtualScreenHeight;

            if (this.Left < 0) this.Left = 0;
            if (this.Top < 0) this.Top = 0;
            if (this.Left + this.Width > screenWidth) this.Left = screenWidth - this.Width;
            if (this.Top + this.Height > screenHeight) this.Top = screenHeight - this.Height;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 保存当前窗口的位置和大小
            Properties.Settings.Default.WindowLeft = this.Left;
            Properties.Settings.Default.WindowTop = this.Top;

            Properties.Settings.Default.Save();
        }
    }
}