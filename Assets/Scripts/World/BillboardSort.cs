using UnityEngine;

/// <summary>
/// Depth from the foot line, the same rule as the player. A lower Y draws in front.
/// </summary>
public class BillboardSort : MonoBehaviour
{
    public float LineY;
    public int OrderBias;
    public bool Follow;

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

        if (_playerRenderer != null)
            _renderer.sortingLayerID = _playerRenderer.sortingLayerID;

        float foot = Follow ? IsoSort.SortY(transform, _renderer) : LineY;
        _renderer.sortingOrder = IsoSort.Stable(foot, gameObject.GetEntityId().GetHashCode(), OrderBias);
    }
}
