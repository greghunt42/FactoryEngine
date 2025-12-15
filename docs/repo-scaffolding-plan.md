# Repository Scaffolding Plan

With design docs in place, this plan outlines the initial steps to create the codebase per the roadmap.

## Directory Structure
```
FactoryEngine/
  docs/
  src/
    Engine/
      FactoryEngine.Core/      # ECS, services, diagnostics
      FactoryEngine.Core.Tests/
    Tools/
      FeTools/
      FeTools.Tests/
    Samples/
      FactoryPlatformer/
      FactoryPlatformer.Tests/
```

## Step-by-Step
1. **Initialize Solution**
   - Create `FactoryEngine.sln`.
   - Add projects: `FactoryEngine.Core` (Class Library), `FactoryEngine.Core.Tests` (xUnit/NUnit), `FeTools` (Console), `FeTools.Tests`, `FactoryPlatformer` (Executable), `FactoryPlatformer.Tests`.

2. **Configure Build**
   - Add `Directory.Build.props/targets` for common settings (nullable, LangVersion, analyzers).
   - Set up multi-targeting if needed (net8.0 for core, net8.0-desktop for samples).

3. **Set Up CI**
   - GitHub Actions workflow running `dotnet build` + `dotnet test` for all projects.
   - Cache NuGet packages for faster builds.

4. **Logging Infrastructure**
   - Add NDJSON logger implementation per ADR-0006 (maybe in `FactoryEngine.Diagnostics`).
   - Provide console sink and in-memory ring buffer for crash dumps.

5. **Service Interfaces**
   - Implement interfaces from `docs/service-interfaces.md` with placeholder classes (no MonoGame dependencies yet).
   - Add dependency injection setup (simple service container or explicit builder).

6. **World/ECS Skeleton**
   - Create `World`, `WorldBuilder`, placeholder entity/component registries ready for implementation in Phase 1.

7. **Sample Runner Skeleton**
   - `FactoryPlatformer` project references engine core, sets up window/MonoGame entry point (even if not functional yet).

8. **Tooling Project Skeleton**
   - Implement CLI entry/argument parser with stub commands logging "Not implemented".

9. **Docs & Journal**
   - Update `docs/design-journal.md` with scaffolding progress.
   - Reference relevant docs/ADRs in README or contributing guide once available.

## Deliverables
- Solution builds successfully with placeholder implementations.
- Basic logging/service scaffolding ready for future work.
- CI pipeline operational.
