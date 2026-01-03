using FactoryEngine.Core.Systems;
using FactoryPlatformer.Components;

namespace FactoryPlatformer.Systems;

public sealed class HazardPatrolSystem : SystemBase
{
    public HazardPatrolSystem()
    {
        DeclareAccess(builder => builder
            .Writes<Transform2D>()
            .Writes<HazardPatrol>());
    }

    protected override void OnRun(SystemContext context)
    {
        var dt = context.DeltaTime;
        foreach (var entity in World!.Query(builder => builder
                     .All<Transform2D>()
                     .All<HazardPatrol>()))
        {
            ref var transform = ref World.GetComponent<Transform2D>(entity);
            ref var patrol = ref World.GetComponent<HazardPatrol>(entity);
            if (!patrol.Initialized)
            {
                patrol.Initialized = true;
                if (Math.Abs(patrol.OriginX) < float.Epsilon)
                {
                    patrol.OriginX = transform.X;
                }
                if (Math.Abs(patrol.OriginY) < float.Epsilon)
                {
                    patrol.OriginY = transform.Y;
                }
                if (Math.Abs(patrol.Direction) < float.Epsilon)
                {
                    patrol.Direction = 1f;
                }
            }

            var delta = patrol.Speed * dt * patrol.Direction;
            patrol.Offset += delta;
            if (patrol.Offset >= patrol.Range)
            {
                patrol.Offset = patrol.Range;
                patrol.Direction = -Math.Abs(patrol.Direction);
            }
            else if (patrol.Offset <= -patrol.Range)
            {
                patrol.Offset = -patrol.Range;
                patrol.Direction = Math.Abs(patrol.Direction);
            }

            switch (patrol.Axis)
            {
                case PatrolAxis.Vertical:
                    transform.Y = patrol.OriginY + patrol.Offset;
                    break;
                default:
                    transform.X = patrol.OriginX + patrol.Offset;
                    break;
            }
        }
    }
}
