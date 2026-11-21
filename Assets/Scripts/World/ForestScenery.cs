using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trees and rocks for one forest room. Each tree is a Tree, so an axe can fell it.
/// Trees prefer to arrive in pairs. Rocks stay scenery and keep their distance.
/// Counts stay in a band that reads on this large field: a few of each in view, not a thicket.
/// </summary>
public static class ForestScenery
{
    const float Edge = 10f;
    const float SpawnClear = 8.5f;
    const float ExitClear = 8.5f;
    const int TreeMin = 22;
    const int TreeMax = 35;
    const int RockMin = 8;
    const int RockMax = 13;
    const float PixelsPerUnit = 32f;

    static Sprite[] _trees;
    static Sprite[] _rocks;

    public static void Place(LevelWorld.Room room, bool clearSpawn, Vector2 spawn)
    {
        if (room == null || room.Root == null)
            return;

        EnsureArt();
        var renderer = room.Root.GetComponent<SpriteRenderer>();
        var trees = new List<Vector2>(TreeMax);
        int treeTarget = UnityEngine.Random.Range(TreeMin, TreeMax);
        int guard = 0;
        while (trees.Count < treeTarget && guard < 240)
        {
            guard++;
            Vector2 spot = RandomPoint(room);
            if (!Free(room, spot, clearSpawn, spawn))
                continue;
            if (Nearest(trees, spot) < 7f)
                continue;

            trees.Add(spot);
            if (trees.Count >= treeTarget || UnityEngine.Random.value > 0.75f)
                continue;

            Vector2 beside = spot + UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(2.7f, 4.4f);
            if (!Free(room, beside, clearSpawn, spawn))
                continue;
            if (Nearest(trees, beside) < 2.4f)
                continue;

            trees.Add(beside);
        }

        var rocks = new List<Vector2>(RockMax);
        int rockTarget = UnityEngine.Random.Range(RockMin, RockMax);
        guard = 0;
        while (rocks.Count < rockTarget && guard < 160)
        {
            guard++;
            Vector2 spot = RandomPoint(room);
            if (!Free(room, spot, clearSpawn, spawn))
                continue;
            if (Nearest(rocks, spot) < 11f)
                continue;
            if (Nearest(trees, spot) < 2.6f)
                continue;

            rocks.Add(spot);
        }

        trees.Sort((a, b) => b.y.CompareTo(a.y));
        rocks.Sort((a, b) => b.y.CompareTo(a.y));

        for (int i = 0; i < trees.Count; i++)
            Spawn(room, renderer, "Tree", _trees[UnityEngine.Random.Range(0, _trees.Length)], trees[i], UnityEngine.Random.Range(0.92f, 1.22f), true);

        for (int i = 0; i < rocks.Count; i++)
            Spawn(room, renderer, "Rock", _rocks[UnityEngine.Random.Range(0, _rocks.Length)], rocks[i], UnityEngine.Random.Range(0.95f, 1.45f), false);
    }

    static void Spawn(LevelWorld.Room room, SpriteRenderer ground, string name, Sprite sprite, Vector2 point, float scale, bool livingTree)
    {
        var go = new GameObject(name);
        go.transform.SetParent(room.Root.transform, false);
        go.transform.position = new Vector3(point.x, point.y, 0f);
        float flip = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        go.transform.localScale = new Vector3(flip * scale, scale, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = Depth(room, point.y);
        if (ground != null)
        {
            renderer.sortingLayerID = ground.sortingLayerID;
            if (ground.sharedMaterial != null)
                renderer.sharedMaterial = ground.sharedMaterial;
        }

        if (livingTree)
            go.AddComponent<Tree>();
    }

    static int Depth(LevelWorld.Room room, float y)
    {
        float t = Mathf.InverseLerp(room.Bounds.min.y, room.Bounds.max.y, y);
        return Mathf.RoundToInt(Mathf.Lerp(8f, 2f, t));
    }

    static Vector2 RandomPoint(LevelWorld.Room room)
    {
        Bounds bounds = room.Bounds;
        return new Vector2(
            UnityEngine.Random.Range(bounds.min.x + Edge, bounds.max.x - Edge),
            UnityEngine.Random.Range(bounds.min.y + Edge, bounds.max.y - Edge));
    }

    static bool Free(LevelWorld.Room room, Vector2 point, bool clearSpawn, Vector2 spawn)
    {
        Bounds bounds = room.Bounds;
        if (point.x < bounds.min.x + Edge || point.x > bounds.max.x - Edge)
            return false;
        if (point.y < bounds.min.y + Edge || point.y > bounds.max.y - Edge)
            return false;
        if (clearSpawn && (point - spawn).sqrMagnitude < SpawnClear * SpawnClear)
            return false;
        if (room.IsExit && ((Vector2)room.Bounds.center - point).sqrMagnitude < ExitClear * ExitClear)
            return false;

        return true;
    }

    static float Nearest(List<Vector2> points, Vector2 spot)
    {
        float best = 999f;
        for (int i = 0; i < points.Count; i++)
        {
            float distance = Vector2.Distance(points[i], spot);
            if (distance < best)
                best = distance;
        }

        return best;
    }

    static void EnsureArt()
    {
        if (_trees != null)
            return;

        _trees = new[]
        {
            Tree(0),
            Tree(1),
            Tree(2)
        };
        _rocks = new[]
        {
            Rock(0),
            Rock(1),
            Rock(2)
        };
    }

    static Sprite Tree(int kind)
    {
        const int width = 72;
        const int height = 118;
        var pixels = new Color32[width * height];
        var trunk = new Color32(104, 66, 36, 255);
        var trunkLit = new Color32(136, 90, 50, 255);
        var shadow = new Color32(28, 58, 26, 110);

        FillEllipse(pixels, width, height, 36, 7, 20, 5, shadow, 0);

        if (kind == 1)
        {
            FillRect(pixels, width, 33, 8, 6, 34, trunk);
            FillRect(pixels, width, 34, 8, 3, 34, trunkLit);
            FillEllipse(pixels, width, height, 36, 46, 16, 14, new Color32(28, 78, 34, 255), 1);
            FillEllipse(pixels, width, height, 36, 62, 20, 16, new Color32(36, 96, 40, 255), 2);
            FillEllipse(pixels, width, height, 36, 80, 22, 18, new Color32(46, 112, 46, 255), 3);
            FillEllipse(pixels, width, height, 36, 96, 16, 14, new Color32(58, 128, 52, 255), 4);
            FillEllipse(pixels, width, height, 30, 100, 7, 6, new Color32(138, 176, 78, 255), 5);
        }
        else if (kind == 2)
        {
            FillRect(pixels, width, 30, 10, 12, 28, trunk);
            FillRect(pixels, width, 31, 10, 4, 28, trunkLit);
            FillEllipse(pixels, width, height, 36, 48, 30, 22, new Color32(30, 84, 36, 255), 6);
            FillEllipse(pixels, width, height, 36, 64, 26, 20, new Color32(48, 118, 48, 255), 7);
            FillEllipse(pixels, width, height, 24, 70, 10, 8, new Color32(150, 186, 86, 255), 8);
        }
        else
        {
            FillRect(pixels, width, 32, 6, 8, 32, trunk);
            FillRect(pixels, width, 33, 6, 3, 32, trunkLit);
            FillEllipse(pixels, width, height, 36, 58, 26, 24, new Color32(32, 86, 36, 255), 9);
            FillEllipse(pixels, width, height, 36, 70, 22, 20, new Color32(52, 122, 50, 255), 10);
            FillEllipse(pixels, width, height, 28, 78, 10, 8, new Color32(146, 184, 84, 255), 11);
        }

        return Finish(pixels, width, height, new Vector2(0.5f, 0.02f));
    }

    static Sprite Rock(int kind)
    {
        const int width = 40;
        const int height = 26;
        var pixels = new Color32[width * height];
        var shade = new Color32(72, 70, 66, 255);
        var body = new Color32(118, 116, 110, 255);
        var light = new Color32(168, 166, 156, 255);
        FillEllipse(pixels, width, height, 20, 6, 14, 4, new Color32(40, 56, 32, 100), 0);

        if (kind == 1)
        {
            FillEllipse(pixels, width, height, 20, 12, 16, 8, shade, 2);
            FillEllipse(pixels, width, height, 20, 14, 14, 6, body, 3);
            FillEllipse(pixels, width, height, 16, 16, 6, 3, light, 4);
        }
        else if (kind == 2)
        {
            FillEllipse(pixels, width, height, 14, 11, 8, 6, shade, 5);
            FillEllipse(pixels, width, height, 24, 12, 9, 6, shade, 6);
            FillEllipse(pixels, width, height, 14, 13, 6, 4, body, 7);
            FillEllipse(pixels, width, height, 24, 14, 7, 4, body, 8);
            FillEllipse(pixels, width, height, 13, 15, 3, 2, light, 9);
        }
        else
        {
            FillEllipse(pixels, width, height, 20, 12, 11, 7, shade, 1);
            FillEllipse(pixels, width, height, 20, 14, 9, 5, body, 2);
            FillEllipse(pixels, width, height, 17, 16, 4, 2, light, 3);
        }

        return Finish(pixels, width, height, new Vector2(0.5f, 0.08f));
    }

    static void FillRect(Color32[] pixels, int width, int x, int y, int w, int h, Color32 color)
    {
        for (int py = y; py < y + h; py++)
        {
            for (int px = x; px < x + w; px++)
            {
                if ((uint)px >= (uint)width || py < 0 || py * width >= pixels.Length)
                    continue;
                pixels[py * width + px] = color;
            }
        }
    }

    static void FillEllipse(Color32[] pixels, int width, int height, int cx, int cy, int rx, int ry, Color32 color, int salt)
    {
        if (rx < 1 || ry < 1)
            return;

        for (int y = cy - ry; y <= cy + ry; y++)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                if ((uint)x >= (uint)width || (uint)y >= (uint)height)
                    continue;

                float dx = (x - cx) / (float)rx;
                float dy = (y - cy) / (float)ry;
                if (dx * dx + dy * dy > 1f)
                    continue;

                Color32 write = color;
                if (color.a == 255)
                {
                    int hash = (x * 17 + y * 31 + salt * 13) & 7;
                    if (hash == 0)
                        write = Darken(color, 0.72f);
                    else if (hash == 1)
                        write = Darken(color, 1.12f);
                }

                pixels[y * width + x] = write;
            }
        }
    }

    static Color32 Darken(Color32 color, float factor)
    {
        return new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * factor), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * factor), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * factor), 0, 255),
            255);
    }

    static Sprite Finish(Color32[] pixels, int width, int height, Vector2 pivot)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        texture.hideFlags = HideFlags.HideAndDontSave;

        var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, PixelsPerUnit);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
