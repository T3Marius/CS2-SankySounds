using CounterStrikeSharp.API.Core;
using static SankySounds.Config_Config;
using CounterStrikeSharp.API.Modules.Commands;
using Menu;
using Menu.Enums;
using CounterStrikeSharp.API;
using static SankySounds.SankySounds;
using CounterStrikeSharp.API.Modules.Admin;

namespace SankySounds;

public static class Menu
{
    private static Dictionary<int?, DateTime> lastCommandUsage = new Dictionary<int?, DateTime>();

    public static void Load()
    {
        Config_Config.Load();


        var commands = new Dictionary<IEnumerable<string>, (string description, CommandInfo.CommandCallback handler)>
            {
               { Config.Settings.SankyMenu, ("Opens Menu", Command_SankySound) }
            };

        foreach (var commandPair in commands)
        {
            foreach (var command in commandPair.Key)
            {
                Instance.AddCommand($"css_{command}", commandPair.Value.description, commandPair.Value.handler);
            }
        }
    }

    public static void Command_SankySound(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        if (Config.Permissions.Count > 0 && !Config.Permissions.Any(permission => AdminManager.PlayerHasPermissions(player, permission)))
        {
            return;
        }
        if (!Config.Settings.EnableMenu)
        {
            return;
        }

        var kitsuneMenu = new KitsuneMenu(Instance);
        List<MenuItem> soundMenuItems = new List<MenuItem>();

        foreach (var soundEntry in Config.Sounds)
        {
            soundMenuItems.Add(new MenuItem(MenuItemType.Button, new List<MenuValue> { new MenuValue(soundEntry.Key) }));
        }

        var handleSoundSelection = (CCSPlayerController? p, string selectedSound) =>
        {
            if (p == null || !Config.Sounds.TryGetValue(selectedSound, out var sound))
                return;

            DateTime now = DateTime.Now;
            if (Config.Settings.CommandsCooldown > 0 && lastCommandUsage.TryGetValue(player.UserId, out DateTime lastUsage))
            {
                TimeSpan cooldownTime = now - lastUsage;
                if (cooldownTime.TotalSeconds < Config.Settings.CommandsCooldown)
                {
                    int remainingSeconds = (int)Math.Ceiling(Config.Settings.CommandsCooldown - cooldownTime.TotalSeconds);
                    player.PrintToChat(Instance.Localizer["prefix"] + Instance.Localizer["cooldown", remainingSeconds]);
                    return;
                }
            }
            lastCommandUsage[player.UserId] = now;

            Utilities.GetPlayers().ForEach(player =>
            {
                if (player != null && player.IsValid)
                {
                    player.ExecuteClientCommand($"play {sound}");
                }
            });
        };

        kitsuneMenu.ShowScrollableMenu(player, Instance.Localizer["menu<title>"], soundMenuItems, (menuButtons, menu, selectedItem) =>
        {
            if (menuButtons == MenuButtons.Exit)
                return;

            if (selectedItem != null && selectedItem.Values != null && selectedItem.Values.Count > 0)
            {
                handleSoundSelection(player, selectedItem.Values[0].Value);
            }
        }, false, true);
    }
}
