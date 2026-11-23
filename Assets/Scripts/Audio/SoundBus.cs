using System.Collections.Generic;
using UnityEngine;

public enum SoundGroup
{
    Master,
    Music,
    Sfx,
    Ambient
}

/// <summary>
/// One bus for footsteps, hits, music and ambience. A missing clip is silence, not an error.
/// The same event cannot stack faster than its cooldown, and a short window caps every one-shot together.
/// </summary>
public static class SoundBus
{
    const int Voices = 6;
    const int BurstCap = 8;

    public static float Master = 1f;
    public static float Music = 1f;
    public static float Sfx = 1f;
    public static float Ambient = 1f;
    public static bool Muted;
    public static bool AllowInEditMode;

    static readonly Dictionary<FxEvent, List<AudioClip>> Clips = new Dictionary<FxEvent, List<AudioClip>>();
    static readonly Dictionary<FxEvent, float> Next = new Dictionary<FxEvent, float>();
    static readonly Dictionary<FxEvent, float> Cooldown = new Dictionary<FxEvent, float>();
    static readonly Dictionary<FxEvent, SoundGroup> Groups = new Dictionary<FxEvent, SoundGroup>();
    static readonly Dictionary<FxEvent, float> Gains = new Dictionary<FxEvent, float>();
    static readonly Dictionary<FxEvent, bool> Spatial = new Dictionary<FxEvent, bool>();
    static readonly Dictionary<string, AudioClip> MusicClips = new Dictionary<string, AudioClip>();
    static readonly Dictionary<string, AudioClip> AmbientClips = new Dictionary<string, AudioClip>();
    static AudioSource[] _voices;
    static AudioSource _bed;
    static AudioSource _music;
    static int _voice;
    static int _played;
    static int _burst;
    static float _burstUntil;
    static string _bedId;
    static string _musicId;

    public static int PlayedCount => _played;
    public static string AmbientId => _bedId;
    public static string CurrentMusicId => _musicId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        Clips.Clear();
        Next.Clear();
        Cooldown.Clear();
        Groups.Clear();
        Gains.Clear();
        Spatial.Clear();
        MusicClips.Clear();
        AmbientClips.Clear();
        if (_voices != null)
        {
            for (int i = 0; i < _voices.Length; i++)
                Forget(_voices[i]);
        }

        Forget(_bed);
        Forget(_music);
        _voices = null;
        _bed = null;
        _music = null;
        _voice = 0;
        _played = 0;
        _burst = 0;
        _burstUntil = 0f;
        _bedId = null;
        _musicId = null;
        Master = Music = Sfx = Ambient = 1f;
        Muted = false;
        AllowInEditMode = false;
    }

    static void Forget(AudioSource source)
    {
        if (source != null)
            Object.DestroyImmediate(source.gameObject);
    }

    public static void Warm()
    {
        SoundProfile[] profiles = Resources.LoadAll<SoundProfile>("Audio");
        if (profiles != null)
        {
            for (int i = 0; i < profiles.Length; i++)
                Bind(profiles[i]);
        }

        AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio");
        if (clips == null)
            return;

        for (int i = 0; i < clips.Length; i++)
            Adopt(clips[i]);
    }

    public static void Bind(SoundProfile profile)
    {
        if (profile == null)
            return;

        if (profile.Cues != null)
        {
            for (int i = 0; i < profile.Cues.Length; i++)
            {
                SoundCue cue = profile.Cues[i];
                if (cue == null)
                    continue;

                Groups[cue.Event] = cue.Group;
                Cooldown[cue.Event] = Mathf.Max(0f, cue.Cooldown);
                Gains[cue.Event] = Mathf.Clamp01(cue.Volume);
                Spatial[cue.Event] = cue.Spatial;
                if (cue.Clips == null)
                    continue;

                for (int c = 0; c < cue.Clips.Length; c++)
                    Register(cue.Event, cue.Clips[c]);
            }
        }

        if (profile.Music != null && !string.IsNullOrEmpty(profile.MusicId))
            RegisterMusic(profile.MusicId, profile.Music);
    }

    public static void SetVolume(SoundGroup group, float value)
    {
        value = Mathf.Clamp01(value);
        switch (group)
        {
            case SoundGroup.Music: Music = value; break;
            case SoundGroup.Sfx: Sfx = value; break;
            case SoundGroup.Ambient: Ambient = value; break;
            default: Master = value; break;
        }
    }

    public static float Volume(SoundGroup group)
    {
        if (Muted || Master <= 0f)
            return 0f;

        float lane = group == SoundGroup.Music ? Music : group == SoundGroup.Ambient ? Ambient : Sfx;
        if (group == SoundGroup.Master)
            lane = 1f;
        return Mathf.Clamp01(Master * lane);
    }

    public static void Register(FxEvent ev, AudioClip clip)
    {
        if (clip == null)
            return;

        List<AudioClip> list;
        if (!Clips.TryGetValue(ev, out list))
        {
            list = new List<AudioClip>(2);
            Clips[ev] = list;
        }

        if (!list.Contains(clip))
            list.Add(clip);
    }

    public static void RegisterMusic(string id, AudioClip clip)
    {
        if (string.IsNullOrEmpty(id) || clip == null)
            return;

        MusicClips[id] = clip;
    }

    public static void RegisterAmbient(string id, AudioClip clip)
    {
        if (string.IsNullOrEmpty(id) || clip == null)
            return;

        AmbientClips[id] = clip;
    }

    public static bool HasClip(FxEvent ev)
    {
        List<AudioClip> list;
        return Clips.TryGetValue(ev, out list) && list.Count > 0 && list[0] != null;
    }

    public static void Play(FxEvent ev, Vector3 point)
    {
        if (!Live() || Muted)
            return;

        AudioClip clip = Pick(ev);
        if (clip == null)
            return;

        float ready;
        if (Next.TryGetValue(ev, out ready) && Time.unscaledTime < ready)
            return;

        if (Time.unscaledTime > _burstUntil)
        {
            _burst = 0;
            _burstUntil = Time.unscaledTime + 0.1f;
        }

        if (_burst >= BurstCap)
            return;

        float gap;
        if (!Cooldown.TryGetValue(ev, out gap))
            gap = ev == FxEvent.AmbientSting ? 4f : ev == FxEvent.Step ? 0.1f : 0.08f;

        Next[ev] = Time.unscaledTime + gap;
        _burst++;

        bool spatial = true;
        if (Spatial.TryGetValue(ev, out bool flagged))
            spatial = flagged;

        AudioSource voice = Voice(spatial);
        if (voice == null)
            return;

        voice.transform.position = point;
        voice.spatialBlend = spatial ? 1f : 0f;
        float gain;
        if (!Gains.TryGetValue(ev, out gain))
            gain = 1f;
        voice.PlayOneShot(clip, Volume(GroupFor(ev)) * gain);
        _played++;
    }

    public static void PlayMusic(string id)
    {
        if (!Live())
            return;

        _musicId = id;
        AudioClip clip = null;
        if (!string.IsNullOrEmpty(id))
            MusicClips.TryGetValue(id, out clip);

        AudioSource source = MusicVoice();
        if (source == null)
            return;

        if (clip == null)
        {
            source.Stop();
            source.clip = null;
            return;
        }

        if (source.clip == clip && source.isPlaying)
            return;

        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = Volume(SoundGroup.Music);
        source.Play();
    }

    public static void StopMusic()
    {
        _musicId = null;
        if (_music != null)
        {
            _music.Stop();
            _music.clip = null;
        }
    }

    public static void SetAmbient(string id)
    {
        if (!Live())
            return;

        if (id == _bedId)
            return;

        _bedId = id;
        AudioClip clip = null;
        if (!string.IsNullOrEmpty(id))
            AmbientClips.TryGetValue(id, out clip);

        AudioSource source = Bed();
        if (source == null)
            return;

        if (clip == null)
        {
            source.Stop();
            source.clip = null;
            return;
        }

        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = Volume(SoundGroup.Ambient);
        source.Play();
    }

    public static void Tick()
    {
        if (_bed != null)
            _bed.volume = Volume(SoundGroup.Ambient);
        if (_music != null)
            _music.volume = Volume(SoundGroup.Music);
    }

    public static SoundGroup GroupFor(FxEvent ev)
    {
        SoundGroup group;
        if (Groups.TryGetValue(ev, out group))
            return group;

        switch (ev)
        {
            case FxEvent.Water:
            case FxEvent.AmbientSting:
                return SoundGroup.Ambient;
            default:
                return SoundGroup.Sfx;
        }
    }

    static AudioClip Pick(FxEvent ev)
    {
        List<AudioClip> list;
        if (!Clips.TryGetValue(ev, out list) || list.Count == 0)
            return null;

        int start = Random.Range(0, list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            AudioClip clip = list[(start + i) % list.Count];
            if (clip != null)
                return clip;
        }

        return null;
    }

    static void Adopt(AudioClip clip)
    {
        if (clip == null || string.IsNullOrEmpty(clip.name))
            return;

        string name = clip.name.ToLowerInvariant();
        if (name.StartsWith("music_"))
        {
            RegisterMusic(name.Substring(6), clip);
            return;
        }

        if (name.StartsWith("ambient_"))
        {
            RegisterAmbient(name.Substring(8), clip);
            return;
        }

        FxEvent ev;
        if (!TryEvent(name, out ev))
            return;

        Register(ev, clip);
        if (ev == FxEvent.Water || ev == FxEvent.AmbientSting)
            Groups[ev] = SoundGroup.Ambient;
    }

    static bool TryEvent(string name, out FxEvent ev)
    {
        ev = FxEvent.Hit;
        if (name.StartsWith("sfx_"))
            name = name.Substring(4);

        int split = name.LastIndexOf('_');
        if (split > 0 && name.Length > split + 1 && char.IsDigit(name[split + 1]))
            name = name.Substring(0, split);

        switch (name)
        {
            case "hit": ev = FxEvent.Hit; return true;
            case "critical": ev = FxEvent.Critical; return true;
            case "hurt": ev = FxEvent.Hurt; return true;
            case "bleed": ev = FxEvent.Bleed; return true;
            case "death": ev = FxEvent.Death; return true;
            case "harvest": ev = FxEvent.Harvest; return true;
            case "chop": ev = FxEvent.Chop; return true;
            case "mine": ev = FxEvent.Mine; return true;
            case "ore": ev = FxEvent.Ore; return true;
            case "break": ev = FxEvent.Break; return true;
            case "pickup": ev = FxEvent.Pickup; return true;
            case "chest": ev = FxEvent.Chest; return true;
            case "rare": ev = FxEvent.Rare; return true;
            case "magic": ev = FxEvent.Magic; return true;
            case "bow": ev = FxEvent.Bow; return true;
            case "crossbow": ev = FxEvent.Crossbow; return true;
            case "craft": ev = FxEvent.Craft; return true;
            case "step": ev = FxEvent.Step; return true;
            case "water": ev = FxEvent.Water; return true;
            case "sting": ev = FxEvent.AmbientSting; return true;
            default: return false;
        }
    }

    static bool Live()
    {
        return Application.isPlaying || AllowInEditMode;
    }

    static AudioSource Voice(bool spatial)
    {
        if (_voices == null || _voices.Length != Voices)
        {
            _voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
                _voices[i] = Make("Sfx Voice", true);
        }

        AudioSource voice = _voices[_voice];
        _voice = (_voice + 1) % Voices;
        if (!spatial && voice != null)
            voice.spatialBlend = 0f;
        return voice;
    }

    static AudioSource Bed()
    {
        if (_bed == null)
            _bed = Make("Ambient Bed", false);
        return _bed;
    }

    static AudioSource MusicVoice()
    {
        if (_music == null)
            _music = Make("Music Bed", false);
        return _music;
    }

    static AudioSource Make(string name, bool spatial)
    {
        var go = new GameObject(name);
        go.hideFlags = HideFlags.HideAndDontSave;
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = spatial ? 1f : 0f;
        source.minDistance = 1.5f;
        source.maxDistance = 18f;
        source.rolloffMode = AudioRolloffMode.Linear;
        return source;
    }
}
