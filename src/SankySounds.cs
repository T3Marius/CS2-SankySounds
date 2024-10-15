using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using static SankySounds.Config_Config;

namespace SankySounds;

public class SankySounds : BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "SankySounds";
    public override string ModuleVersion => "1.1";
    public static SankySounds Instance { get; set; } = new SankySounds();
    public static Dictionary<int, DateTime> LastCommandUsage { get; set; } = new Dictionary<int, DateTime>();
    public static Dictionary<int, bool> PlayerSoundStatus { get; set; } = new Dictionary<int, bool>();

    public override void Load(bool hotReload)
    {
        Instance = this;
        Menu.Load();
        AddCommandListener("say", Command_Say, HookMode.Pre);
        AddCommandListener("say_team", Command_Say, HookMode.Pre);
        Config_Config.Load();

        var commands = new Dictionary<IEnumerable<string>, (string description, CommandInfo.CommandCallback handler)>
        {
            { Config.Settings.SoundCommand, ("Toggle sounds", Command_Sounds) }
        };

        foreach (var commandPair in commands)
        {
            foreach (var command in commandPair.Key)
            {
                Instance.AddCommand($"css_{command}", commandPair.Value.description, commandPair.Value.handler);
            }
        }
    }

    public HookResult Command_Say(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        string commandArgument = info.ArgByIndex(1);
        string prefix = Config.Settings.SoundsPrefix;

        if (Config.Permissions.Count > 0 && !Config.Permissions.Any(permission => AdminManager.PlayerHasPermissions(player, permission)))
        {
            return HookResult.Continue;
        }

        if (commandArgument != null && commandArgument.StartsWith(prefix))
        {
            string soundKey = commandArgument.Substring(prefix.Length);

            if (Config.Sounds.TryGetValue(soundKey, out var sound))
            {
                DateTime now = DateTime.Now;

                if (LastCommandUsage.TryGetValue(player.UserId!.Value, out DateTime lastUsage) &&
                    (now - lastUsage).TotalSeconds < Config.Settings.CommandsCooldown)
                {
                    int remainingSeconds = (int)(Config.Settings.CommandsCooldown - (now - lastUsage).TotalSeconds);
                    player.PrintToChat(Localizer["prefix"] + Localizer["cooldown", remainingSeconds]);
                    return HookResult.Continue;
                }

                LastCommandUsage[player.UserId!.Value] = now;

                Utilities.GetPlayers().ForEach(p =>
                {
                    bool soundEnabled = PlayerSoundStatus.TryGetValue(p.UserId!.Value, out bool isSoundOn) && isSoundOn;
                    if (soundEnabled)
                    {
                        p.ExecuteClientCommand($"play {sound}");
                    }
                });
            }
        }
        return HookResult.Continue;
    }
    public void Command_Sounds(CCSPlayerController? player, CommandInfo info)
    {
        if (player != null)
        {
            if (PlayerSoundStatus.TryGetValue(player.UserId!.Value, out bool isSoundOn))
            {
                PlayerSoundStatus[player.UserId.Value] = !isSoundOn;
            }
            else
            {
                PlayerSoundStatus[player.UserId.Value] = true;
            }

            string statusMessage = PlayerSoundStatus[player.UserId.Value]
                ? Localizer["sounds unmuted"]
                : Localizer["sounds muted"];
            player.PrintToChat(Localizer["prefix"] + statusMessage);
        }
    }
}