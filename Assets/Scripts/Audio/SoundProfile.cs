using System;
using UnityEngine;

/// <summary>
/// Variants for one event. Empty clips stay silent; they are not treated as missing-file errors.
/// </summary>
[Serializable]
public class SoundCue
{
    public FxEvent Event = FxEvent.Hit;
    public SoundGroup Group = SoundGroup.Sfx;
    public AudioClip[] Clips = Array.Empty<AudioClip>();
    public float Cooldown = 0.08f;
    public bool Spatial = true;
    [Range(0f, 1f)] public float Volume = 1f;
}

/// <summary>
/// Central clip list. Music and ambience stay empty until a real file is assigned.
/// </summary>
[CreateAssetMenu(fileName = "SoundProfile", menuName = "Game/Sound Profile")]
public class SoundProfile : ScriptableObject
{
    public SoundCue[] Cues = Array.Empty<SoundCue>();
    public string MusicId;
    public AudioClip Music;
}
