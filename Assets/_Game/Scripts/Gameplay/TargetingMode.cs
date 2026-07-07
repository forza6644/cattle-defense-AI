namespace Stonehold
{
    /// <summary>
    /// Supported enemy targeting algorithms for defense towers.
    /// </summary>
    public enum TargetingMode
    {
        ClosestToGoal,
        FirstInRange,
        LastInRange,
        Strongest,
        Weakest
    }
}
