﻿using CounterStrikeSharp.API;
using System.Reflection;
using Tomlyn.Model;
using Tomlyn;

namespace SankySounds
{
    public static class Config_Config
    {
        public static Cfg Config { get; set; } = new Cfg();

        public static void Load()
        {
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "";
            string cfgPath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/{assemblyName}";

            LoadConfig($"{cfgPath}/config.toml");
        }

        private static void LoadConfig(string configPath)
        {
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"Configuration file not found: {configPath}");
            }

            string configText = File.ReadAllText(configPath);
            TomlTable model = Toml.ToModel(configText);

            TomlTable soundsTable = (TomlTable)model["Sounds"];
            Dictionary<string, string> soundsDict = soundsTable.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToString()!
            );

            TomlTable permissionsTable = (TomlTable)model["Permissions"];
            List<string> permissionsList = new();
            foreach (var permission in (TomlArray)permissionsTable["Permissions"])
            {
                permissionsList.Add(permission!.ToString()!);
            }

            TomlTable settingsTable = (TomlTable)model["Settings"];
            Config_Settings config_settings = new()
            {
                CommandsCooldown = int.Parse(settingsTable["CommandsCooldown"]?.ToString() ?? "0"),
                EnableMessageForCooldown = bool.Parse(settingsTable["EnableMessageForCooldown"].ToString()!),
                SoundsPrefix = settingsTable["SoundsPrefix"]?.ToString() ?? ".",
                EnableMenu = bool.Parse(settingsTable["EnableMenu"].ToString()!),
                SankyMenu = GetTomlArray(settingsTable, "SankyMenu"),
                SoundCommand = GetTomlArray(settingsTable, "SoundCommand")
            };
            TomlTable databaseTable = (TomlTable)model["Database"];
            Database_Configuration config_database = new()
            {
                DBHost = databaseTable["DBHost"].ToString()!,
                DBName = databaseTable["DBName"].ToString()!,
                DBUser = databaseTable["DBUser"].ToString()!,
                DBPassword = databaseTable["DBPassword"].ToString()!,
                DBPort = uint.Parse(databaseTable["DBPort"].ToString()!)
            };

            Config = new Cfg
            {
                Sounds = soundsDict,
                Permissions = permissionsList,
                Settings = config_settings,
                Database = config_database
            };
        }
        private static string[] GetTomlArray(TomlTable table, string key)
        {
            if (table.TryGetValue(key, out var value) && value is TomlArray array)
            {
                return array.OfType<string>().ToArray();
            }
            return Array.Empty<string>();
        }

        public class Cfg
        {
            public Dictionary<string, string> Sounds { get; set; } = new();
            public List<string> Permissions { get; set; } = new();
            public Config_Settings Settings { get; set; } = new();
            public Database_Configuration Database { get; set; } = new();
        }

        public class Config_Settings
        {
            public int CommandsCooldown { get; set; }
            public bool EnableMessageForCooldown { get; set; }
            public string SoundsPrefix { get; set; } = ".";
            public bool EnableMenu { get; set; } = false;
            public string[] SankyMenu { get; set; } = Array.Empty<string>();
            public string[] SoundCommand { get; set; } = Array.Empty <string>();
        }
        public class Database_Configuration
        {
            public string DBHost { get; set; } = string.Empty;
            public string DBName { get; set; } = string.Empty;
            public string DBUser { get; set; } = string.Empty;
            public string DBPassword { get; set; } = string.Empty;
            public uint DBPort { get; set; } = 3306;
        }
    }
}
