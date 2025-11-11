using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static vatSysManager.MainWindow;

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
        private static Settings Settings = null;
        private static HttpClient HttpClient = new();

        public MainWindow()
        {
            InitializeComponent();
            Init();
            VatSysCheck();
            VatSysTimer.Tick += VatSysTimer_Tick;
            VatSysTimer.Interval = new TimeSpan(0, 0, 1);
            VatSysTimer.Start();
        }

        private void Init()
        {
            InitSettings();
            HomeButton_Click(null, null);
        }

        private async Task InitProfiles()
        {
            var profiles = new List<ProfileOption>();

            ProfilesList.ItemsSource = profiles;

            var installed = ProfilesGetInstalled();

            profiles.AddRange(installed);

            var available = await ProfilesGetAvailable();

            foreach (var profile in available)
            {
                var existing = profiles.FirstOrDefault(x => x.Title == profile.Title);
                if (existing != null)
                {
                    existing.Url = profile.Url;
                    existing.CurrentVersion = profile.CurrentVersion;
                    continue;
                }
                profiles.Add(profile);
            }

            ProfilesList.ItemsSource = profiles;
        }

        private void InitSettings()
        {
            var settings = new Settings();

            var defaultProfileDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "vatSys Files", "Profiles");
            
            var defaultBaseDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "vatSys");

            if (Directory.Exists(defaultProfileDirectory))
            {
                settings.ProfileDirectory = defaultProfileDirectory;
            }

            if (Directory.Exists(defaultBaseDirectory))
            {
                settings.BaseDirectory = defaultBaseDirectory;
            }

            Settings = settings;
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
                ProfilesCanvas.Visibility = Visibility.Hidden;
            }
            else
            {
                InitCheckCanvas.Visibility = Visibility.Hidden;
                HomeButton.IsEnabled = true;
                ///PluginsButton.IsEnabled = true;
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
            ProfilesCanvas.Visibility = Visibility.Hidden;
            UpdaterCanvas.Visibility = Visibility.Hidden;

            if (Settings == null) return;
            BaseDirectoryTextBox.Text = Settings.BaseDirectory;
            ProfileDirectoryTextBox.Text = Settings.ProfileDirectory;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentCanvas = HomeCanvas;
            SetupCanvas.Visibility = Visibility.Hidden;
            InitCheckCanvas.Visibility = Visibility.Hidden;
            HomeCanvas.Visibility = Visibility.Visible;
            ProfilesCanvas.Visibility = Visibility.Hidden;
            UpdaterCanvas.Visibility = Visibility.Hidden;
        }

        private void ProfilesButton_Click(object sender, RoutedEventArgs e)
        {
            _ = InitProfiles();

            CurrentCanvas = ProfilesCanvas;
            SetupCanvas.Visibility = Visibility.Hidden;
            InitCheckCanvas.Visibility = Visibility.Hidden;
            HomeCanvas.Visibility = Visibility.Hidden;
            ProfilesCanvas.Visibility = Visibility.Visible;
            UpdaterCanvas.Visibility = Visibility.Hidden;
        }

        public class ProfileOption
        {
            public ProfileOption(string title, string url, bool installed = false)
            {
                Title = title;
                Url = url;
                Installed = installed;
            }

            public string Title { get; set; }
            public bool Installed { get; set; }
            public string LocalVersion { get; set; }
            public string CurrentVersion { get; set; }
            public string Url { get; set; } 
            public bool UpdateAvailable
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(LocalVersion)) return false;
                    if (LocalVersion != CurrentVersion) return true;
                    return false;
                }
            }
            public string DeleteCommand => $"Delete:Profile:{Title}";
        }


        private static async Task<List<ProfileOption>> ProfilesGetAvailable()
        {
            var profiles = new List<ProfileOption>();

            var url = "https://vatsys.sawbe.com/downloads/data/emptyprofiles/profiles.json";

            var response = await HttpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode) return profiles;

            var responseString = await response.Content.ReadAsStringAsync();

            var available = JsonConvert.DeserializeObject<List<ProfilesResponse>>(responseString);

            foreach (var profile in available)
            {
                var profileOption = new ProfileOption(profile.name, profile.path);

                var profileFile = $"{profile.path}/Profile.xml";

                var profileResponse = await HttpClient.GetAsync(profileFile);

                var contents = await profileResponse.Content.ReadAsStringAsync();

                profileOption.CurrentVersion = ProfileGetVersion(contents);

                profiles.Add(profileOption);
            }

            return profiles;
        }

        private static List<ProfileOption> ProfilesGetInstalled()
        {
            var profiles = new List<ProfileOption>();

            if (Settings == null || string.IsNullOrWhiteSpace(Settings.ProfileDirectory)) return profiles;

            foreach (var directory in Directory.GetDirectories(Settings.ProfileDirectory))
            {
                var profileOption = new ProfileOption(directory.Split('\\').Last(), null, true);

                var profileFile = System.IO.Path.Combine(directory, "Profile.xml");

                if (System.IO.File.Exists(profileFile))
                {
                    var contents = System.IO.File.ReadAllText(profileFile);

                    profileOption.LocalVersion = ProfileGetVersion(contents);
                }

                profiles.Add(profileOption);
            }

            return profiles;
        }

        private static string ProfileGetVersion(string contents)
        {
            var serializer = new XmlSerializer(typeof(Profile));

            using var reader = new StringReader(contents);

            var profileXml = (Profile)serializer.Deserialize(reader);

            if (!string.IsNullOrWhiteSpace(profileXml.Version.Revision))
            {
                return $"{profileXml.Version.AIRAC}.{profileXml.Version.Revision}";
            }

            return $"{profileXml.Version.AIRAC}";
        }

        private void UpdaterCanvasMode()
        {
            CurrentCanvas = UpdaterCanvas;
            SetupCanvas.Visibility = Visibility.Hidden;
            InitCheckCanvas.Visibility = Visibility.Hidden;
            HomeCanvas.Visibility = Visibility.Hidden;
            ProfilesCanvas.Visibility = Visibility.Hidden;
            UpdaterCanvas.Visibility = Visibility.Visible;
        }

        private async void UpdaterAction(string code)
        {
            var split = code.Split(':');

            if (split[0] == "Delete")
            {
                if (split[1] == "Profile")
                {
                    var directory = System.IO.Path.Combine(Settings.ProfileDirectory, split[2]);

                    if (!System.IO.Path.Exists(directory)) return;

                    DirectoryInfo dir = new(directory);

                    SetAttributesNormal(dir);

                    dir.Delete(true);

                    await InitProfiles();

                    ProfilesButton_Click(null, null);
                }
            }
        }

        private void SetAttributesNormal(DirectoryInfo dir)
        {
            foreach (var subDir in dir.GetDirectories())
            {
                SetAttributesNormal(subDir);

                subDir.Attributes = FileAttributes.Normal;
            }
            foreach (var file in dir.GetFiles())
            {
                file.Attributes = FileAttributes.Normal;
            }
            dir.Attributes = FileAttributes.Normal;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            UpdaterCanvasMode();

            UpdaterAction(((Button)sender).Tag.ToString());
        }
    }
}