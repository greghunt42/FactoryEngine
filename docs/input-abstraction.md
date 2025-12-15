# Input Abstraction Specification

FactoryEngine must shield gameplay code from platform-specific input APIs while remaining flexible for modules. This document defines the input service responsibilities and data model.

## Goals
- Support keyboard, mouse, gamepad, touch, and future devices.
- Data-driven action mapping and rebinding.
- Deterministic event ordering and integration with ECS.
- Extensible for module-defined input layers (e.g., RTS hotkeys vs. platformer controls).

## Architecture
```
Platform Adapters -> Input Service -> Input Components & Events
```

- **Platform Adapters:** Capture raw device states (MonoGame window, native APIs) and emit standardized events (`ButtonDown`, `AxisChanged`).
- **Input Service:** Aggregates events per frame, applies action maps, updates ECS components, and publishes events via the event bus.
- **Input Components:** Entities (players, UI widgets) store action states, analog values, device associations.

## Data Model
### Action Map File
```yaml
name: default
contexts:
  gameplay:
    actions:
      move_left:
        type: axis
        bindings:
          - device: keyboard
            key: A
            scale: -1
          - device: gamepad
            axis: LeftStickX
      jump:
        type: button
        bindings:
          - device: keyboard
            key: Space
          - device: gamepad
            button: A
```
- Contexts can be enabled/disabled at runtime (e.g., gameplay vs. UI).
- Binding metadata includes dead zones, sensitivity curves.

## ECS Integration
- `InputComponent`: stores current action values (float/button state) and metadata (player ID).
- Input system (Input phase) updates components based on action maps and raw events.
- Modules can subscribe to `ActionTriggered` events for decoupled logic.

## Rebinder API
- Input service exposes functions to remap bindings at runtime (with persistence to data files):
```csharp
input.BeginRebind("jump", deviceFilter: DeviceType.Keyboard);
input.CompleteRebind(newBinding);
```

## Determinism
- Input service queues events per frame with timestamps.
- During deterministic capture/replay, inputs are fed from recorded streams instead of live devices.

## Diagnostics
- Input overlay shows device states, active contexts, and action values.
- Logging records binding conflicts or missing devices.

## Future Work
- Touch gesture recognizer module.
- Accessibility features (toggle vs hold, sensitivity profiles).
- ADR for input capture serialization format.
