# Rendering Facade & Command Buffer

This document defines the initial rendering abstraction that shields modules from MonoGame specifics while supporting 2D-first workflows.

## Goals
- Allow systems in the RenderPrep phase to emit draw commands without touching `SpriteBatch`/MonoGame APIs.
- Enable future batching optimizations and backend swaps without changing gameplay code.
- Provide diagnostic hooks (frame capture, stats).

## Command Buffer
RenderPrep systems obtain a `RenderCommandBuffer` for the current world:
```csharp
var cmd = renderService.GetFrameBuffer();
cmd.DrawSprite(spriteId: Assets.Core.PlayerIdle,
               transform: worldMatrix,
               colorTint: Color.White,
               layer: 0.5f,
               material: Materials.BasicLit);
```

### Command Types (v1)
- `DrawSprite`: textured quad with position, rotation, scale, color tint, layer depth, material ID.
- `DrawText`: sprite-font text draw with alignment, color, layer.
- `DrawMesh2D`: optional for tilemaps or complex geometry (indices + vertices).
- `PushClip` / `PopClip`: define scissor rectangles.
- `SetCamera`: declare camera matrices per layer/target.

Commands are recorded into a linear buffer. The rendering backend sorts/ batches them before issuing to MonoGame.

## Materials & Shaders
- Materials are logical IDs referencing shader + blend state combos.
- Modules request materials via the asset service or register new ones through the rendering service.
- RenderPrep commands reference materials by ID, keeping MonoGame `Effect` objects hidden.

## Render Targets
- Systems can request logical render targets (e.g., `SceneColor`, `UIOverlay`).
- The service manages actual `RenderTarget2D` objects and handles resizing per window/device.

## Cameras
- Cameras are components that register with the rendering service per frame.
- Command buffer associates draws with camera IDs; backend computes final matrices.

## Diagnostics
- `RenderStats` exposes draw call counts, triangles, material switches.
- Optional capture mode records command buffer contents for replay in tooling.

## Future Extensions
- 3D primitives and instancing.
- GPU-driven tilemaps or particle systems.
- Multi-threaded command recording once ECS phases can parallelize RenderPrep.

Implementation details (buffer structs, pooling) will be covered in a future ADR once prototypes exist.
