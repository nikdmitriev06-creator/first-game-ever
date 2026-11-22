using UnityEngine;

/// <summary>
/// Cuts the slime strips into frames. Every sheet is one row of 128-pixel cells,
/// and the feet sit a quarter of the way up the cell, so one pivot fits the hop and the splat.
/// </summary>
public static class SlimeSprites
{
    const int FrameWidth = 128;
    const float FeetPivot = 0.25f;

    static Set _set;

    public struct Set
    {
        public Sprite[] Idle;
        public Sprite[] Jump;
        public Sprite[] Hurt;
        public Sprite[] Dead;

        public bool Ready => Idle != null && Idle.Length > 0;
    }

    public static Set Load()
    {
        if (_set.Ready)
            return _set;

        _set = new Set
        {
            Idle = Slice("Slime/Walk"),
            Jump = Slice("Slime/Jump"),
            Hurt = Slice("Slime/Hurt"),
            Dead = Slice("Slime/Dead")
        };
        return _set;
    }

    static Sprite[] Slice(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Sprite whole = Resources.Load<Sprite>(resourcePath);
            if (whole != null)
                texture = whole.texture;
        }

        if (texture == null)
        {
            Debug.LogError("Missing slime sheet at Resources/" + resourcePath);
            return System.Array.Empty<Sprite>();
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        int count = Mathf.Max(1, texture.width / FrameWidth);
        var frames = new Sprite[count];
        float height = texture.height;
        var pivot = new Vector2(0.5f, FeetPivot);
        for (int i = 0; i < count; i++)
        {
            var rect = new Rect(i * FrameWidth, 0f, FrameWidth, height);
            Sprite sprite = Sprite.Create(texture, rect, pivot, PixelUnits.PerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = resourcePath + "_" + i;
            sprite.hideFlags = HideFlags.DontSave;
            frames[i] = sprite;
        }

        return frames;
    }
}
