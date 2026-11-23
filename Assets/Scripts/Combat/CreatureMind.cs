/// <summary>
/// Which order a creature should follow. Distances are already in world units.
/// Home leash wins over a nearby player, so a chase cannot run forever.
/// </summary>
public enum MindOrder
{
    Idle,
    Patrol,
    Detect,
    Chase,
    Attack,
    Return
}

public static class CreatureMind
{
    public static MindOrder Choose(float gap, float homeDistance, float notice, float lose, float attack, float leash, bool seen)
    {
        if (homeDistance > leash)
            return MindOrder.Return;
        if (gap <= attack)
            return MindOrder.Attack;
        if (seen && gap > lose)
            return MindOrder.Return;
        if (!seen && gap <= notice)
            return MindOrder.Detect;
        if (gap <= notice || (seen && gap <= lose))
            return MindOrder.Chase;
        if (homeDistance > 1.2f)
            return MindOrder.Return;
        return MindOrder.Idle;
    }
}
