using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core.Translations;
using Nexd.MySQL;

namespace SankySounds
{
    public class PluginConfig : BasePluginConfig
    {
        [JsonPropertyName("Tag")] public string Tag { get; set; } = "[{blue}SankySounds{default}]";
        public Dictionary<string, string> Sounds { get; set; } = new Dictionary<string, string>();
        public string Permission { get; set; } = "";
        public int CommandsCooldown { get; set; } = 0;
        public bool ShowCommandInChat { get; set; } = true;

        // Database configuration class
        public class Database_Configuration
        {
            public string DatabaseHost { get; set; } = "host";
            public string DatabaseUser { get; set; } = "root";
            public string DatabasePassword { get; set; } = "password";
            public string DatabaseName { get; set; } = "name";
            public int DatabasePort { get; set; } = 3306;
        }

        public Database_Configuration DatabaseConfig { get; set; } = new Database_Configuration();
    }

    public partial class SankySounds : BasePlugin, IPluginConfig<PluginConfig>
    {
        public override string ModuleAuthor => "T3Marius";
        public override string ModuleName => "SankySounds";
        public override string ModuleVersion => "0.0.6";
        public override string ModuleDescription => "Plugin for using custom sounds with words in chat.";

        public PluginConfig Config { get; set; } = new PluginConfig();
        public Dictionary<int?, DateTime> lastCommandUsage = new Dictionary<int?, DateTime>();
        public Dictionary<int?, bool> PlayerSoundSettings { get; set; } = new Dictionary<int?, bool>();

        MySqlDb? MySql = null;

        public void OnConfigParsed(PluginConfig config)
        {
            config.Tag = StringExtensions.ReplaceColorTags(config.Tag);
            Config = config;
        }

        public override void Load(bool hotReload)
        {
            AddCommandListener("say", OnCommand_Say, HookMode.Pre);
            AddCommandListener("say_team", OnCommand_Say, HookMode.Pre);

            var dbConfig = Config.DatabaseConfig;
            MySql = new MySqlDb(dbConfig.DatabaseHost, dbConfig.DatabaseUser, dbConfig.DatabasePassword, dbConfig.DatabaseName, dbConfig.DatabasePort);

           
            MySql.ExecuteNonQueryAsync(@"CREATE TABLE IF NOT EXISTS `Sanky_Sounds` (
                `user_id` INT NOT NULL PRIMARY KEY, 
                `last_command_usage` DATETIME, 
                `sound_enabled` BOOLEAN DEFAULT TRUE
            );").Wait(); 

            if (hotReload)
            {
                
            }
        }

        public override void Unload(bool hotReload)
        {
            RemoveCommandListener("say", OnCommand_Say, HookMode.Pre);
            RemoveCommandListener("say_team", OnCommand_Say, HookMode.Pre);
        }

        public HookResult OnCommand_Say(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid || player.IsBot)
                return HookResult.Continue;

            
            if (info.ArgByIndex(1)?.Equals("!sounds", StringComparison.OrdinalIgnoreCase) == true)
            {
                TogglePlayerSoundSetting(player);
                return HookResult.Handled;
            }

           
            string commandArgument = info.ArgByIndex(1);
            if (commandArgument != null && commandArgument.StartsWith("."))
            {
                string soundKey = commandArgument.Substring(1);

                
                if (!string.IsNullOrEmpty(Config.Permission) && !AdminManager.PlayerHasPermissions(player, Config.Permission))
                {
                    player.PrintToChat($"{Config.Tag} {Localizer["no_permission"]}");
                    return HookResult.Continue;
                }

                
                if (Config.Sounds.TryGetValue(soundKey, out string soundValue))
                {
                    DateTime now = DateTime.Now;

                    if (Config.CommandsCooldown > 0 && lastCommandUsage.TryGetValue(player.UserId, out DateTime lastUsage))
                    {
                        TimeSpan cooldownTime = now - lastUsage;
                        if (cooldownTime.TotalSeconds < Config.CommandsCooldown)
                        {
                            int remainingSeconds = (int)Math.Ceiling(Config.CommandsCooldown - cooldownTime.TotalSeconds);
                            string message = $"{Config.Tag} {Localizer["cooldown"]}";
                            message = message.Replace("{secondsRemaining}", remainingSeconds.ToString());
                            player.PrintToChat(message);
                            return HookResult.Continue;
                        }
                    }

                    if (PlayerSoundSettings.GetValueOrDefault(player.UserId, true) == false)
                    {
                        return HookResult.Continue;
                    }

                    Utilities.GetPlayers().ForEach(p =>
                    {
                        if (p != null && p.IsValid && PlayerSoundSettings.GetValueOrDefault(p.UserId, true))
                        {
                            p.ExecuteClientCommand($"play {soundValue}");
                        }
                    });

                    lastCommandUsage[player.UserId] = now;
                    SavePlayerSettings(player.UserId, now, PlayerSoundSettings[player.UserId]);
                    return Config.ShowCommandInChat ? HookResult.Continue : HookResult.Handled;
                }
            }

            return HookResult.Continue;
        }

        private void TogglePlayerSoundSetting(CCSPlayerController player)
        {
            bool newSetting = !PlayerSoundSettings.GetValueOrDefault(player.UserId, true);
            PlayerSoundSettings[player.UserId] = newSetting;
            SavePlayerSettings(player.UserId, lastCommandUsage.GetValueOrDefault(player.UserId, DateTime.MinValue), newSetting);

            string statusMessage = newSetting ? Localizer["command.sound.enabled"] : Localizer["command.sound.disabled"];
            player.PrintToChat($"{Config.Tag} {statusMessage}");
        }

        private void SavePlayerSettings(int? userId, DateTime lastCommandUsage, bool soundEnabled)
        {
            string query = $@"
                INSERT INTO `Sanky_Sounds` (`user_id`, `last_command_usage`, `sound_enabled`)
                VALUES ({userId?.ToString() ?? "NULL"}, {(lastCommandUsage != DateTime.MinValue ? $"'{lastCommandUsage:yyyy-MM-dd HH:mm:ss}'" : "NULL")}, {(soundEnabled ? "TRUE" : "FALSE")})
                ON DUPLICATE KEY UPDATE `last_command_usage` = VALUES(`last_command_usage`), `sound_enabled` = VALUES(`sound_enabled`)";

            MySql!.ExecuteNonQueryAsync(query).Wait(); 
        }

        private void LoadPlayerSettings(int? userId)
        {
            string query = $"SELECT `last_command_usage`, `sound_enabled` FROM `Sanky_Sounds` WHERE `user_id` = {userId?.ToString() ?? "NULL"}";
            var result = MySql!.ExecuteQuery(query);

            if (result.Rows > 0)
            {
                lastCommandUsage[userId] = DateTime.Parse(result.Get<string>(0, "last_command_usage"));
                PlayerSoundSettings[userId] = result.Get<bool>(0, "sound_enabled");
            }
            else
            {
                lastCommandUsage[userId] = DateTime.MinValue;
                PlayerSoundSettings[userId] = true;
            }
        }
    }
}
