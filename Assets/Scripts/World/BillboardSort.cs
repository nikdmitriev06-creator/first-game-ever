using UnityEngine;

/// <summary>
/// Puts this sprite behind or in front of the player using one ground line.
/// South of the line the player covers the sprite. North of it, the sprite covers the player.
/// </summary>
public class BillboardSort : MonoBehaviour
{
    public float LineY;
    public int OrderBias;

    SpriteRenderer _renderer;
    SpriteRenderer _playerRenderer;
    PlayerController _player;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (_renderer == null)
            return;

        if (_player == null && GameRoot.Active != null)
        {
            _player = GameRoot.Active.Player;
            if (_player != null)
                _playerRenderer = _player.GetComponent<SpriteRenderer>();
        }

        if (_playerRenderer == null)
            return;

        bool playerInFront = _player.transform.position.y < LineY;
        _renderer.sortingLayerID = _playerRenderer.sortingLayerID;
        _renderer.sortingOrder = _playerRenderer.sortingOrder + (playerInFront ? -4 : 3) + OrderBias;
    }
}
