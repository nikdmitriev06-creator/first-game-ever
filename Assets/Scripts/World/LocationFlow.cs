using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads and unloads the house interior. The scene name lives only here.
/// </summary>
public static class LocationFlow
{
    public const string InteriorScene = "HouseInterior";
    public static readonly Vector2 InteriorOrigin = new Vector2(0f, -8000f);

    static bool _inside;
    static bool _busy;
    static bool _hooked;
    static float _lockUntil;
    static Vector2 _returnPoint;
    static Action _afterExit;

    public static bool IsInside => _inside;
    public static bool IsBusy => _busy;
    public static bool Pending => _busy && !_inside;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        _inside = false;
        _busy = false;
        _hooked = false;
        _lockUntil = 0f;
        _afterExit = null;
    }

    public static void Enter(Vector2 returnPoint)
    {
        if (_inside || _busy || Time.unscaledTime < _lockUntil)
            return;

        EnsureHook();
        _busy = true;
        _returnPoint = returnPoint;
        _lockUntil = Time.unscaledTime + 0.3f;
        WorldPresentation.ClearTransient();
        if (LevelWorld.Instance != null)
            LevelWorld.Instance.SuspendForInterior(returnPoint);

        try
        {
            SceneManager.LoadSceneAsync(InteriorScene, LoadSceneMode.Additive);
        }
        catch (Exception)
        {
            _busy = false;
            if (LevelWorld.Instance != null)
                LevelWorld.Instance.ResumeFromInterior(_returnPoint);
        }
    }

    public static void PlaceInside(Vector2 spawn, PlayArea area)
    {
        _inside = true;
        _busy = false;
        PlayerController player = Player();
        if (player != null)
        {
            if (area != null)
                player.SetPlayArea(area);
            player.Place(spawn);
        }

        Camera cam = Camera.main;
        if (cam != null)
            cam.backgroundColor = new Color(0.07f, 0.045f, 0.03f, 1f);
    }

    public static void Exit(Action then = null)
    {
        if (!_inside)
        {
            then?.Invoke();
            return;
        }

        if (_busy)
            return;

        _busy = true;
        _inside = false;
        _lockUntil = Time.unscaledTime + 0.3f;
        _afterExit = then;
        WorldPresentation.ClearTransient();

        if (HouseInterior.Active != null && LevelWorld.Instance != null)
            HouseInterior.Active.EvacuateDrops(LevelWorld.Instance.HomeAnchor, _returnPoint);

        if (LevelWorld.Instance != null)
            LevelWorld.Instance.ResumeFromInterior(_returnPoint);

        AsyncOperation unload = null;
        try
        {
            Scene scene = SceneManager.GetSceneByName(InteriorScene);
            if (scene.isLoaded)
                unload = SceneManager.UnloadSceneAsync(scene);
        }
        catch (Exception)
        {
            unload = null;
        }

        if (unload == null)
        {
            FinishExit();
            return;
        }

        unload.completed += _ => FinishExit();
    }

    static void FinishExit()
    {
        _busy = false;
        Action then = _afterExit;
        _afterExit = null;
        then?.Invoke();
    }

    static void EnsureHook()
    {
        if (_hooked)
            return;

        _hooked = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != InteriorScene || !_busy || _inside)
            return;

        if (HouseInterior.Active != null)
            return;

        _busy = false;
        if (LevelWorld.Instance != null)
            LevelWorld.Instance.ResumeFromInterior(_returnPoint);
    }

    static PlayerController Player()
    {
        if (GameRoot.Active != null && GameRoot.Active.Player != null)
            return GameRoot.Active.Player;

        return UnityEngine.Object.FindAnyObjectByType<PlayerController>();
    }
}
