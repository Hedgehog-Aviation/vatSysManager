namespace vatSysManager
{
    public class PluginInstalled(string title, string profile, string localDirectory)
    {
        public string Title { get; set; } = title;
        public string Profile { get; set; } = profile;
        public string LocalDirectory { get; set; } = localDirectory;
        public string LocalVersion { get; set; }
        public string CurrentVersion { get; set; }
        public bool UpdateAvailable
        {
            get
            {
                if (string.IsNullOrWhiteSpace(LocalVersion)) return false;
                if (LocalVersion != CurrentVersion) return true;
                return false;
            }
        }
        public string UpdateCommand => $"Update|Plugin|{Title}|{LocalDirectory}";
        public string DeleteCommand => $"Delete|Plugin|{Title}|{LocalDirectory}";
    }
}
