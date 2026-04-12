# Changelog

All notable changes to Auto LOD Generator will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.2.1] - 2026-04-12

### Changed
- Enabled automatic release publishing from default-branch merges by auto-tagging `v<package.json version>` when missing

## [2.2.0] - 2025-03-17

### Added
- **Advanced Mesh Simplification Options** - Full integration with UnityMeshSimplifier's advanced features
  - `Enable Smart Link` - Prevents holes in simplified meshes (enabled by default)
  - `Vertex Link Distance` - Configurable distance for vertex linking
  - `Preserve Borders` - Preserves mesh edge boundaries
  - `Preserve UV Seams` - Prevents UV stretching at seams
  - `Preserve UV Foldovers` - Prevents distortion on overlapping UVs
  - Options are configurable per preset (Quality and VR presets enable border/seam preservation)
  - New UI section in Advanced Settings for all simplification options

### Changed
- CI requires `package.json` version to match the first non-`Unreleased` section in `CHANGELOG.md`; Version Bump workflow prepends changelog entries with real newlines and pushes to the repository default branch
- Release workflow now auto-tags the default branch when a new `package.json` version is merged, then publishes GitHub Releases from the matching changelog section
- Updated `LODGeneratorSettings` with new simplification options fields
- Updated `SimplifyMesh()` to use `SimplificationOptions` from settings
- All LOD generation methods now pass settings to the simplifier

## [2.1.3] - 2025-03-17

### Fixed
- **Animation not working on generated LOD Groups** - Skinned mesh animations now work correctly
  - Added `CopyAnimatorComponents()` to copy Animator/Animation to LOD groups
  - Fixed bone reference remapping when skeleton is moved to LOD group (composite objects)
  - Single mesh LOD generation now copies Animator component
  - Simplify Mesh feature now copies Animator for skinned meshes
  - Added `CollectBoneTransforms()` for bone remapping when skeleton hierarchy is moved

## [2.1.2] - 2025-03-17

### Fixed
- **Prefab conversion issue (#2)** - Fixed LOD meshes disappearing when converting LOD Group to prefab
  - Added `HasUnsavedMeshes()` to check for in-memory meshes
  - Added `SaveLODMeshesToAssets()` to retroactively save meshes for existing LOD groups
  - Added context menu item: `Auto LOD > Save LOD Meshes to Assets`
  - Added warning in UI when meshes are not saved as assets
  - Meshes now properly persist when creating prefabs

## [2.1.1] - 2025-03-17

### Fixed
- **Icon path for UPM installations** - Fixed hardcoded icon path that failed to load when package installed via Package Manager
- **Preset storage location** - Moved from plugin folder to user project folder (`Assets/Editor/AutoLODGenerator/Presets`) to survive package updates
- **Legacy preset migration** - Automatically migrates presets from old location to new location
- **Variable scope error** - Fixed CS0136 compilation error in `LoadPreset()` method

### Added
- **Preset storage configuration** - Added UI in Presets tab to configure custom preset folder location
- **Unit tests for Core** - Added `LODGeneratorCoreTests` with 12 additional test cases

## [2.1.0] - 2025-03-17

### Added
- **UPM package support** - Install directly via Unity Package Manager with git URL
- **Assembly Definition** (`Plugins.AutoLODGenerator.Editor.asmdef`) for faster compilation and namespace isolation
- **Editor test scaffolding** with NUnit test assembly and initial settings tests
- **GitHub Actions CI/CD** - Automated package validation on PRs and release creation on tags
- **Version bump workflow** - One-click semantic version bumping via GitHub Actions
- **.editorconfig** for consistent code style enforcement across editors
- **Release badge** and **CI badge** in README

### Changed
- **Improved installation docs** - Added UPM (git URL) as recommended installation method
- **Enhanced .gitattributes** - Proper binary/text handling for Unity asset types
- **All editor tabs are now scrollable** for better usability in small windows

### Fixed
- CS0165 compilation error from unassigned `validationError` variable
- Nested scroll view in advanced settings causing UI shrinking
- Zero-allocation menu validation for better editor performance

## [2.0.0] - 2024-01

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
| 2.1.0 | 2025-03 | UPM support, CI/CD, assembly definition, test scaffolding |
| 2.0.0 | 2024-01 | Major refactor: Modern UI, presets, batch processing |
| 1.0.0 | 2022-04 | Initial release |
