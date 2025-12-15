using System;

namespace FactoryEngine.Core.Systems;

internal static class AccessAnalyzer
{
    public static bool HasConflict(ComponentAccess a, ComponentAccess b)
    {
        return Intersects(a.WriteComponents, b.ReadComponents) ||
               Intersects(a.ReadComponents, b.WriteComponents) ||
               Intersects(a.WriteComponents, b.WriteComponents);
    }

    private static bool Intersects(Type[] left, Type[] right)
    {
        foreach (var l in left)
        {
            foreach (var r in right)
            {
                if (l == r)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
