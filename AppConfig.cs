using System;
using System.IO;

namespace DropTableEditor
{
    /// <summary>
    /// Application configuration manager
    /// </summary>
    public class AppConfig
    {
        private const string ConfigFileName = "DropTableEditor.config";

        public string LastDataPath { get; set; }

        public AppConfig()
        {
            LastDataPath = string.Empty;
        }

        /// <summary>
        /// Load configuration from file
        /// </summary>
        public static AppConfig Load()
        {
            var config = new AppConfig();
            
            try
            {
                if (File.Exists(ConfigFileName))
                {
                    string content = File.ReadAllText(ConfigFileName);
                    
                    // Simple JSON parsing (no external dependencies for .NET 3.5)
                    if (content.Contains("\"LastDataPath\""))
                    {
                        int start = content.IndexOf("\"LastDataPath\"") + 15;
                        start = content.IndexOf("\"", start) + 1;
                        int end = content.IndexOf("\"", start);
                        if (start > 0 && end > start)
                        {
                            config.LastDataPath = content.Substring(start, end - start);
                        }
                    }
                }
                else
                {
                    // Create default config on first launch
                    config.Save();
                }
            }
            catch
            {
                // Return default config on error
            }

            return config;
        }

        /// <summary>
        /// Save configuration to file
        /// </summary>
        public void Save()
        {
            try
            {
                // Simple JSON format
                string json = string.Format("{{\n  \"LastDataPath\": \"{0}\"\n}}", 
                    LastDataPath.Replace("\\", "\\\\"));
                File.WriteAllText(ConfigFileName, json);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}
