using UnityEngine;

/// <summary>
/// Indoor furniture survives leaving the house, because that scene unloads.
/// The lists live on the session LocationState, which GameRoot owns while playing.
/// </summary>
public static class PlacedFurniture
{
    public struct Record
    {
        public string Id;
        public string ItemId;
        public Vector2 Position;
        public int Health;
    }

    public static int IndoorCount => State.IndoorCount;

    static LocationState State
    {
        get
        {
            if (LocationState.Active == null)
                LocationState.Reset();

            return LocationState.Active;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        LocationState.Reset();
    }

    public static string Remember(string itemId, Vector2 position, int health)
    {
        return State.Remember(itemId, position, health);
    }

    public static void Touch(string id, Vector2 position, int health)
    {
        State.Touch(id, position, health);
    }

    public static void Forget(string id)
    {
        State.Forget(id);
    }

    public static void MarkLive(string id)
    {
        State.MarkLive(id);
    }

    public static void MarkGone(string id)
    {
        State.MarkGone(id);
    }

    public static void Restore(Transform parent)
    {
        State.Restore(parent);
    }
}
