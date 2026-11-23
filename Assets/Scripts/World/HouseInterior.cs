using UnityEngine;

/// <summary>
/// Builds the log room when the HouseInterior scene loads.
/// The scene itself only holds this bootstrap, so the planks and logs are not placed by hand.
/// </summary>
public class HouseInterior : MonoBehaviour
{
    const int BoardHeight = 18;
    const int FloorWidth = 704;
    const int FloorHeight = 432;
    const int LogHeight = 14;

    public static HouseInterior Active { get; private set; }

    public PlayArea WalkArea { get; private set; }

    public Transform DropAnchor { get; private set; }

    void Awake()
    {
        Active = this;
        Build(LocationFlow.Pending ? LocationFlow.InteriorOrigin : Vector2.zero);
    }

    void OnDestroy()
    {
        if (Active == this)
            Active = null;
    }

    public void EvacuateDrops(Transform home, Vector2 outside)
    {
        if (DropAnchor == null)
            return;

        WorldItem[] items = DropAnchor.GetComponentsInChildren<WorldItem>();
        PlayArea area = home != null ? home.GetComponent<PlayArea>() : null;
        for (int i = 0; i < items.Length; i++)
        {
            float side = i % 2 == 0 ? 1.7f : -1.7f;
            Vector2 land = outside + new Vector2(side, 0.35f * (i / 2));
            if (area != null)
                land = area.Clamp(land, 1.2f);
            items[i].Relocate(land, home);
        }
    }

    void Build(Vector2 origin)
    {
        float floorW = FloorWidth / HouseArt.PixelsPerUnit;
        float floorH = FloorHeight / HouseArt.PixelsPerUnit;
        Vector2 floorMin = origin - new Vector2(floorW * 0.5f, floorH * 0.5f);

        Material material = SharedMaterial();
        Transform root = transform;

        Transform bounds = CreateBounds(root, origin, floorW, floorH, material);
        WalkArea = bounds.GetComponent<PlayArea>();
        DropAnchor = CreateDropAnchor(root, material);
        LocationContext boundsContext = bounds.GetComponent<LocationContext>();
        if (boundsContext != null)
            boundsContext.SetDropParent(DropAnchor);

        LayFloor(root, floorMin, material);
        LayBackWall(root, origin, floorW, floorH, material);
        LaySideWall(root, origin, floorW, floorH, -1f, material);
        LaySideWall(root, origin, floorW, floorH, 1f, material);
        LayFrontWall(root, origin, floorW, floorH, material);

        Vector2 rugAt = origin + new Vector2(0f, 0.4f);
        CreateSprite(root, "Rug", HouseArt.Rug(), rugAt, IsoSort.FloorOrder + 1, material, false, 0f, 0);

        Vector2 door = floorMin + new Vector2(floorW * 0.5f, 3.15f);
        var doorObject = new GameObject("Interior Door");
        doorObject.transform.SetParent(root, false);
        doorObject.transform.position = new Vector3(door.x, door.y, 0f);
        HouseDoor doorway = doorObject.AddComponent<HouseDoor>();
        doorway.Configure(true, door, 2.2f, door);

        if (GameRoot.Active != null && GameRoot.Active.Player != null || FindAnyObjectByType<PlayerController>() != null)
            LocationFlow.PlaceInside(door, WalkArea);

        if (WalkArea != null)
        {
            LocationContext.Use(WalkArea.GetComponent<LocationContext>());
            PlacedFurniture.Restore(WalkArea.transform);
        }
    }

    Transform CreateBounds(Transform root, Vector2 origin, float width, float height, Material material)
    {
        var go = new GameObject("Interior Bounds");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(origin.x, origin.y, 0f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = HouseArt.RoomBounds(width, height);
        renderer.color = new Color(1f, 1f, 1f, 0f);
        renderer.sortingOrder = IsoSort.FloorOrder;
        if (material != null)
            renderer.sharedMaterial = material;

        var area = go.AddComponent<PlayArea>();
        area.CacheBounds();
        LocationContext context = go.AddComponent<LocationContext>();
        context.Configure(RegionKind.Home, LocationKind.Indoor, area);
        return go.transform;
    }

    void LayFloor(Transform root, Vector2 floorMin, Material material)
    {
        int y = 0;
        int row = 0;
        while (y + BoardHeight <= FloorHeight)
        {
            int x = 0;
            int col = 0;
            while (x < FloorWidth)
            {
                int remain = FloorWidth - x;
                int width = BoardWidth(row, col, remain);
                Sprite sprite = HouseArt.FloorBoard(width, (row * 5 + col * 3) % 6);
                float worldX = floorMin.x + (x + width * 0.5f) / HouseArt.PixelsPerUnit;
                float worldY = floorMin.y + (y + BoardHeight * 0.5f) / HouseArt.PixelsPerUnit;
                CreateSprite(root, "Plank", sprite, new Vector2(worldX, worldY), IsoSort.FloorOrder, material, false, 0f, 0);
                x += width;
                col++;
            }

            y += BoardHeight;
            row++;
        }
    }

    static int BoardWidth(int row, int col, int remain)
    {
        if (remain <= 48)
            return remain;

        int[] pattern = { 128, 96, 160, 112, 144, 80 };
        int width = pattern[(row + col) % pattern.Length];
        if (width > remain)
            width = remain;
        int after = remain - width;
        if (after > 0 && after < 32)
            width = remain;
        return width;
    }

    void LayBackWall(Transform root, Vector2 origin, float floorW, float floorH, Material material)
    {
        int width = FloorWidth + 96;
        float baseY = origin.y + floorH * 0.5f - LogHeight / HouseArt.PixelsPerUnit * 0.5f;
        const int count = 10;
        for (int i = 0; i < count; i++)
        {
            Sprite sprite = HouseArt.Log(width, i);
            float y = baseY + i * (LogHeight / HouseArt.PixelsPerUnit);
            CreateSprite(root, "Back Log", sprite, new Vector2(origin.x, y), 4, material, true, y, 0);
            if (i == 4 || i == 6)
            {
                float side = i == 4 ? -floorW * 0.28f : floorW * 0.28f;
                CreateSprite(root, "Window", HouseArt.Window(), new Vector2(origin.x + side, y), 4, material, true, y, 1);
            }
        }

        Vector2 hearth = new Vector2(origin.x, origin.y + floorH * 0.5f - 0.15f);
        CreateSprite(root, "Hearth", HouseArt.Hearth(), hearth, 4, material, true, hearth.y + 0.4f, 2);
    }

    void LaySideWall(Transform root, Vector2 origin, float floorW, float floorH, float side, Material material)
    {
        const int logWidth = 48;
        float x = origin.x + side * (floorW * 0.5f - logWidth / HouseArt.PixelsPerUnit * 0.35f);
        float bottom = origin.y - floorH * 0.5f + LogHeight / HouseArt.PixelsPerUnit * 0.5f;
        int count = FloorHeight / LogHeight;
        for (int i = 0; i < count; i++)
        {
            Sprite sprite = HouseArt.Log(logWidth, i + (side < 0f ? 2 : 4));
            float y = bottom + i * (LogHeight / HouseArt.PixelsPerUnit);
            CreateSprite(root, side < 0f ? "Left Log" : "Right Log", sprite, new Vector2(x, y), 4, material, true, y, 0);
        }
    }

    void LayFrontWall(Transform root, Vector2 origin, float floorW, float floorH, Material material)
    {
        const int gap = 96;
        int sideWidth = (FloorWidth - gap) / 2;
        float bottom = origin.y - floorH * 0.5f + LogHeight / HouseArt.PixelsPerUnit * 0.5f;
        float leftX = origin.x - gap / HouseArt.PixelsPerUnit * 0.5f - sideWidth / HouseArt.PixelsPerUnit * 0.5f;
        float rightX = origin.x + gap / HouseArt.PixelsPerUnit * 0.5f + sideWidth / HouseArt.PixelsPerUnit * 0.5f;
        const int courses = 7;
        for (int i = 0; i < courses; i++)
        {
            float y = bottom + i * (LogHeight / HouseArt.PixelsPerUnit);
            bool lintel = i == courses - 1;
            if (lintel)
            {
                CreateSprite(root, "Lintel", HouseArt.Log(FloorWidth - 40, i + 1), new Vector2(origin.x, y), 4, material, true, y, 1);
                continue;
            }

            CreateSprite(root, "Front Log", HouseArt.Log(sideWidth, i), new Vector2(leftX, y), 4, material, true, y, 0);
            CreateSprite(root, "Front Log", HouseArt.Log(sideWidth, i + 3), new Vector2(rightX, y), 4, material, true, y, 0);
        }

        Vector2 doorAt = new Vector2(origin.x, origin.y - floorH * 0.5f);
        CreateSprite(root, "Door", HouseArt.InteriorDoor(), doorAt, 4, material, true, doorAt.y + 1.2f, 1);
    }

    static Transform CreateDropAnchor(Transform root, Material material)
    {
        var go = new GameObject("Drops");
        go.transform.SetParent(root, false);
        var marker = go.AddComponent<SpriteRenderer>();
        marker.enabled = false;
        if (material != null)
            marker.sharedMaterial = material;
        return go.transform;
    }

    static SpriteRenderer CreateSprite(Transform parent, string name, Sprite sprite, Vector2 world, int order, Material material, bool sort, float lineY, int bias)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(world.x, world.y, 0f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        if (material != null)
            renderer.sharedMaterial = material;

        if (sort)
        {
            BillboardSort depth = go.AddComponent<BillboardSort>();
            depth.LineY = lineY;
            depth.OrderBias = bias;
        }

        return renderer;
    }

    static Material SharedMaterial()
    {
        PlayerController player = GameRoot.Active != null ? GameRoot.Active.Player : null;
        if (player == null)
            return null;

        var renderer = player.GetComponent<SpriteRenderer>();
        return renderer != null ? renderer.sharedMaterial : null;
    }
}
