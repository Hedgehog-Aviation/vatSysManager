using System.Diagnostics;
using System.Net.NetworkInformation;
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

namespace vatSysManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string VatsysProcessName = "vatSys";
        private static readonly DispatcherTimer VatSysTimer = new();
        private static Canvas CurrentCanvas = null;

        public MainWindow()
        {
            InitializeComponent();

            VatSysCheck();
            VatSysTimer.Tick += VatSysTimer_Tick;
            VatSysTimer.Interval = new TimeSpan(0, 0, 1);
            VatSysTimer.Start();
        }

        private void VatSysTimer_Tick(object sender, EventArgs e)
        {
            VatSysCheck();
        }

        private void VatSysCheck()
        {
            // Get any running vatSys processes.
            var vatsysProcesses = Process.GetProcessesByName(VatsysProcessName);

            if (vatsysProcesses.Length > 0)
            {
                InitCheckCanvas.Visibility = Visibility.Visible;
                HomeButton.IsEnabled = false;
                HomeCanvas.Visibility = Visibility.Hidden;
                PluginsButton.IsEnabled = false;
                ProfilesButton.IsEnabled = false;
                SetupButton.IsEnabled = false;
                SetupCanvas.Visibility = Visibility.Hidden;
            }
            else
            {
                InitCheckCanvas.Visibility = Visibility.Hidden;
                HomeButton.IsEnabled = true;
                PluginsButton.IsEnabled = true;
                ProfilesButton.IsEnabled = true;
                SetupButton.IsEnabled = true;

                if (CurrentCanvas == null) HomeCanvas.Visibility = Visibility.Visible;
                else CurrentCanvas.Visibility = Visibility.Visible;
            }
        }

        private void VatSysClose()
        {
            // Get any running vatSys processes.
            var vatsysProcesses = Process.GetProcessesByName(VatsysProcessName);

            // Kill all running vatSys processes.
            if (vatsysProcesses.Length > 0)
            {
                foreach (var vatsysProcess in vatsysProcesses)
                    vatsysProcess.Kill();
            }
        }

        private void VatSysCloseButton_Click(object sender, RoutedEventArgs e)
        {
            VatSysClose();
        }

        private void VatSysLaunchButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(@"C:\Program Files (x86)\vatSys\bin\vatSys.exe");
            Environment.Exit(1);
        }

        private void SetupButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentCanvas = SetupCanvas;
            SetupCanvas.Visibility = Visibility.Visible;
            InitCheckCanvas.Visibility = Visibility.Hidden;
            HomeCanvas.Visibility = Visibility.Hidden;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentCanvas = HomeCanvas;
            SetupCanvas.Visibility = Visibility.Hidden;
            InitCheckCanvas.Visibility = Visibility.Hidden;
            HomeCanvas.Visibility = Visibility.Visible;
        }
    }
}