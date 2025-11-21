# Changelog

All notable changes to Auto LOD Generator will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2024-01-XX

### Added
- **New unified editor window** with modern tabbed UI (LOD Group, Simplify Mesh, Batch Process)
- **6 built-in presets**: Performance, Balanced, Quality, Mobile (Low-end), Mobile (High-end), VR
- **Batch processing** - Process multiple objects at once
- **Drag & drop support** from hierarchy and project window
- **Context menu integration** - Right-click objects in Hierarchy for quick access
- **Keyboard shortcuts** - Ctrl+Alt+L for quick LOD generation
- **Real-time preview** of estimated vertex/triangle counts before generation
- **Configurable LOD levels** (2-6 levels) with custom quality factors
- **Screen transition height settings** for fine-tuned LOD switching
- **Optional culled level** for complete object culling at distance
- **Full undo support** for all operations
- **SkinnedMeshRenderer support** for animated characters
- **Save meshes to Assets** - Export simplified meshes as asset files
- **Custom preset saving/loading** - Save and reuse your own configurations
- **Comprehensive statistics** display showing reduction percentages
- **Assembly Definition file** for faster compilation
- **GitHub issue templates** for bug reports and feature requests
- **Pull request template** for contributors
- **CONTRIBUTING.md** with development guidelines

### Changed
- **Reorganized codebase** into clean, maintainable architecture
- **Moved menu location** from `AutoLOD/` to `Tools/Auto LOD Generator/`
- **Improved error handling** with user-friendly messages
- **Enhanced README** with comprehensive documentation and API examples

### Removed
- Old duplicate UI windows (SimplifiedPopUp, LODGroupWindow)
- Hardcoded quality factors and LOD level counts

### Fixed
- Inconsistent namespace usage across files
- Missing validation for objects without MeshRenderer

## [1.0.0] - 2022-04-19

### Added
- Initial release
- Basic LOD group generation with 4 levels
- Simple mesh simplification
- Basic editor UI with file browser
- Integration with UnityMeshSimplifier

---

## Version History Summary

| Version | Date | Highlights |
|---------|------|------------|
| 2.0.0 | 2024-01 | Major refactor: Modern UI, presets, batch processing |
| 1.0.0 | 2022-04 | Initial release |
