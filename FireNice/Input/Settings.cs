using System.Text.Json;
using System.Text.Json.Serialization;
using FireNice.Core;

namespace FireNice.Input;

/// <summary>
/// Key bindings and timings, stored in keybindings.json next to the game.
/// </summary>
public sealed class Settings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public Dictionary<GameAction, List<Keys>> Bindings { get; set; } = DefaultBindings();

    /// <summary>How often a held key repeats (walking and the action button).</summary>
    public int RepeatIntervalMs { get; set; } = 333;

    /// <summary>Length of one game tick; sets how fast things fall and slide.</summary>
    public int TickMs { get; set; } = 100;

    /// <summary>How long a step or climb takes to animate. 0 turns walking animation off.</summary>
    public int WalkAnimationMs { get; set; } = 150;

    /// <summary>How long Solve searches before giving up.</summary>
    public int SolverTimeLimitMs { get; set; } = 60_000;

    public static Dictionary<GameAction, List<Keys>> DefaultBindings() => new()
    {
        [GameAction.MoveLeft] = new() { Keys.Left, Keys.A },
        [GameAction.MoveRight] = new() { Keys.Right, Keys.D },
        [GameAction.MoveUp] = new() { Keys.Up, Keys.W },
        [GameAction.MoveDown] = new() { Keys.Down, Keys.S },
        [GameAction.Action] = new() { Keys.Space },
        [GameAction.Reset] = new() { Keys.R },
        [GameAction.NextLevel] = new() { Keys.Enter },
    };

    /// <summary>
    /// Loads the settings file, or writes one with the defaults if it doesn't exist yet.
    /// A broken file falls back to the defaults without overwriting it.
    /// </summary>
    public static Settings LoadOrCreate(string path)
    {
        Settings settings;
        if (File.Exists(path))
        {
            try
            {
                settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path), JsonOptions) ?? new Settings();
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                settings = new Settings();
            }
        }
        else
        {
            settings = new Settings();
            try
            {
                File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOptions));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Not being able to save the defaults isn't a problem; they're still used.
            }
        }

        settings.FillInMissing();
        return settings;
    }

    public Dictionary<Keys, GameAction> BuildKeyMap()
    {
        var map = new Dictionary<Keys, GameAction>();
        foreach (var (action, keys) in Bindings)
        {
            foreach (Keys key in keys)
            {
                map[key] = action;
            }
        }

        return map;
    }

    private void FillInMissing()
    {
        Bindings ??= new();
        foreach (var (action, keys) in DefaultBindings())
        {
            if (!Bindings.TryGetValue(action, out var bound) || bound is null || bound.Count == 0)
            {
                Bindings[action] = keys;
            }
        }

        RepeatIntervalMs = Math.Clamp(RepeatIntervalMs, 30, 5000);
        TickMs = Math.Clamp(TickMs, 10, 5000);
        WalkAnimationMs = Math.Clamp(WalkAnimationMs, 0, 2000);
        SolverTimeLimitMs = Math.Clamp(SolverTimeLimitMs, 500, 600_000);
    }
}
