using System.IO;
using UnityEngine;

/// <summary>
/// Location level is the progress written here. The same file also keeps the hotbar slot,
/// minimap mode and sound volumes. Run difficulty never calls Save.
/// </summary>
public static class ProgressionService
{
    [System.Serializable]
    class ProgressFile
    {
        public LocationLevelRecord[] locations;
        public int hotbarSlot;
        public int mapMode;
        public float master = 1f;
        public float music = 1f;
        public float sfx = 1f;
        public float ambient = 1f;
        public int audioSaved;
    }

    static GameProgress _progress = new GameProgress();
    static RunSession _run = new RunSession();
    static bool _write = true;
    static int _hotbarSlot;
    static int _mapMode;
    static float _master = 1f;
    static float _music = 1f;
    static float _sfx = 1f;
    static float _ambient = 1f;
    static int _audioSaved;

    public static GameProgress Progress => _progress;
    public static RunSession Run => _run;
    public static int HotbarSlot => _hotbarSlot;
    public static int MapMode => _mapMode;
    public static bool PersistInterface { get; set; }
    public static event System.Action<string, int> LocationAdvanced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        _progress = new GameProgress();
        _run = new RunSession();
        _write = true;
        _hotbarSlot = 0;
        _mapMode = 0;
        _master = _music = _sfx = _ambient = 1f;
        _audioSaved = 0;
        PersistInterface = false;
        LocationAdvanced = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (_progress == null)
            _progress = new GameProgress();
        Load();
        ApplyAudio();
    }

    public static void Use(GameProgress progress, bool writeToDisk)
    {
        _progress = progress ?? new GameProgress();
        _run = new RunSession();
        _write = writeToDisk;
    }

    public static void EnableDisk()
    {
        _write = true;
        _progress = new GameProgress();
        _run = new RunSession();
        Load();
        ApplyAudio();
    }

    public static int LocationLevel(string locationId)
    {
        if (string.IsNullOrEmpty(locationId))
            return 1;

        return _progress.Get(locationId);
    }

    public static int EffectiveDifficulty(string locationId)
    {
        if (_run.DifficultyOverride.HasValue)
            return Mathf.Max(1, _run.DifficultyOverride.Value);

        return LocationLevel(locationId);
    }

    public static void SetRunDifficulty(int? difficulty)
    {
        _run.DifficultyOverride = difficulty.HasValue ? Mathf.Max(1, difficulty.Value) : (int?)null;
    }

    public static void ClearOverride()
    {
        _run.DifficultyOverride = null;
    }

    public static void AdvanceLocation(string locationId)
    {
        if (string.IsNullOrEmpty(locationId))
            return;

        int level = _progress.Get(locationId) + 1;
        _progress.Set(locationId, level);
        Save();
        LocationAdvanced?.Invoke(locationId, level);
    }

    public static void RememberHotbar(int slot)
    {
        _hotbarSlot = Mathf.Clamp(slot, 0, 9);
        RememberInterface();
    }

    public static void RememberMap(int mode)
    {
        _mapMode = Mathf.Clamp(mode, 0, 2);
        RememberInterface();
    }

    public static void RememberAudio(float master, float music, float sfx, float ambient, bool muted)
    {
        _master = Mathf.Clamp01(master);
        _music = Mathf.Clamp01(music);
        _sfx = Mathf.Clamp01(sfx);
        _ambient = Mathf.Clamp01(ambient);
        _audioSaved = muted ? 2 : 1;
        RememberInterface();
    }

    public static void ApplyAudio()
    {
        if (_audioSaved == 0)
            return;

        SoundBus.Muted = _audioSaved == 2;
        SoundBus.SetVolume(SoundGroup.Master, _master);
        SoundBus.SetVolume(SoundGroup.Music, _music);
        SoundBus.SetVolume(SoundGroup.Sfx, _sfx);
        SoundBus.SetVolume(SoundGroup.Ambient, _ambient);
    }

    static void RememberInterface()
    {
        if (_write && PersistInterface)
            Save();
    }

    public static void Load()
    {
        string path = FilePath();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return;

        try
        {
            ProgressFile file = JsonUtility.FromJson<ProgressFile>(File.ReadAllText(path));
            _progress = new GameProgress();
            if (file == null)
                return;

            ReadInterface(file);
            if (file.locations == null)
                return;

            for (int i = 0; i < file.locations.Length; i++)
            {
                LocationLevelRecord record = file.locations[i];
                if (record != null && !string.IsNullOrEmpty(record.id))
                    _progress.Set(record.id, record.level);
            }
        }
        catch (System.Exception error)
        {
            Debug.LogWarning("Location progress was not read: " + error.Message);
        }
    }

    public static void Save()
    {
        if (!_write)
            return;

        string path = FilePath();
        if (string.IsNullOrEmpty(path))
            return;

        var file = new ProgressFile
        {
            locations = _progress.Locations.ToArray(),
            hotbarSlot = _hotbarSlot,
            mapMode = _mapMode,
            master = _master,
            music = _music,
            sfx = _sfx,
            ambient = _ambient,
            audioSaved = _audioSaved
        };
        File.WriteAllText(path, JsonUtility.ToJson(file));
    }

    static void ReadInterface(ProgressFile file)
    {
        _hotbarSlot = Mathf.Clamp(file.hotbarSlot, 0, 9);
        _mapMode = Mathf.Clamp(file.mapMode, 0, 2);
        _audioSaved = file.audioSaved;
        if (_audioSaved == 0)
            return;

        _master = Mathf.Clamp01(file.master);
        _music = Mathf.Clamp01(file.music);
        _sfx = Mathf.Clamp01(file.sfx);
        _ambient = Mathf.Clamp01(file.ambient);
    }

    static string FilePath()
    {
        if (string.IsNullOrEmpty(Application.persistentDataPath))
            return null;

        return Path.Combine(Application.persistentDataPath, "location-progress.json");
    }
}
