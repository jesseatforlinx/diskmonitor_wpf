using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Ookii.Dialogs.Wpf;

namespace DiskMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, (ProgressBar Bar, Label Label, StackPanel Panel)> driveWidgets = new Dictionary<string, (ProgressBar, Label, StackPanel)>();
        private DispatcherTimer timer;
        private string configFile = "drives.txt";

        public MainWindow()
        {
            InitializeComponent();

            // 读取配置
            if (File.Exists(configFile))
            {
                foreach (var line in File.ReadAllLines(configFile))
                {
                    AddDrive(line.Trim());
                }
            }

            // 定时刷新
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) => UpdateDrives();
            timer.Start();
        }

        private void SaveConfig()
        {
            var drives = new List<string>();
            foreach (var item in DriveList.Items)
            {
                drives.Add(item.ToString());
            }
            File.WriteAllLines(configFile, drives);
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
            var sp = new StackPanel() { Margin = new Thickness(0, 0, 0, 2) };

            var bar = new ProgressBar() { Height = 13, Minimum = 0, Maximum = 100 };
            var label = new Label() { Content = drive, FontSize = 10, Foreground = System.Windows.Media.Brushes.Black };

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
                tuple.Label.Content = $"{drive} 剩 {FormatSize(info.AvailableFreeSpace)} / 共 {FormatSize(info.TotalSize)} ({percent:F1}% 已用)";

                // 根据使用率设置颜色
                if (percent >= 90)
                {
                    tuple.Bar.Foreground = Brushes.Red;
                }
                else
                {
                    tuple.Bar.Foreground = Brushes.DodgerBlue;
                }
                tuple.Bar.Background = Brushes.LightGray;
            }
            catch
            {
                if (driveWidgets.TryGetValue(drive, out var tuple))
                {
                    tuple.Bar.Value = 0;
                    tuple.Bar.Foreground = Brushes.Gray;
                    tuple.Label.Content = $"{drive} 无法读取";
                }
            }
        }

        private string FormatSize(long bytes)
        {
            double size = bytes;
            if (size < 1024 * 1024 * 1024)
                return $"{size / (1024 * 1024):F1} MB";
            else
                return $"{size / (1024 * 1024 * 1024):F1} GB";
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new VistaFolderBrowserDialog
            {
                Description = "请选择一个文件夹",
                UseDescriptionForTitle = true // 部分系统会把 Description 当标题显示
            };

            if (dlg.ShowDialog(this) == true)
            {
                string drive = System.IO.Path.GetPathRoot(dlg.SelectedPath);
                AddDrive(drive);
                SaveConfig();
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
            SaveConfig();
        }
    }
}