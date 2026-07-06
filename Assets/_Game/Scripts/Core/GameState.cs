namespace Stonehold
{
    /// <summary>
    /// High-level states the game can be in. The GameManager owns the current one.
    /// (Type definition only — no logic.)
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        WaveComplete,
        Victory,
        Defeat
    }
}
