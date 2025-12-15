namespace FactoryEngine.Core.Services.Input;

public sealed class NullInputService : IInputService
{
    public event Action<ActionEvent>? OnActionTriggered;

    public void LoadActionMap(string actionMapId)
    {
    }

    public void EnableContext(string contextName)
    {
    }

    public void DisableContext(string contextName)
    {
    }

    public ActionState GetActionState(string actionName) => new(0f, false);

    public void SetActionState(string actionName, ActionState state)
    {
    }

    public void Trigger(ActionEvent evt) => OnActionTriggered?.Invoke(evt);

    public void RegisterActionMap(ActionMap map)
    {
    }

    public void LoadActionMapFromJson(string path)
    {
    }
}
