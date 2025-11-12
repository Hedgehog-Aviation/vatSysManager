namespace vatSysManager
{
    public class PluginResponse
    {
        public string Name { get; set; }
        public string DllName { get; set; }
        public string DownloadUrl => $"https://github.com/{Name}/releases/download/latest/Plugin.zip";
        public string DirectoryName => Name.Split('/').Last();
    }
}
