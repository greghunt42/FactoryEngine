# Audio Subsystem Specification

This document defines the responsibilities, data flow, and extensibility of the FactoryEngine audio service described in `docs/service-layer.md`.

## Goals
- Consistent audio behavior across platforms while hiding MonoGame specifics.
- Logical mixing model (buses, groups) to support genre-specific effects.
- Data-driven configuration for sound banks and music playlists.
- Runtime controls for ducking, dynamic music, and positional audio.

## Architecture Overview
```
Systems -> Audio Commands -> Audio Service -> Backend Adapter (MonoGame)
```

- **Systems:** Publish events (e.g., `PlaySound`, `SetMusicState`) or manipulate audio components.
- **Audio Service:** Interprets commands, manages channels/buses, enforces volume curves, routes to backend.
- **Backend Adapter:** Concrete implementation per platform, owns `SoundEffect`, `Song`, streaming buffers.

## Data
### Sound Banks
```yaml
namespace: core_audio
sounds:
  ui_click:
    asset: core:ui_click
    group: ui
    volume: 0.8
  explosion_large:
    asset: core:explosion_large
    group: sfx
    spatial: true
```
- Maps logical sound IDs to asset catalog entries and default settings.

### Music Playlists
```yaml
playlists:
  exploration:
    tracks:
      - asset: rpg:overworld_theme
        weight: 2
      - asset: rpg:meadow_theme
    transitions:
      fadeIn: 2.0
      fadeOut: 2.0
```

## Runtime Concepts
- **Channels:** Individual playback instances for sounds or music.
- **Groups/Buses:** Hierarchical, e.g., `master > music`, `master > sfx`, `sfx > weapons`.
- **Snapshots:** Saved group states (volumes, filters) for quick transitions.
- **Ducking Rules:** e.g., lower music by 40% when narration plays.

## API Sketch
```csharp
audio.PlaySound(SoundId.UI_Click, in AudioParams { Position = pos });
audio.PlayMusicPlaylist("exploration");
audio.SetGroupVolume("sfx", 0.7f);
audio.PushSnapshot("combat");
```

- Commands are lightweight structs to minimize allocations.
- Positional audio uses ECS components to update listener/emitter states each frame.

## Event Integration
- Audio service emits events (`SoundStarted`, `SoundFinished`, `MusicStateChanged`) for gameplay logic.
- Systems can subscribe to handle achievements, UI feedback, etc.

## Diagnostics
- Expose per-group meters, currently playing sounds, and streaming buffer stats.
- Support capture logs for debugging missing assets or clipping.

## Future Work
- Middleware integration (FMOD/Wwise) via alternate adapters.
- Procedural audio graph definition for complex effects.
- ADR for mixing/effect processing pipeline when requirements emerge.
