using UnityEngine;

/// <summary>
/// Puts a stack on the ground in the current location. Callers do not name scenes or rooms.
/// </summary>
public static class WorldDropper
{
    public static WorldItem Spawn(string id, int count, Vector2 from, Vector2 land, float pickupDelay)
    {
        if (string.IsNullOrEmpty(id) || count <= 0)
            return null;

        Transform parent = null;
        LocationContext place = LocationContext.Active;
        if (place != null)
        {
            parent = place.DropParent;
            if (place.Area != null)
                land = place.Area.Clamp(land, 1.05f);
        }

        return WorldItem.Toss(id, count, from, land, parent, pickupDelay);
    }
}
