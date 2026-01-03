using System;
using FactoryEngine.Core.Ecs;
using FactoryEngine.Core.Systems;
using FactoryPlatformer.Components;

namespace FactoryPlatformer.Systems;

public sealed class CameraFollowSystem : SystemBase
{
    public CameraFollowSystem()
    {
        DeclareAccess(builder => builder
            .Reads<Transform2D>()
            .Reads<CameraTarget>()
            .Writes<Camera2D>());
    }

    protected override void OnRun(SystemContext context)
    {
        var cameraEntity = Entity.Invalid;
        foreach (var cameraRef in World!.Query<Camera2D>())
        {
            cameraEntity = cameraRef.Entity;
            break;
        }

        if (!cameraEntity.IsValid)
        {
            return;
        }

        ref var camera = ref World.GetComponent<Camera2D>(cameraEntity);
        if (!camera.Enabled)
        {
            return;
        }

        var targetEntity = Entity.Invalid;
        foreach (var targetRef in World.Query<CameraTarget>())
        {
            targetEntity = targetRef.Entity;
            break;
        }

        if (!targetEntity.IsValid)
        {
            return;
        }

        ref var transform = ref World.GetComponent<Transform2D>(targetEntity);
        var halfWidth = camera.ViewportWidth * 0.5f;
        var halfHeight = camera.ViewportHeight * 0.5f;
        var targetX = Math.Clamp(transform.X, camera.MinX, camera.MaxX);
        var targetY = Math.Clamp(transform.Y, camera.MinY, camera.MaxY);
        camera.OffsetX = targetX - halfWidth;
        camera.OffsetY = targetY - halfHeight;
    }
}
