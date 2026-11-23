using UnityEngine;

/// <summary>
/// The cabin that stands in the middle of the home meadow. Built once, with the room.
/// </summary>
public static class HomeHouse
{
    public static void Place(LevelWorld.Room room)
    {
        if (room == null || room.Root == null)
            return;
        if (room.Root.transform.Find("Home House") != null)
            return;

        Sprite body = HouseArt.CabinBody();
        Sprite roof = HouseArt.CabinRoof();
        float bodyHeight = body.rect.height / HouseArt.PixelsPerUnit;

        Vector2 center = room.Bounds.center;
        Vector2 footprint = center + new Vector2(0f, 2.5f);
        Vector2 door = footprint + new Vector2(0f, -0.25f);
        Vector2 back = footprint + new Vector2(0f, -1.45f);
        if (room.Area != null)
            back = room.Area.Clamp(back, 1.3f);

        var root = new GameObject("Home House");
        root.transform.SetParent(room.Root.transform, false);
        root.transform.position = new Vector3(footprint.x, footprint.y, 0f);

        Material material = null;
        var ground = room.Root.GetComponent<SpriteRenderer>();
        if (ground != null)
            material = ground.sharedMaterial;

        SpriteRenderer shadow = Create(root.transform, "Shadow", HouseArt.CabinShadow(), new Vector2(0f, 0.2f), IsoSort.FloorOrder + 1, material);
        shadow.color = new Color(1f, 1f, 1f, 0.85f);

        SpriteRenderer walls = Create(root.transform, "Walls", body, Vector2.zero, 4, material);
        BillboardSort wallSort = walls.gameObject.AddComponent<BillboardSort>();
        wallSort.LineY = footprint.y + 0.08f;

        SpriteRenderer roofRenderer = Create(root.transform, "Roof", roof, new Vector2(0f, bodyHeight - 0.9f), 5, material);
        BillboardSort roofSort = roofRenderer.gameObject.AddComponent<BillboardSort>();
        roofSort.LineY = footprint.y + bodyHeight * 0.72f;
        roofSort.OrderBias = 1;

        var doorObject = new GameObject("Door");
        doorObject.transform.SetParent(root.transform, false);
        doorObject.transform.position = new Vector3(door.x, door.y, 0f);
        HouseDoor doorway = doorObject.AddComponent<HouseDoor>();
        doorway.Configure(false, door, 1.8f, back);

        PlacementBlocker blocker = root.AddComponent<PlacementBlocker>();
        blocker.Configure(new Vector2(0f, 3.1f), new Vector2(4.5f, 3.05f));
    }

    static SpriteRenderer Create(Transform parent, string name, Sprite sprite, Vector2 local, int order, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(local.x, local.y, 0f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        if (material != null)
            renderer.sharedMaterial = material;
        return renderer;
    }
}
