namespace Stonehold
{
    /// <summary>
    /// High-level states the game can be in. The GameManager owns the current one.
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Victory,
        Defeat,
        LevelUp
    }
}
