namespace FactoryEngine.Core.Services.Input;

public interface IInputService
{
    void LoadActionMap(string actionMapId);
    void EnableContext(string contextName);
    void DisableContext(string contextName);
    ActionState GetActionState(string actionName);
    void SetActionState(string actionName, ActionState state);
    event Action<ActionEvent>? OnActionTriggered;
    void RegisterActionMap(ActionMap map);
    void LoadActionMapFromJson(string path);
}

public readonly record struct ActionState(float Value, bool IsPressed);

public readonly record struct ActionEvent(string ActionName, ActionState State);
