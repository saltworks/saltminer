namespace Saltworks.SaltMiner.ConsoleApp.Core
{
    public class ConsoleAppHostBuilderOptions : IConsoleAppHostBuilderOptions
    {
        public ConsoleAppHostBuilderOptions() {
            ConfigFilePath = "";
            ConfigFilePathEnvVariable = "SALTMINER_CONFIG_PATH";
            ConfigFilePathLocatorFile = "settings-locator.json";
        }

        public string SettingsFile { get; set; }
        public string AppSettingsSection { get; set; }
        public string LogSettingsSection { get; set; }

        //Defaulted to ''
        public string ConfigFilePath{ get; set; }
        //Defaulted to 'SALTMINER_CONFIG_PATH'
        public string ConfigFilePathEnvVariable{ get; set; }
        //Defaulted to 'settings-locator.json'
        public string ConfigFilePathLocatorFile{ get; set; }
        
        public string ResolvedConfigFilePath
        {
            get
            {
                Validate();
                //1. Default filepath for Config is ConfigFilePath. This is defaulted to ""
                var filePath = ConfigFilePath;

                //2. Filepath can be overwritten and pulled from a json file listed here in ConfigFilePathLocatorFile.
                //todo: Travis/Eddie Define File and Elements 
                //check for file.exists(ConfigFilePathLocatorFile)
                //filePath = ConfigFilePathLocatorFile;

                //3. Filepath can again be overwritten and pulled from a environment variable stored in ConfigFilePathEnvVariable.
                if (Environment.GetEnvironmentVariable(ConfigFilePathEnvVariable) != null)
                {
                    filePath = Environment.GetEnvironmentVariable(ConfigFilePathEnvVariable);
                }

                return filePath;
            }
        }

        private void Validate()
        {
            if(string.IsNullOrEmpty(LogSettingsSection) || string.IsNullOrEmpty(AppSettingsSection) || string.IsNullOrEmpty(SettingsFile))
            {
                throw new ConsoleAppHostBuilderException("Travis made me do it!");
            }
        }
    }
}