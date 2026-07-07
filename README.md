# DupTerminator

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)
![Platform: Windows](https://img.shields.io/badge/Platform-Windows-0078D6)

**DupTerminator** — free, open-source duplicate file finder with advanced features: perceptual hashing (pHash) for visually similar images, archive scanning (50+ formats via 7-Zip SDK), and PDF content comparison.

> This is the **Archive branch** — a major evolution of the original project with a refactored architecture, multi-strategy search pipeline, and comprehensive archive support.

---

## Features

### Search Modes

| Mode | Description |
|------|-------------|
| **MD5** | Find exact file duplicates by MD5 hash |
| **MD5 Container** | Find exact duplicates inside archives (ZIP, RAR, 7z, etc.) |
| **pHash** | Find visually similar images using perceptual hashing |
| **pHash Container** | Find visually similar images inside archives |
| **pHash Search Container** | Search inside archives for images matching a given perceptual hash |
| **pHash Search Image** | Find duplicates of a specified image by perceptual hash |
| **PDF Content** | Compare PDF files by extracted text content |

### Archive Support
- **50+ formats** via 7-Zip SDK: 7z, ZIP, RAR, TAR, GZip, BZip2, CAB, ISO, WIM, NSIS, and many more
- **Nested archives** — recursive detection and extraction (up to 2 levels deep)
- **Efficient streaming** — `ChunkedMemoryStream` and `PooledMemoryStream` to avoid large heap allocations

### Image Similarity (pHash)
- Perceptual hashing (pHash) for **visually similar** image detection
- **MIH (Multi-Index Hashing)** for efficient similarity search with multiple indexing strategies:
  - `MIHIndex`, `MIHIndex2` — basic implementations
  - `MIHIndexSeek` — seek-optimized variant
  - `MIHIndexSIMD` — SIMD-accelerated variant
  - `MIHMy` — custom implementation

### Performance Optimizations
- **Multi-pass preliminary filtering**: CRC32 + file size pass before full hash computation
- **Per-drive parallel processing** for large file sets
- **SQLite caching** of computed MD5 hashes for repeated scans
- **SIMD-optimized** MIH indexing

### Safety & Usability
- **Recycle Bin delete** with Undo support
- **Thumbs.db** detection and filtering
- **Exclude locations** — configure folders to skip
- **Progress reporting** with cancellation support
- **Logging infrastructure** for diagnostics

---

## Architecture

```
┌──────────────────────────────────────────────┐
│               WPF UI Layer                    │
│         (DupTerminator.WPF)                   │
├──────────────────────────────────────────────┤
│           Business Logic Layer                │
│         (DupTerminator.BusinessLogic)         │
├──────────────────────────────────────────────┤
│           Data Access Layer                   │
│    (DupTerminator.DataBase)                   │
├──────────────────────────────────────────────┤
│       Specialized Libraries                   │
│  ImageHash / Pdf / Unsafe / WindowsSpecific   │
└──────────────────────────────────────────────┘
```

The project follows a **clean layered architecture** with dependency injection (Microsoft.Extensions.DependencyInjection).

### Key Design Principles

- **BusinessLogic independence**: The `DupTerminator.BusinessLogic` project has **zero dependencies** on other projects in the solution. All cross-layer communication is done through abstractions (interfaces) defined in its `Abstraction/` namespace.
- **Repository pattern** for data access (MD5, pHash, PDF, archive info)
- **Strategy pattern** for search modes — each mode is a separate `SearcherXxx` class inheriting from `SearcherBase<T>`
- **MVVM** with constructor-injected ViewModels

---

## Solution Structure

| Project | Description |
|---------|-------------|
| `DupTerminator.BusinessLogic` | Core logic: searchers, models, abstractions |
| `DupTerminator.WPF` | WPF UI (Views, ViewModels, Controls) |
| `DupTerminator.DataBase` | Repositories, JSON serialization, SQLite access |
| `DupTerminator.ImageHash` | Perceptual hashing (pHash) + MIH indexing |
| `DupTerminator.Pdf` | PDF text extraction and comparison |
| `DupTerminator.Unsafe` | Unsafe memory helpers |
| `DupTerminator.WindowsSpecific` | Windows utilities (file icons, Win32 API) |
| `DupTerminator.Test` | Unit tests |
| `DupTerminator.Benchmark` | Performance benchmarks |
| `DupTerminator.IntegrationTest` | Integration tests |
| `SevenZipExtractor` | 7-Zip SDK wrapper |

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10+ (WPF runtime)

### Build & Run

```bash
# Clone and build
git clone https://github.com/heinrichI/DupTerminator.git
cd DupTerminator
dotnet build DupTerminator.sln

# Run the application
dotnet run --project DupTerminator.WPF

# Run tests
dotnet test DupTerminator.Test

# Run benchmarks
dotnet run --project DupTerminator.Benchmark
```

### Publish

```bash
dotnet publish DupTerminator.WPF -c Release -o ./publish
```

---

## Technologies

- **.NET 8** / C# 12
- **WPF** (Windows Presentation Foundation)
- **Microsoft.Extensions.DependencyInjection** — DI container
- **System.Text.Json** — source-generated JSON serialization
- **System.Data.SQLite** — hash cache database
- **SevenZipExtractor** — archive extraction (50+ formats)
- **System.Drawing.Common** — image loading for pHash
- **System.IO.Hashing** — CRC32 preliminary pass

---

## License

This project is licensed under the **GNU General Public License v3.0** — see the [LICENSE](LICENSE) file for details.

---

## Acknowledgments

- Original DupTerminator project and contributors
- [7-Zip](https://www.7-zip.org/) SDK for archive extraction
- [pHash](https://www.phash.org/) — perceptual hash algorithm