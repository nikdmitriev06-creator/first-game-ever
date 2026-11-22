using System;

/// <summary>
/// A future chest or the player bag. Breaking a rock does not require this.
/// </summary>
public interface IItemStorage
{
    ItemContainer Contents { get; }
}
