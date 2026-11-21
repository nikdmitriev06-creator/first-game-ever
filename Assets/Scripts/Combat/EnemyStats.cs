using System;
using UnityEngine;

/// <summary>
/// Combat numbers shared by every enemy. A slime only fills these in and then plays its own movement.
/// Ranges are in source pixels at 32 pixels per unit, the same space as the character sheet.
/// </summary>
[Serializable]
public class EnemyStats
{
    public float maxHealth = 100f;
    public float damage = 10f;
    // Seconds from one hit to the next while the player stays in reach.
    public float attackInterval = 0.5f;
    // Gap between the two bodies, in source pixels. 192 is 6 blocks, and one block is 32 pixels, one world unit.
    public float noticeRange = 192f;
    // Hits land in this band around the body, so the player does not have to stand inside the sprite.
    public float attackPadding = 10f;

    public float MaxHealth => Mathf.Max(1f, maxHealth);
    public float Damage => Mathf.Max(0f, damage);
    public float AttackInterval => Mathf.Max(0.05f, attackInterval);
    public float NoticeRange => Mathf.Max(0f, noticeRange);
    public float AttackPadding => Mathf.Max(0f, attackPadding);
}
