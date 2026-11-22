using System;
using UnityEngine;

/// <summary>
/// Old name for the control stack. MenuOpen means movement is blocked.
/// New code asks ControlState.Allows.
/// </summary>
public static class GameplayGate
{
    static int _claimFrame = -1;

    public static ControlState State { get; private set; }

    public static bool MenuOpen => !EnsureState().Allows(PlayerAction.Move);

    public static event Action Changed;

    public static bool UseClaimed => _claimFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        State = new ControlState();
        _claimFrame = -1;
        Changed = null;
    }

    public static ControlState EnsureState()
    {
        if (State == null)
            State = new ControlState();

        return State;
    }

    public static void SetMenuOpen(bool open)
    {
        ControlState state = EnsureState();
        bool before = MenuOpen;
        if (open)
            state.Push("menu");
        else
            state.Pop("menu");

        if (before != MenuOpen)
            Changed?.Invoke();
    }

    public static void ClaimUse()
    {
        _claimFrame = Time.frameCount;
    }
}
