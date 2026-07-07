# Project Brief

## DupTerminator — Duplicate File Finder

DupTerminator is a free, open-source program for finding, managing, and removing duplicate files. It supports multiple comparison methods including MD5 hashing, perceptual hashing (pHash) for visually similar images, CRC32 comparison, and PDF content comparison. The application can also search inside archives (50+ formats via 7-Zip SDK) and handle nested archives.

### Core Requirements

- **Duplicate Detection Accuracy**: Find exact and similar duplicates using multiple algorithms
- **Performance**: Efficient processing of large file sets with chunked memory streams, database caching, and SIMD-optimized indexing
- **Flexibility**: Multiple search modes for different use cases (MD5, pHash, archive containers, PDF)
- **Usability**: Intuitive WPF UI with group management, sorting, filtering, bulk operations
- **Extensibility**: Clean architecture with dependency injection, abstraction layers, and separation of concerns

### Target Platforms

- Windows (.NET 8, WPF)
- .NET Standard libraries for business logic (platform-agnostic)

### License

Open source software.