using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using System.Text.Json.Serialization;

namespace SankySounds;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("Sounds")] public Dictionary<string, string> Sounds { get; set; } = [];
    [JsonPropertyName("Permission")] public string Permission { get; set; } = "@css/generic";

}

public partial class SankySounds : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "SankySounds";
    public override string ModuleVersion => "0.0.1";

    public PluginConfig Config { get; set; } = new PluginConfig();
    public void OnConfigParsed(PluginConfig config)
    {
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

    [RequiresPermissions("css/generic")]
    public HookResult OnCommand_Say(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        if (!AdminManager.PlayerHasPermissions(player, Config.Permission))
            return HookResult.Continue;

        foreach (var sound in Config.Sounds)
        {
            if (info.ArgByIndex(1) == sound.Key)
            {
                player.ExecuteClientCommand($"play {sound.Value}");
                return HookResult.Continue;
            }
        }

        return HookResult.Continue;
    }
}