using UnityEngine;

public enum PlaceReject
{
    None,
    Missing,
    Zone,
    Outside,
    Occupied,
    Player
}

/// <summary>
/// Pure placement checks. The preview and the confirm path both use this.
/// </summary>
public static class PlacementRules
{
    public static PlaceReject Evaluate(ItemDefinition item, Vector2 point, Vector2 feet, float playerClearance)
    {
        if (item == null || !item.IsPlaceable)
            return PlaceReject.Missing;

        LocationContext place = LocationContext.Active;
        if (place == null || !item.Fits(place))
            return PlaceReject.Zone;

        float inset = place.Kind == LocationKind.Indoor
            ? Mathf.Max(item.Footprint, 1.15f)
            : item.Footprint;
        if (!place.Contains(point, inset))
            return PlaceReject.Outside;

        if (Vector2.Distance(point, feet) < playerClearance)
            return PlaceReject.Player;

        if (PlaceableObject.Overlaps(point, item.Footprint) || PlacementBlocker.Hits(point))
            return PlaceReject.Occupied;

        return PlaceReject.None;
    }

    public static Vector2 Snap(Vector2 world)
    {
        Vector2 iso = IsometricCoordinates.WorldToIsometric(world);
        iso.x = Mathf.Round(iso.x * 2f) * 0.5f;
        iso.y = Mathf.Round(iso.y * 2f) * 0.5f;
        return IsometricCoordinates.IsometricToWorld(iso);
    }
}
