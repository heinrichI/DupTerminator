# Progress

## What Works

- **MD5 Search** — full file hashing with SQLite caching and preliminary CRC32 pass
- **MD5 Container Search** — MD5 hash computation for files inside archives
- **pHash Search** — perceptual image hash for visually similar duplicates
- **pHash Container Search** — pHash of images extracted from archives
- **pHash Search Container** — search inside containers using perceptual hashing
- **pHash Search Image** — find image duplicates by pHash
- **PDF Content Comparison** — text extraction and hash-based comparison
- **Archive Extraction** — 50+ formats via 7-Zip SDK, including nested archives (2 levels)
- **MIH Indexing** — multiple implementations (MIHIndex, MIHIndex2, MIHIndexSeek, MIHIndexSIMD, MIHMy)
- **JSON Serialization** — polymorphic serialization with source-generated context (ArchiveJsonContext)
- **Memory-Efficient Streams** — ChunkedMemoryStream and PooledMemoryStream for large file/archive processing
- **Recycle Bin Delete** — safe deletion with undo support
- **Progress Reporting** — per-drive parallel processing with cancellation
- **Thumbs.db Checking** — skip/check thumbs.db files
- **Exclude Locations** — configure folders to exclude from search
- **Logging Infrastructure** — diagnostic logging throughout search pipeline
- **BusinessLogic Independence** — zero external dependencies in the core logic layer

## What's Left to Build

- Save/Load test implementation for search results
- Sorting display improvements in the results view
- Nested ignore folders support
- UI polish for archive browsing and result navigation
- MIH implementation consolidation or benchmark-driven selection criteria
- Extended unit test coverage for archive-related features

## Current Status

The project is in a **stable development state** with all core duplicate detection features implemented and functional. The Archive branch introduced major architectural improvements over master:

- Refactored from monolithic WinForms-style code to clean layered architecture with DI
- Added comprehensive archive support (50+ formats)
- Added perceptual hashing for image similarity detection
- Added PDF content comparison
- Multiple MIH indexing strategies for optimal pHash search performance

## Known Issues

- Multiple MIH implementations may have overlapping or inconsistent behavior
- Archive extraction depth limited to 2 levels for nested archives
- Some unit tests may be incomplete for archive-specific scenarios
- BusinessLogic independence must be verified on each PR/commit

## Evolution of Project Decisions

| Decision | Rationale |
|----------|-----------|
| Move to .NET 8 | Modern runtime with performance improvements and source generators |
| Use SevenZipExtractor | Full format coverage without licensing issues |
| Source-generated JSON | Type safety and performance for polymorphic serialization |
| Multiple MIH strategies | Exploration of optimal indexing for different data distributions |
| ChunkedMemoryStream | Avoid LOH fragmentation for large archive entries |
| BusinessLogic isolation | Ensure testability and future portability to other platforms |