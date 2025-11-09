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
                            // Unescape the JSON string (convert \\ back to \)
                            config.LastDataPath = content.Substring(start, end - start).Replace("\\\\", "\\");
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
                // Escape backslashes for JSON format
                string escapedPath = LastDataPath.Replace("\\", "\\\\");
                string json = string.Format("{{\n  \"LastDataPath\": \"{0}\"\n}}", escapedPath);
                File.WriteAllText(ConfigFileName, json);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}
