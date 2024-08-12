using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core.Translations;
using Nexd.MySQL;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Menu;
using System.Text;

namespace SankySounds
{
    public class PluginConfig : BasePluginConfig
    {
        [JsonPropertyName("Tag")]
        public string Tag { get; set; } = "[{blue}SankySounds{default}]";

        public class Sounds_Configuration
        {
            [JsonPropertyName("Sounds")]
            public Dictionary<string, string> Sounds { get; set; } = new Dictionary<string, string>();
        }

        [JsonPropertyName("Permissions")]
        public List<string> Permissions { get; set; } = new List<string>();

        [JsonPropertyName("CommandsCooldown")]
        public int CommandsCooldown { get; set; } = 0;

        [JsonPropertyName("ShowCommandInChat")]
        public bool ShowCommandInChat { get; set; } = true;

        [JsonPropertyName("SoundsPrefix")]
        public string SoundsPrefix { get; set; } = ".";

        public class Database_Configuration
        {
            [JsonPropertyName("DatabaseHost")]
            public string DatabaseHost { get; set; } = "host";

            [JsonPropertyName("DatabaseUser")]
            public string DatabaseUser { get; set; } = "root";

            [JsonPropertyName("DatabasePassword")]
            public string DatabasePassword { get; set; } = "password";

            [JsonPropertyName("DatabaseName")]
            public string DatabaseName { get; set; } = "name";

            [JsonPropertyName("DatabasePort")]
            public int DatabasePort { get; set; } = 3306;
        }

        [JsonPropertyName("DatabaseConfig")]
        public Database_Configuration DatabaseConfig { get; set; } = new Database_Configuration();

        [JsonPropertyName("SoundConfig")]
        public Sounds_Configuration SoundConfig { get; set; } = new Sounds_Configuration();
    }

    public class SankySounds : BasePlugin, IPluginConfig<PluginConfig>
    {
        public override string ModuleAuthor => "T3Marius";
        public override string ModuleName => "SankySounds";
        public override string ModuleVersion => "0.1.0";
        public override string ModuleDescription => "Plugin for using custom sounds with words in chat.";

        public PluginConfig Config { get; set; } = new PluginConfig();
        private Dictionary<int?, DateTime> lastCommandUsage = new Dictionary<int?, DateTime>();
        private Dictionary<int?, bool> PlayerSoundSettings { get; set; } = new Dictionary<int?, bool>();
        private MySqlDb? MySql = null;

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

        [ConsoleCommand("css_sankysounds", "shows a menu with all the sounds that are in config.")]
        public void OnSankySoundsCommand(CCSPlayerController player, CommandInfo info)
        {
            // Check permissions
            if (Config.Permissions.Count > 0 && !Config.Permissions.Any(permission => AdminManager.PlayerHasPermissions(player, permission)))
            {
                return;
            }

            var menu = new SankyMenu(this);
            menu.DisplaySankySoundsMenu(player);
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

            string commandArgument = info.ArgByIndex(1);
            string prefix = Config.SoundsPrefix;

            if (commandArgument != null && commandArgument.StartsWith(prefix))
            {
                string soundKey = commandArgument.Substring(prefix.Length);

                if (Config.Permissions.Count > 0 && !Config.Permissions.Any(permission => AdminManager.PlayerHasPermissions(player, permission)))
                {
                    return HookResult.Continue;
                }

                if (Config.SoundConfig.Sounds.TryGetValue(soundKey, out string soundValue))
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

                    if (!PlayerSoundSettings.GetValueOrDefault(player.UserId, true))
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
                    SavePlayerSettings(player.UserId, now, PlayerSoundSettings.GetValueOrDefault(player.UserId, true));

                    // Optionally, suppress the chat output if you don't want the "say" command to show in chat
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

        public class SankyMenu
        {
            private readonly SankySounds _plugin;
            private Dictionary<int?, DateTime> lastOptionUsage = new Dictionary<int?, DateTime>();

            public SankyMenu(SankySounds plugin)
            {
                _plugin = plugin;
            }

            public void AddMenuOption(CCSPlayerController player, CenterHtmlMenu menu, Action<CCSPlayerController, ChatMenuOption> onSelect, bool disabled, string display, params object[] args)
            {
                using (new WithTemporaryCulture(player.GetLanguage()))
                {
                    StringBuilder builder = new();
                    builder.AppendFormat(display, args);

                    Action<CCSPlayerController, ChatMenuOption> onSelectWithCooldown = (p, option) =>
                    {
                        DateTime now = DateTime.Now;

                        if (lastOptionUsage.TryGetValue(p.UserId, out DateTime lastUsage))
                        {
                            TimeSpan cooldownTime = now - lastUsage;
                            if (cooldownTime.TotalSeconds < _plugin.Config.CommandsCooldown)
                            {
                                int remainingSeconds = (int)Math.Ceiling(_plugin.Config.CommandsCooldown - cooldownTime.TotalSeconds);
                                string message = $"{_plugin.Config.Tag} {_plugin.Localizer["cooldown"]}";
                                message = message.Replace("{secondsRemaining}", remainingSeconds.ToString());
                                p.PrintToChat(message);
                                return;
                            }
                        }

                        lastOptionUsage[p.UserId] = now;

                        // Execute the original action
                        onSelect(p, option);
                    };

                    menu.AddMenuOption(builder.ToString(), onSelectWithCooldown, disabled);
                }
            }

            public void DisplaySankySoundsMenu(CCSPlayerController player)
            {
                using (new WithTemporaryCulture(player.GetLanguage()))
                {
                    var config = _plugin.Config;
                    StringBuilder builder = new();
                    builder.AppendFormat(_plugin.Localizer["menu_sankysounds<title>"]);
                    CenterHtmlMenu menu = new(builder.ToString(), _plugin);
                    DateTime now = DateTime.Now;

                    foreach (var soundEntry in config.SoundConfig.Sounds)
                    {
                        string soundKey = soundEntry.Key;
                        string soundValue = soundEntry.Value;

                        AddMenuOption(player, menu, (player, option) =>
                        {
                            if (_plugin.PlayerSoundSettings.GetValueOrDefault(player.UserId, true) == false)
                            {
                                return;
                            }

                            Utilities.GetPlayers().ForEach(p =>
                            {
                                if (p != null && p.IsValid && _plugin.PlayerSoundSettings.GetValueOrDefault(p.UserId, true))
                                {
                                    p.ExecuteClientCommand($"play {soundValue}");
                                    p.ExecuteClientCommand($"say {config.SoundsPrefix}{soundKey}");
                                }
                            });

                            _plugin.lastCommandUsage[player.UserId] = now;

                        },
                        false, $"{config.SoundsPrefix}{soundKey}");
                    }

                    MenuManager.OpenCenterHtmlMenu(_plugin, player, menu);
                }
            }
        }
    }
}
