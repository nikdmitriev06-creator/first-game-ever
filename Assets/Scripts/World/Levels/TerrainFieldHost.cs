using UnityEngine;

/// <summary>
/// Keeps one <see cref="TerrainField"/> in the navigation list for the lifetime of this object.
/// </summary>
public class TerrainFieldHost : MonoBehaviour
{
    TerrainField _field;

    public TerrainField Field => _field;

    public void Bind(TerrainField field)
    {
        if (_field != null)
            TerrainNavigation.Unregister(_field);

        _field = field;
        if (isActiveAndEnabled && _field != null)
            TerrainNavigation.Register(_field);
    }

    void OnEnable()
    {
        if (_field != null)
            TerrainNavigation.Register(_field);
    }

    void OnDisable()
    {
        if (_field != null)
            TerrainNavigation.Unregister(_field);
    }
}
