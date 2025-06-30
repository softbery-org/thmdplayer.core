// Version: 1.0.0.676
using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows.Media;
using System.Xml;
using ThmdPlayer.Core.logs;
using System.Xml.Serialization;

namespace ThmdPlayer.Core.configuration
{
    /// <summary>
    /// Main configuration class handling application settings and persistence
    /// </summary>
    public class Config
    {
        private static readonly object _lock = new object();
        private static Config _instance;

        /// <summary>
        /// Database connection string for main application connection
        /// </summary>
        public string DatabaseConnectionString { get; set; }

        /// <summary>
        /// Maximum number of simultaneous connection connections
        /// </summary>
        public int MaxConnections { get; set; }

        /// <summary>
        /// Enables or disables application-wide logging
        /// </summary>
        public bool EnableLogging { get; set; }

        /// <summary>
        /// Directory path for storing log files
        /// </summary>
        public string LogsDirectoryPath { get; set; }

        /// <summary>
        /// API key for external service authentication
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Path to LibVLC native libraries directory
        /// </summary>
        public string LibVlcPath { get; set; }

        /// <summary>
        /// Enables or disables LibVLC multimedia functionality
        /// </summary>
        public bool EnableLibVlc { get; set; }

        /// <summary>
        /// Enables or disables console output logging
        /// </summary>
        public bool EnableConsoleLogging { get; set; }

        /// <summary>
        /// Minimum log level for filtering log messages
        /// </summary>
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// configuration settings for subtitle rendering
        /// </summary>
        public SubtitleConfig SubtitleConfig { get; set; }

        /// <summary>
        /// configuration settings for application updates
        /// </summary>
        public UpdateConfig UpdateConfig { get; set; }

        /// <summary>
        /// Array of installed plugin configurations
        /// </summary>
        public PluginConfig[] Plugins { get; set; } = new PluginConfig[0];

        /// <summary>
        /// Initializes configuration with default values
        /// </summary>
        public Config()
        {
            DatabaseConnectionString = "server=localhost;connection=default";
            MaxConnections = 10;
            EnableLogging = true;
            LogsDirectoryPath = "logs";
            ApiKey = "default-key";
            LibVlcPath = "libvlc";
            EnableLibVlc = true;
            LogLevel = LogLevel.Info;
            SubtitleConfig = new SubtitleConfig(24, "Arial", System.Windows.Media.Brushes.WhiteSmoke, true, new Shadow());
            UpdateConfig = new UpdateConfig
            {
                CheckForUpdates = true,
                UpdateUrl = "http://thmdplayer.softbery.org/update.rar",
                UpdatePath = "update",
                UpdateFileName = "update",
                Version = "3.0.0",
                VersionUrl = "http://thmdplayer.softbery.org/version.txt",
                UpdateInterval = 86400,
                UpdateTimeout = 30
            };
        }

        /// <summary>
        /// Singleton instance accessor for configuration
        /// </summary>
        public static Config Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= LoadFromFile("config.json");
                }
            }
        }

        /// <summary>
        /// Loads configuration from specified JSON file
        /// </summary>
        /// <param name="filePath">Path to configuration file</param>
        /// <returns>Loaded configuration instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when loading fails</exception>
        public static Config LoadFromFile(string filePath)
        {
            try
            {
                Console.WriteLine(filePath);
                if (!File.Exists(filePath))
                {
                    // Tworzy domyślną konfigurację jeśli plik nie istnieje
                    var defaultConfig = new Config();
                    defaultConfig.SaveToFile(filePath);
                    return defaultConfig;
                }

                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<Config>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Błąd ładowania konfiguracji: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves current configuration to specified file
        /// </summary>
        /// <param name="filePath">Target file path for saving</param>
        /// <exception cref="InvalidOperationException">Thrown when saving fails</exception>
        public void SaveToFile(string filePath)
        {
            try
            {
                var file_info = new FileInfo(filePath);
                var directory = file_info.Directory.FullName;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Błąd zapisu konfiguracji: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Applies modifications and persists configuration atomically
        /// </summary>
        /// <param name="updateAction">Modification action to execute</param>
        /// <param name="filePath">configuration file path (default: config.json)</param>
        public void UpdateAndSave(Action<Config> updateAction, string filePath = "config.json")
        {
            lock (_lock)
            {
                updateAction.Invoke(this);
                SaveToFile(filePath);
            }
        }
    }

    /// <summary>
    /// configuration for subtitle display properties
    /// </summary>
    public class SubtitleConfig
    {
        /// <summary>
        /// Font size in points for subtitles
        /// </summary>
        public double FontSize { get; set; }

        /// <summary>
        /// Font family name for subtitles
        /// </summary>
        public FontFamily FontFamily { get; set; }

        /// <summary>
        /// Font color for subtitles
        /// </summary>
        public Brush FontColor { get; set; }

        /// <summary>
        /// Enables text show_shadow effect for subtitles
        /// </summary>
        public Shadow Shadow { get; set; }

        /// <summary>
        /// Creates subtitle configuration with specified parameters
        /// </summary>
        /// <param name="size">Initial font size in points</param>
        /// <param name="fontfamily">Font family name</param>
        /// <param name="color">Font color brush</param>
        /// <param name="show_shadow">ShowShadow effect enabled state</param>
        public SubtitleConfig(double size, string fontfamily, System.Windows.Media.Brush color, bool show_shadow, Shadow shadow = null)
        {
            FontSize = size;
            FontFamily = new FontFamily(fontfamily);
            FontColor = color;
            Shadow = shadow ?? new Shadow
            {
                Color = Colors.Black,
                ShadowDepth = 0,
                Opacity = 0.5,
                BlurRadius = 10,
                Visible = show_shadow
            };
        }
    }

    public class Shadow
    {
        /// <summary>
        /// Color of the show_shadow effect
        /// </summary>
        public Color Color { get; set; }= Colors.Black;
        /// <summary>
        /// Horizontal offset of the show_shadow
        /// </summary>
        public double ShadowDepth { get; set; }= 0;
        /// <summary>
        /// Vertical offset of the show_shadow
        /// </summary>
        public double Opacity { get; set; }= 0.5;
        /// <summary>
        /// Blur radius of the show_shadow effect
        /// </summary>
        public double BlurRadius { get; set; } = 10;
        public bool Visible { get; set; } = true;
    }

    /// <summary>
    /// configuration for application update system
    /// </summary>
    public class UpdateConfig
    {
        /// <summary>
        /// Enables automatic update checks
        /// </summary>
        public bool CheckForUpdates { get; set; }

        /// <summary>
        /// URL for downloading update packages
        /// </summary>
        public string UpdateUrl { get; set; } = "http://thmdplayer.softbery.org/update.rar";

        /// <summary>
        /// Local directory for storing update files
        /// </summary>
        public string UpdatePath { get; set; } = "update";

        /// <summary>
        /// Filename for update package
        /// </summary>
        public string UpdateFileName { get; set; } = "update";

        /// <summary>
        /// Current application version string
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// URL for checking current version information
        /// </summary>
        public string VersionUrl { get; set; } = "http://thmdplayer.softbery.org/version.txt";

        /// <summary>
        /// Update check interval in seconds (default: 86400 = 24h)
        /// </summary>
        public int UpdateInterval { get; set; } = 86400;

        /// <summary>
        /// Timeout in seconds for update operations
        /// </summary>
        public int UpdateTimeout { get; set; } = 30;
    }

    /// <summary>
    /// configuration for individual plugin
    /// </summary>
    public class PluginConfig
    {
        /// <summary>
        /// Display name of the plugin
        /// </summary>
        public string PluginName { get; set; }

        /// <summary>
        /// Path to plugin assembly
        /// </summary>
        public string PluginPath { get; set; }

        /// <summary>
        /// Indicates if plugin is active
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Plugin version string
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Descriptive information about the plugin
        /// </summary>
        public string Description { get; set; }
    }
}
