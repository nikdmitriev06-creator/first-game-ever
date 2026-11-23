using UnityEngine;

/// <summary>
/// Wires scene objects together. It does not decide crafting, damage, or placement rules.
/// Player systems that used to appear from inventory Awake are attached here, once.
/// </summary>
[DefaultExecutionOrder(-200)]
public class GameRoot : MonoBehaviour
{
    public static GameRoot Active { get; private set; }

    public PlayerController Player { get; private set; }
    public PlayerInventory Inventory { get; private set; }
    public LocationState Locations { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Active = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Ensure()
    {
        if (FindAnyObjectByType<GameRoot>() != null)
            return;

        if (FindAnyObjectByType<PlayerController>() == null)
            return;

        var host = new GameObject("Game Root");
        host.AddComponent<GameRoot>();
    }

    void Awake()
    {
        if (Active != null && Active != this)
        {
            Destroy(gameObject);
            return;
        }

        Active = this;
        Locations = new LocationState();
        LocationState.Bind(Locations);
        WorldMemory.Load(Locations);
        GameplayGate.EnsureState();
        BindPlayer();
        WirePlayer();
    }

    void OnDestroy()
    {
        if (Active == this)
            Active = null;
    }

    public void BindPlayer()
    {
        Player = FindAnyObjectByType<PlayerController>();
        Inventory = Player != null ? Player.GetComponent<PlayerInventory>() : null;
    }

    void WirePlayer()
    {
        if (Player == null)
            return;

        GameObject host = Player.gameObject;
        if (host.GetComponent<CraftingSystem>() == null)
            host.AddComponent<CraftingSystem>();
        if (host.GetComponent<PlacementSystem>() == null)
            host.AddComponent<PlacementSystem>();
        if (host.GetComponent<HeldProp>() == null)
            host.AddComponent<HeldProp>();
        if (host.GetComponent<InventoryHud>() == null)
            host.AddComponent<InventoryHud>();
        if (host.GetComponent<NoticeHud>() == null)
            host.AddComponent<NoticeHud>();
        if (host.GetComponent<InteractionFocus>() == null)
            host.AddComponent<InteractionFocus>();
        if (host.GetComponent<IsoSortBody>() == null)
            host.AddComponent<IsoSortBody>();
        // Presentation: effects, ambience and biome light. No gameplay rules live here.
        WorldPresentation.Warm();
        DecorField.Ensure();
        DecorEvents.Ensure();
        AtmosphereDirector.Ensure();


        if (host.GetComponentInChildren<Axe>(true) == null)
        {
            var axe = new GameObject("Axe");
            axe.transform.SetParent(host.transform, false);
            axe.AddComponent<Axe>();
        }
    }
}
