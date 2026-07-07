# Active Context

## Current Work Focus

The project is at the **Archive branch** state — a major evolution from the original master branch with significant new features:

1. **Archive support** (50+ formats via 7-Zip SDK)
2. **Perceptual hashing** (pHash) for visually similar image detection
3. **Multiple search modes** (MD5, MD5 Container, pHash, pHash Container, pHash Search Container, pHash Search Image)
4. **PDF content comparison**
5. **Refactored architecture** with clean DI, abstractions, and layered design
6. **JSON serialization with polymorphic type discriminators** for archive file info

## Recent Changes

Based on the Archive branch git history:
- ChunkedMemoryStream with ulong support
- MIH indexing optimizations (MIHMy, MIHIndexSIMD)
- ContainerPairKey implementation
- PDF info repository JSON fix
- Logging infrastructure
- Cancel check in phash mode
- Thumbs.db checking
- Delete button and excludeLocations feature
- Various bugfixes and stability improvements

## Active Decisions and Considerations

1. **Serialization Strategy**: Using System.Text.Json source generators (ArchiveJsonContext) for polymorphic serialization of ExtendedFileInfo/ArchiveFileInfo with $type discriminators
2. **MIH Indexing**: Multiple implementations exist (MIHIndex, MIHIndex2, MIHIndexSeek, MIHIndexSIMD, MIHMy) — may need consolidation or clear selection criteria
3. **Memory Management**: Using ChunkedMemoryStream and PooledMemoryStream for efficient archive extraction — careful tuning needed for large archives
4. **BusinessLogic Independence**: Strict enforcement of zero dependencies from BusinessLogic layer to other projects

## Next Steps

- Complete and polish archive extraction pipeline
- Add more unit tests for archive-related features
- Consider performance benchmarks for MIH implementations
- Improve UI for archive browsing and result navigation
- Address TODO items (save/load test, sorting display, nested ignore folders)

## Important Patterns and Preferences

- **Constructor injection** for all services and ViewModels
- **Interface-based** abstractions defined in BusinessLogic/Abstraction/
- **Source-generated JSON serialization** (System.Text.Json source generators)
- **Per-drive parallel processing** for large file sets
- **CRC32 preliminary pass** before full hash computation

## Learnings and Project Insights

- Archive CRC from 7-Zip entry metadata is used for quick duplicate detection within archives
- MIH requires careful parameter tuning (hash bit length, number of tables) for optimal performance
- Nested archives require recursive extraction with depth limit (2 levels)
- JSON polymorphic serialization requires explicit registration in JsonSerializerContext