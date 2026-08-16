namespace Stonehold
{
    public enum StatusEffectType
    {
        None,
        Slow,
        Burn,
        Shock,
        Stun,
        Poison
    }

    public enum ElementalReactionType
    {
        None,
        ThermalShock,   // Fire (Burn) + Frost (Slow/Freeze)
        Overload,       // Electric (Shock) + Fire (Burn)
        Shatter,        // Frost (Slow/Freeze) + Physical/Explosive
        CorrosiveBlast, // Poison + Fire (Burn)
        Neurotoxin,     // Poison + Electric (Shock)
        BrittleBlight   // Poison + Frost (Slow)
    }
}


