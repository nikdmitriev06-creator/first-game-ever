using UnityEngine;

/// <summary>
/// A world object that answers one tool action. It is not a destructible and not a container.
/// </summary>
public class ToolActionHost : MonoBehaviour, IToolActionReceiver
{
    [SerializeField] string actionId;

    public int Applications { get; private set; }

    public bool CanReceiveAction => isActiveAndEnabled;
    public Vector2 Origin => transform.position;

    public void Configure(string id)
    {
        actionId = id;
    }

    public bool Apply(in ToolAction action)
    {
        if (!string.IsNullOrEmpty(actionId) && action.ActionId != actionId)
            return false;

        Applications++;
        return true;
    }

    void OnEnable()
    {
        ActionReceiverRegistry.Register(this);
    }

    void OnDisable()
    {
        ActionReceiverRegistry.Unregister(this);
    }
}
