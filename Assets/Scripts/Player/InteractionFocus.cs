using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// One interaction button for the nearest world object. It does not swing a tool.
/// </summary>
public class InteractionFocus : MonoBehaviour
{
    const float Reach = 2.6f;

    IInteractable _focus;
    SpriteRenderer _lit;
    Color _litColor = Color.white;
    InputAction _interact;
    GameObject _canvas;
    TMP_Text _label;

    void Awake()
    {
        if (InputSystem.actions != null)
            _interact = InputSystem.actions.FindAction("Player/Interact", throwIfNotFound: false);
        _interact?.Enable();
        BuildPrompt();
    }

    void OnDisable()
    {
        ClearHighlight();
        Show(false);
    }

    void Update()
    {
        bool allowed = GameplayGate.EnsureState().Allows(PlayerAction.Interact);
        PlayerInventory bag = GetComponent<PlayerInventory>();
        if (bag != null && bag.IsOpen)
            allowed = false;

        IInteractable next = allowed ? Interactables.Nearest(transform.position, Reach) : null;
        if (!Same(next, _focus))
            Retarget(next);

        if (_focus == null)
        {
            Show(false);
            return;
        }

        bool can = _focus.CanInteract;
        _label.text = can ? _focus.Label + "  [" + _focus.Verb + "]" : _focus.Label + "\n" + _focus.Refusal;
        Show(true);
        if (!can || !Pressed())
            return;

        _focus.Interact(new InteractionContext { Actor = transform, Feet = transform.position });
        if (_focus != null)
            _label.text = _focus.CanInteract ? _focus.Label + "  [" + _focus.Verb + "]" : _focus.Label + "\n" + _focus.Refusal;
    }

    void Retarget(IInteractable next)
    {
        ClearHighlight();
        _focus = next;
        if (_focus == null)
            return;

        Component body = _focus as Component;
        if (body == null)
            return;

        _lit = body.GetComponent<SpriteRenderer>();
        if (_lit == null)
            return;

        _litColor = _lit.color;
        _lit.color = Color.Lerp(_litColor, new Color(1f, 0.92f, 0.55f, 1f), 0.45f);
    }

    void ClearHighlight()
    {
        if (_lit != null)
            _lit.color = _litColor;
        _lit = null;
    }

    bool Pressed()
    {
        if (_interact != null)
            return _interact.WasPressedThisFrame();

        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame;
    }

    static bool Same(IInteractable a, IInteractable b)
    {
        return (a as Object) == (b as Object);
    }

    void Show(bool visible)
    {
        if (_canvas != null)
            _canvas.SetActive(visible);
    }

    void BuildPrompt()
    {
        _canvas = new GameObject("Interact Prompt");
        UiScreen.Mount(_canvas, 120);

        var textObject = new GameObject("Label");
        textObject.transform.SetParent(_canvas.transform, false);
        var rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 148f);
        rect.sizeDelta = new Vector2(720f, 76f);
        _label = UiGlyphs.Place(textObject, string.Empty, 26f, TextAnchor.MiddleCenter, new Color(0.96f, 0.93f, 0.84f, 1f), UiGlyphs.Wrap.Single, true);
        _canvas.SetActive(false);
    }
}
