namespace FactoryEngine.Core.Services.Input;

public sealed class InputService : IInputService
{
    private readonly Dictionary<string, ActionMap> _actionMaps = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _activeContexts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ActionState> _states = new(StringComparer.OrdinalIgnoreCase);

    public event Action<ActionEvent>? OnActionTriggered;

    public void RegisterActionMap(ActionMap map)
    {
        _actionMaps[map.Name] = map;
        foreach (var action in map.Actions)
        {
            _states[action.Name] = default;
        }
    }

    public void LoadActionMap(string actionMapId)
    {
        _activeContexts.Add(actionMapId);
    }

    public void EnableContext(string contextName)
    {
        _activeContexts.Add(contextName);
    }

    public void DisableContext(string contextName)
    {
        _activeContexts.Remove(contextName);
    }

    public ActionState GetActionState(string actionName)
    {
        return _states.TryGetValue(actionName, out var state) ? state : default;
    }

    public void SetActionState(string actionName, ActionState state)
    {
        _states[actionName] = state;
        OnActionTriggered?.Invoke(new ActionEvent(actionName, state));
    }

    public void LoadActionMapFromJson(string path)
    {
        var json = File.ReadAllText(path);
        var document = System.Text.Json.JsonDocument.Parse(json);
        var root = document.RootElement;
        var map = new ActionMap { Name = root.GetProperty("name").GetString() ?? "default" };
        foreach (var actionElement in root.GetProperty("actions").EnumerateArray())
        {
            var binding = new ActionBinding
            {
                Name = actionElement.GetProperty("name").GetString() ?? string.Empty
            };
            map.Actions.Add(binding);
        }

        RegisterActionMap(map);
    }
}

public sealed class ActionMap
{
    public required string Name { get; init; }
    public List<ActionBinding> Actions { get; } = new();
}

public sealed class ActionBinding
{
    public required string Name { get; init; }
    public string Context { get; init; } = "default";
}
