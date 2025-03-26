namespace Saltworks.SaltMiner.ConsoleApp.Core
{
    public interface IConsoleAppHostBuilderOptions
    {
        public string SettingsFile { get; set; }
        public string AppSettingsSection { get; set; }
        public string LogSettingsSection { get; set; }
        public string ConfigFilePath { get; set; }
        public string ConfigFilePathEnvVariable { get; set; }
        public string ConfigFilePathLocatorFile { get; set; }
        public string ResolvedConfigFilePath { get; }
    }
}
