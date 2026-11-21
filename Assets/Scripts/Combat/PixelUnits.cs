/// <summary>
/// The character sheets and the slime sheets are imported at 32 pixels per world unit.
/// Gameplay distances the designer states in pixels go through here before they touch the world.
/// </summary>
public static class PixelUnits
{
    public const float PerUnit = 32f;

    public static float ToWorld(float pixels)
    {
        return pixels / PerUnit;
    }
}
