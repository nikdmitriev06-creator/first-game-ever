using System;

/// <summary>
/// A usable item that is not a tool. EffectId names a future effect. Nothing here is hunger or spoilage.
/// </summary>
[Serializable]
public class ConsumableProfile
{
    public string EffectId;
    public float Magnitude;
}
