/// <summary>
/// Extra work when a body reaches zero integrity: loot, a stump, a shrink. The tool does not call these.
/// </summary>
public interface IBreakReaction
{
    void OnBroken(in Impact impact);
}
