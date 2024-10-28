using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using MySqlConnector;
using static SankySounds.Config_Config;

namespace SankySounds;

public class SankySounds : BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "SankySounds";
    public override string ModuleVersion => "1.2";
    public static SankySounds Instance { get; set; } = new SankySounds();
    public static Dictionary<int, DateTime> LastCommandUsage { get; set; } = new Dictionary<int, DateTime>();
    public static Dictionary<int, bool> PlayerSoundStatus { get; set; } = new Dictionary<int, bool>();

    private MySqlConnection? _connection;

    public override void Load(bool hotReload)
    {
        Instance = this;
        Config_Config.Load();
        InitializeDatabaseConnection();

        Menu.Load();
        AddCommandListener("say", Command_Say, HookMode.Pre);
        AddCommandListener("say_team", Command_Say, HookMode.Pre);

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

        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    }

    private void InitializeDatabaseConnection()
    {
        string connString = $"Server={Config.Database.DBHost};Database={Config.Database.DBName};" +
                            $"User={Config.Database.DBUser};Password={Config.Database.DBPassword};" +
                            $"Port={Config.Database.DBPort};";

        _connection = new MySqlConnection(connString);
        _connection.Open();

        using var command = new MySqlCommand(
            @"CREATE TABLE IF NOT EXISTS player_sounds (
                user_id INT PRIMARY KEY,
                sound_enabled BOOLEAN NOT NULL DEFAULT TRUE
            );", _connection);
        command.ExecuteNonQuery();
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

            SavePlayerSoundSetting(player.UserId.Value, PlayerSoundStatus[player.UserId.Value]);
        }
    }

    private void SavePlayerSoundSetting(int userId, bool soundEnabled)
    {
        if (_connection == null) return;

        using var command = new MySqlCommand(
            @"INSERT INTO player_sounds (user_id, sound_enabled) VALUES (@userId, @soundEnabled)
              ON DUPLICATE KEY UPDATE sound_enabled = @soundEnabled;", _connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@soundEnabled", soundEnabled);
        command.ExecuteNonQuery();
    }

    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player?.UserId == null || _connection == null)
            return HookResult.Continue;

        int userId = player.UserId.Value;
        using var command = new MySqlCommand("SELECT sound_enabled FROM player_sounds WHERE user_id = @userId;", _connection);
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            bool soundEnabled = reader.GetBoolean("sound_enabled");
            PlayerSoundStatus[userId] = soundEnabled;
        }
        else
        {
            PlayerSoundStatus[userId] = true;
        }

        return HookResult.Continue;
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player?.UserId != null && PlayerSoundStatus.ContainsKey(player.UserId.Value))
        {
            SavePlayerSoundSetting(player.UserId.Value, PlayerSoundStatus[player.UserId.Value]);
            PlayerSoundStatus.Remove(player.UserId.Value);
        }

        return HookResult.Continue;
    }
}
