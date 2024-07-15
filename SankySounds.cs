using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
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

            if (Config.Permission != "" && !AdminManager.PlayerHasPermissions(player, $"{Config.Permission}"))
                return HookResult.Continue;

            DateTime now = DateTime.Now;

            foreach (var sound in Config.Sounds)
            {
                if (info.ArgByIndex(1) == sound.Key)
                {
                    if (Config.CommandsCooldown > 0 && lastCommandUsage.TryGetValue(player.UserId, out DateTime lastUsage))
                    {
                        TimeSpan cooldownTime = now - lastUsage;
                        if (cooldownTime.TotalSeconds < Config.CommandsCooldown)
                        {
                            player.PrintToChat(Config.Tag + Localizer["Cooldown"]);
                            return HookResult.Continue;
                        }
                    }
                    Utilities.GetPlayers().ForEach(player =>
                    {
                        if (player is { IsValid: true })
                        {
                            player.ExecuteClientCommand($"play {sound.Value}");
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
