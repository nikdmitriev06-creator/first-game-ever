/// <summary>
/// Unique attack behaviour that data alone cannot describe.
/// A sword and a scythe still share <see cref="WeaponProfile"/>. Only the scythe registers a strategy.
/// </summary>
public interface IAttackStrategy
{
    string Id { get; }
    void Adjust(ref AttackPlan plan, ItemInstance instance);
}
