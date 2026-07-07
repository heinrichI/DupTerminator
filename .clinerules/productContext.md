# Product Context

## Why This Project Exists

DupTerminator was created because existing duplicate file finders (like DupKiller) had limitations:
- Did not support all needed comparison methods
- Were outdated and slow
- Lacked archive inspection capabilities
- Had poor performance with large file collections

The project evolved from a simple MD5-based duplicate finder into a comprehensive tool supporting multiple search modes and comparison algorithms.

## Problems It Solves

1. **Disk Space Management**: Identify and remove duplicate files cluttering hard drives
2. **Photo Organization**: Find visually similar images (not just exact copies) using perceptual hashing
3. **Archive Cleanup**: Detect duplicates inside ZIP, RAR, 7z and 50+ other archive formats
4. **Document Management**: Compare PDF files by content to find duplicates
5. **Large File Collections**: Efficiently process thousands of files across multiple drives

## How It Should Work

- **User selects search mode** based on the type of duplicates they want to find
- **Configure search locations** (folders, drives, archives)
- **Run search** with progress feedback and cancellation support
- **Review results** in a sortable, filterable grouped list
- **Take action** — delete, move, rename, or export duplicates

## User Experience Goals

- Fast and responsive even with large file sets (100k+ files)
- Clear visual grouping of duplicate file sets
- Ability to select/unselect files intelligently (by folder, date, name)
- Safe operations with recycle bin support and undo capabilities
- Progress reporting and cancellation during long searches
- Save/load search results for later review