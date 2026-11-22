using UnityEngine;

/// <summary>
/// Shows the placeable in the hand. Weapons and tools keep their own renderers.
/// </summary>
public class HeldProp : MonoBehaviour
{
    PlayerInventory _inventory;
    PlayerController _player;
    SpriteRenderer _renderer;
    SpriteRenderer _playerRenderer;
    string _shown;

    void Awake()
    {
        _inventory = GetComponent<PlayerInventory>();
        _player = GetComponent<PlayerController>();
        _playerRenderer = GetComponent<SpriteRenderer>();

        var go = new GameObject("Held Prop");
        go.transform.SetParent(transform, false);
        _renderer = go.AddComponent<SpriteRenderer>();
        _renderer.enabled = false;
        if (_playerRenderer != null)
        {
            _renderer.sortingLayerID = _playerRenderer.sortingLayerID;
            if (_playerRenderer.sharedMaterial != null)
                _renderer.sharedMaterial = _playerRenderer.sharedMaterial;
        }
    }

    void LateUpdate()
    {
        if (_renderer == null || _inventory == null || GameplayGate.MenuOpen)
        {
            Hide();
            return;
        }

        ItemDefinition item = ItemCatalog.Find(_inventory.SlotAt(_inventory.Selected));
        if (item == null || !item.IsPlaceable || _inventory.CountAt(_inventory.Selected) <= 0)
        {
            Hide();
            return;
        }

        if (_shown != item.Id)
        {
            _renderer.sprite = item.IconSprite;
            _shown = item.Id;
            Fit(_renderer.sprite);
        }

        _renderer.enabled = _renderer.sprite != null;
        Vector2 facing = _player != null ? _player.FacingDirection : Vector2.down;
        if (facing.sqrMagnitude < 0.01f)
            facing = Vector2.down;

        Vector2 side = new Vector2(-facing.y, facing.x);
        Vector2 grip = facing.normalized * 0.35f + side.normalized * 0.42f + Vector2.up * 0.85f;
        _renderer.transform.localPosition = new Vector3(grip.x, grip.y, 0f);
        if (_playerRenderer != null)
            _renderer.sortingOrder = _playerRenderer.sortingOrder + 1;
    }

    void Hide()
    {
        if (_renderer != null)
            _renderer.enabled = false;
        _shown = null;
    }

    void Fit(Sprite sprite)
    {
        if (sprite == null)
        {
            _renderer.transform.localScale = Vector3.one;
            return;
        }

        float pixels = sprite.rect.height / Mathf.Max(1f, sprite.pixelsPerUnit);
        float scale = pixels > 0.01f ? 0.7f / pixels : 1f;
        float parent = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
        _renderer.transform.localScale = new Vector3(scale / parent, scale / parent, 1f);
    }
}
