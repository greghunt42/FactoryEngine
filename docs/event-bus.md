# Event Bus Specification

The event bus is the glue between modules and the ECS kernel. It enables decoupled communication without forcing systems to reference each other directly.

## Goals
- Minimize coupling between modules.
- Provide synchronous delivery for deterministic logic with optional async channels for tooling.
- Keep allocation and boxing low to avoid GC spikes in hot loops.

## Concepts
- **Topics:** Identified by struct type or string key. Struct topics are preferred for compile-time safety.
- **Publishers:** Any system or service can publish to a topic.
- **Subscribers:** Functions or objects registered per topic, optionally filtered by world or phase.

## Delivery Modes
- **Immediate:** Default. Events dispatch synchronously to subscribers in registration order.
- **Buffered:** For cross-phase or cross-thread delivery, events are queued and flushed at specific pipeline points.
- **Async/Telmetry:** Non-deterministic channel for editor tooling or analytics, isolated from gameplay logic.

## API Sketch
```csharp
bus.Subscribe<PlayerDamaged>(priority: 10, handler: OnPlayerDamaged);
bus.Publish(new PlayerDamaged { Entity = e, Amount = 25 });
```

- Subscription returns a disposable handle to unregister.
- Priorities allow modules to run before/after core handlers.

## Integration with ECS
- Lifecycle events (entity created/destroyed, component added/removed) are emitted through the bus.
- Systems should prefer events over direct component mutations when triggering distant effects.
- Event handlers can request read-only snapshots of components through the world interface.

## Performance Considerations
- Struct events stay on the stack where possible; avoid delegates that capture heap state.
- Subscriber lists are pooled per topic and reuse storage to minimize churn.
- Instrumentation tracks per-topic publish counts and handler timings.

## Future Enhancements
- Event recording/replay for deterministic debugging.
- Network replication adapters that translate events into netcode messages.
