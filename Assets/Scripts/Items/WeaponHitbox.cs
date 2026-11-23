using System;
using UnityEngine;

/// <summary>
/// Hit shape of one weapon. The shared reach stays the default for spears and glaives.
/// A sword puts its own box here, so the swing is not a second copy of that reach.
/// Sprite fields are pixels of the painted blade. World fields are units along the aim.
/// </summary>
[Serializable]
public class WeaponHitbox
{
    // Pixels of the painted blade, including the guard. Zero width keeps the presenter's own box.
    public float SpriteHalfLength;
    public float SpriteHalfWidth;
    public float SpriteAlong;
    public float SpriteSide;

    // World units. Zero length keeps the standard arc test.
    public float HalfLength;
    public float HalfWidth;
    public float Along;
    public float Side;

    public bool HasSpriteBox => SpriteHalfWidth > 0.5f && SpriteHalfLength > 0.5f;

    public bool HasWorldBox => HalfLength > 0.05f;
}

/// <summary>
/// One box test for every melee weapon that carries its own hit shape.
/// </summary>
public static class WeaponHit
{
    public static bool Box(Vector2 center, Vector2 axis, Vector2 point, float halfLength, float halfWidth, float padding)
    {
        if (halfLength <= 0f || axis.sqrMagnitude < 0.0001f)
            return false;

        axis.Normalize();
        Vector2 rel = point - center;
        float along = Vector2.Dot(rel, axis);
        float side = Mathf.Abs(Vector2.Dot(rel, new Vector2(-axis.y, axis.x)));
        float pastTip = Mathf.Max(Mathf.Abs(along) - halfLength, 0f);
        float pastEdge = Mathf.Max(side - Mathf.Max(0f, halfWidth), 0f);
        float reach = Mathf.Max(0f, padding);
        return pastTip * pastTip + pastEdge * pastEdge <= reach * reach;
    }
}
