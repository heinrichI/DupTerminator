# Technical Context

## Technologies Used

### Core Platform
- **.NET 8.0** (target framework for WPF and libraries)
- **WPF** (Windows Presentation Foundation) — UI framework
- **C# 12** — primary language

### Key Libraries and Dependencies

| Library | Purpose |
|---------|---------|
| `Microsoft.Extensions.DependencyInjection` | DI container for service registration/resolution |
| `System.Text.Json` | JSON serialization with source generators |
| `System.Data.SQLite` | SQLite database for cached MD5 hashes |
| `SevenZipExtractor` | 7-Zip SDK wrapper (archive extraction, 50+ formats) |
| `System.Drawing.Common` | Image loading for pHash computation |
| `System.IO.Hashing` | CRC32 for fast preliminary comparison |

### Solution Structure

| Project | Type | Purpose |
|---------|------|---------|
| `DupTerminator.BusinessLogic` | .NET 8 Class Library | Core business logic, searchers, models, abstractions |
| `DupTerminator.WPF` | WPF App | UI layer (Views, ViewModels) |
| `DupTerminator.DataBase` | .NET 8 Class Library | Repositories, JSON helpers, SQLite access |
| `DupTerminator.ImageHash` | .NET 8 Class Library | Perceptual hashing (pHash), MIH indexing |
| `DupTerminator.Pdf` | .NET 8 Class Library | PDF text extraction and comparison |
| `DupTerminator.Unsafe` | .NET 8 Class Library | Unsafe memory operations |
| `DupTerminator.WindowsSpecific` | .NET 8 Class Library | Windows-specific utilities (file icons, Win32) |
| `DupTerminator.Test` | .NET 8 Test | Unit tests |
| `DupTerminator.Benchmark` | .NET 8 Console | Performance benchmarks |
| `DupTerminator.IntegrationTest` | .NET 8 Test | Integration tests |
| `SevenZipExtractor` | .NET 8 Library | 7-Zip archive extraction wrapper |

## Development Setup

- **IDE**: Visual Studio Code or Visual Studio 2022+
- **SDK**: .NET 8 SDK
- **Build**: `dotnet build` or VS build
- **Test**: `dotnet test`
- **Benchmark**: `dotnet run --project DupTerminator.Benchmark`

## Technical Constraints

1. **BusinessLogic Independence** (CRITICAL):
   - `DupTerminator.BusinessLogic` must NOT reference any other project in the solution
   - It defines abstractions (interfaces) in `Abstraction/` namespace that other layers implement
   - All cross-layer communication happens through DI-injected interfaces

2. **Windows-Only**:
   - WPF and `System.Drawing.Common` limit the WPF layer to Windows
   - Business logic libraries are .NET 8 (.NET Standard compatible in principle)

3. **Memory Efficiency**:
   - `ChunkedMemoryStream` and `PooledMemoryStream` for efficient large file processing
   - Avoids large heap allocations during archive extraction

4. **Archive Format Coverage**:
   - 50+ formats via 7-Zip SDK (7z, Zip, Rar, Tar, GZip, BZip2, Cab, Iso, Wim, etc.)
   - Nested archive support (2 levels deep)

5. **Large File Sets**:
   - Per-drive parallel processing
   - Preliminary CRC32 pass before full hash computation
   - SQLite caching of computed hashes

## Tool Usage Patterns

### Common CLI Commands
```bash
# Build the solution
dotnet build DupTerminator.sln

# Run tests
dotnet test DupTerminator.Test

# Run benchmarks
dotnet run --project DupTerminator.Benchmark

# Publish
dotnet publish DupTerminator.WPF -c Release