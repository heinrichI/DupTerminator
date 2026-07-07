# System Architecture and Design Patterns

## Overall Architecture

DupTerminator follows a **clean layered architecture** with strict separation of concerns:

```
┌──────────────────────────────────────────────┐
│               WPF UI Layer                    │
│         (DupTerminator.WPF)                   │
│         Views / ViewModels / Controls         │
├──────────────────────────────────────────────┤
│           Business Logic Layer                │
│         (DupTerminator.BusinessLogic)         │
│       Searchers / Comparers / Models          │
├──────────────────────────────────────────────┤
│           Data Access Layer                   │
│    (DupTerminator.DataBase)                   │
│    Repositories / JSON / SQLite               │
├──────────────────────────────────────────────┤
│       Specialized Libraries                   │
│  ImageHash / Pdf / Unsafe / WindowsSpecific   │
└──────────────────────────────────────────────┘
```

## Search Mode Strategy Pattern

Each search mode is implemented as a separate class inheriting from `SearcherBase<T>`:

| Mode | Class | Algorithm | Result Type |
|------|-------|-----------|-------------|
| MD5 | `SearcherMD5` | MD5 hash of full file content | `MD5Result` |
| MD5 Container | `SearcherMD5Container` | MD5 hash of files inside archives | `MD5Result` |
| pHash | `SearcherPhash` | Perceptual hash for image similarity | `PhashResult` |
| pHash Container | `SearcherPhashContainer` | pHash of images inside archives | `PhashResult` |
| pHash Search Container | `SearcherPhashSearchContainer` | Search inside containers by pHash | `PhashResult` |
| pHash Search Image | `SearcherPhashSearchImage` | Find image duplicates by pHash | `PhashResult` |

### Multi-pass search pipeline
1. **Preliminary pass**: Quick CRC32 comparison to group files by size and initial hash chunk
2. **Full comparison pass**: Complete hash computation for files that passed preliminary check
3. **Archive pass**: Extract and compare contents inside supported archives (50+ formats)

## Key Design Patterns

### Dependency Injection
- `Microsoft.Extensions.DependencyInjection` for service registration and resolution
- Constructor injection across all ViewModels and Services
- Service lifetimes: Scoped for repositories, Singleton for services

### Repository Pattern
- `Md5Repository` — stores computed MD5 hashes with file metadata
- `PhashRepository` — stores perceptual hashes for images
- `PdfInfoRepository` — stores PDF content hashes
- `ArchiveInfoRepository` — stores archive entry CRCs and structure
- `ArchiveInfoRepositoryMemP` / `ArchiveInfoRepositoryMesP` — memory-optimized variants
- `ExtendedFileInfoRepositoryAsync` — async file info with parallel processing

### Strategy Pattern (Search Modes)
Each `SearcherXxx` class implements a specific strategy for comparison. They share a common base `SearcherBase<T>` which provides:
- Drive grouping via `GetPhisicalDrives()`
- Progress reporting with cancellation support
- Parallel file processing

### Object Model Hierarchy
```
ExtendedFileInfo (base)
├── ArchiveFileInfo — files inside archives (with ArchiveFileName, ArchivePath, ArchiveCRC)
├── MD5Result — search result with MD5 hash
├── PhashResult — search result with pHash value
└── ...
```

### MIH Indexing (Multi-Index Hashing)
For efficient perceptual hash search, multiple indexing strategies are implemented:
- `MIHIndex` / `MIHIndex2` — basic MIH implementations
- `MIHIndexSeek` — seek-optimized version
- `MIHIndexSIMD` — SIMD-accelerated version
- `MIHMy` — custom implementation

## Archive Support Architecture

### Container Hierarchy
```
ContainerInfo — stores archive metadata (path, format, size, entry count)
ArchiveFileInfo — represents a single file inside an archive
  └── Supports nested archives (archive-in-archive, 2 levels)
```

### Archive Extraction Pipeline
1. Detect file format via 7-Zip SDK signature inspection
2. Extract entry data to `ChunkedMemoryStream` or `PooledMemoryStream`
3. For nested archives: recursively detect and extract
4. Compute hash (MD5 or pHash) on extracted data
5. Store result with full archive path context

## UI Architecture (MVVM)

```
Model (ObjectModel/*) → ViewModel (ViewModel/*) → View (View/*.xaml)
                              ↕
                        Services (DI)
```

Key ViewModels:
- `MainViewModel` — application orchestrator, manages modes and states
- `SearchViewModel` — search configuration and execution
- `DuplicateResultsViewModel` — result display and management
- `ProgressViewModel` — search progress with cancellation

## Critical Implementation Paths

### Search Execution Flow
```
User clicks "Search" → ViewModel → SearcherBase.SearchAsync()
  → GetPhisicalDrives() → Per-drive parallel processing
  → Preliminary pass (size + CRC32)
  → Full hash computation (MD5 / pHash / PDF)
  → Archive scanning (if enabled)
  → Result aggregation → Display in grouped UI
```

### File Deletion Flow
```
User selects duplicates → ViewModel → FileOperations service
  → Move to Recycle Bin (with Undo support)
  → Or permanent delete
  → Update result groupings
```

## Architecture Constraint: BusinessLogic Independence

**The BusinessLogic library (`DupTerminator.BusinessLogic`) must be completely independent.**

RULES:
- NO imports, dependencies, or references to modules/files outside this library
- NO references to global application variables, framework-specific components, or UI elements
- NO dependency cycles — BusinessLogic may NOT reference WPF, DataBase, or any other project
- BusinessLogic communicates with other layers exclusively through:
  - **Abstractions** (interfaces in `Abstraction/` namespace) — defined IN the BusinessLogic project
  - **Dependency Injection** — implementations are injected at composition root (WPF layer)
- BusinessLogic provides interfaces (`IMd5Repository`, `IPhashRepository`, `IArchiveService`, etc.) — other layers implement them