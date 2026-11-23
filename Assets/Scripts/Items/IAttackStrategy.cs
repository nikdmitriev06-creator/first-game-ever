/// <summary>
/// Unique attack behaviour that data alone cannot describe.
/// A sword and a scythe still share <see cref="WeaponProfile"/>. Only the scythe registers a strategy.
/// </summary>
public interface IAttackStrategy
{
    string Id { get; }

    /// <summary>
    /// May change numbers on the plan, including <see cref="AttackPlan.Payload"/>. Called before modifiers.
    /// </summary>
    void Adjust(ref AttackPlan plan, ItemInstance instance);
}
