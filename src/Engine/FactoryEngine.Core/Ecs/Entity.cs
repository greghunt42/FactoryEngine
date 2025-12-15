namespace FactoryEngine.Core.Ecs;

/// <summary>
/// Lightweight entity handle composed of index + generation.
/// </summary>
public readonly record struct Entity(int Index, uint Generation)
{
    public bool IsValid => Index >= 0;

    public override string ToString() => $"{Index}:{Generation}";

    public static Entity Invalid => new(-1, 0);
}
