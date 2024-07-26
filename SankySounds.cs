using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core.Translations;

namespace SankySounds
{
    public class PluginConfig : BasePluginConfig
    {
        [JsonPropertyName("Tag")] public string Tag { get; set; } = "[{blue}SankySounds{default}]";
        public Dictionary<string, string> Sounds { get; set; } = new Dictionary<string, string>();
        public string Permission { get; set; } = "";
        public int CommandsCooldown { get; set; } = 0;
        public bool ShowCommandInChat { get; set; } = true;
    }

    public partial class SankySounds : BasePlugin, IPluginConfig<PluginConfig>
    {
        public override string ModuleAuthor => "T3Marius";
        public override string ModuleName => "SankySounds";
        public override string ModuleVersion => "0.0.4";

        public PluginConfig Config { get; set; } = new PluginConfig();

        public Dictionary<int?, DateTime> lastCommandUsage = new Dictionary<int?, DateTime>();
        public Dictionary<int?, bool> PlayerSoundSettings { get; set; } = new Dictionary<int?, bool>();


        public void OnConfigParsed(PluginConfig config)
        {
            config.Tag = StringExtensions.ReplaceColorTags(config.Tag);
            Config = config;
        }

        public override void Load(bool hotReload)
        {
            AddCommandListener("say", OnCommand_Say, HookMode.Pre);
            AddCommandListener("say_team", OnCommand_Say, HookMode.Pre);
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
                bool newSetting;

                if (PlayerSoundSettings.ContainsKey(player.UserId))
                {
                    newSetting = !PlayerSoundSettings[player.UserId];
                }
                else
                {
                    newSetting = false;
                }

                // Update player sound setting
                PlayerSoundSettings[player.UserId] = newSetting;

                // Inform the player of the change
                string statusMessage = newSetting
                    ? Localizer["command.sound.enabled"]
                    : Localizer["command.sound.disabled"];

                string message = $"{Config.Tag} {statusMessage}";
                player.PrintToChat(message);

                return HookResult.Handled;
            }

            if (Config.Permission != "" && !AdminManager.PlayerHasPermissions(player, $"{Config.Permission}"))
            {
                string message = $"{Config.Tag} {Localizer["no_permission"]}";
                player.PrintToChat(message);
                return HookResult.Continue;
            }

            DateTime now = DateTime.Now;

            string commandArgument = info.ArgByIndex(1);

            // Check if the command starts with a period
            if (commandArgument != null && commandArgument.StartsWith("."))
            {
                string soundKey = commandArgument.Substring(1);

                if (Config.Sounds.TryGetValue(soundKey, out string soundValue))
                {
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

                    // Check if the player has disabled sounds
                    if (PlayerSoundSettings.GetValueOrDefault(player.UserId, true) == false)
                    {
                        return HookResult.Continue;
                    }

                    Utilities.GetPlayers().ForEach(p =>
                    {
                        if (p is { IsValid: true } && PlayerSoundSettings.GetValueOrDefault(p.UserId, true))
                        {
                            p.ExecuteClientCommand($"play {soundValue}");
                        }
                    });

                    lastCommandUsage[player.UserId] = now;
                    return Config.ShowCommandInChat ? HookResult.Continue : HookResult.Handled;
                }
            }

            return HookResult.Continue;
        }
    }
}