# 0005 - Asset Identifier Scheme

Date: 2024-01-01

## Status
Accepted

## Context
- Assets originate from diverse pipelines (textures, audio, prefabs) and live in different directories.
- Modules and games must reference assets in data without depending on platform-specific paths.
- Hot reload, build caching, and network sync all rely on stable identifiers.

## Decision
Adopt logical asset IDs with namespace + name syntax (`namespace:asset_name`):
- Namespaces map to asset catalogs (e.g., `core`, `platformer`, `dlc1`).
- Loader resolves IDs through registered asset providers that translate to physical files or bundles.
- Asset IDs are case-insensitive ASCII to avoid filesystem variance.
- Hashes of the resolved asset content provide cache keys but do not replace the logical ID.

## Consequences
- **Pros:** Stable references across filesystems, easy mod support, clean logging/diagnostics.
- **Cons:** Requires catalog registration boilerplate; renaming assets needs migration tooling.
- **Follow-ups:** Define asset catalog manifest structure and integrate IDs with serialization validation.

## Alternatives Considered
- **Raw file paths:** Simple but brittle across platforms and packaging formats.
- **GUIDs only:** Unique but unreadable and difficult for humans to author in data files.
