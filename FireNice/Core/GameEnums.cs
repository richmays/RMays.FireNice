namespace FireNice.Core;

public enum GameStatus
{
    Playing,
    Won,
    Lost,
}

/// <summary>Things a key can be bound to.</summary>
public enum GameAction
{
    MoveLeft,
    MoveRight,
    MoveUp,
    MoveDown,
    Action,
    Reset,
    Rewind,
    NextLevel,
}

/// <summary>The player inputs that repeat while a key is held.</summary>
public interface IPlayerControls
{
    void Press(Direction direction);

    void UseAction();
}
