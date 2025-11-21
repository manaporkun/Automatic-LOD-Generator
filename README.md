# Auto LOD Generator

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/License-GPL%20v3-blue.svg)](LICENSE)

**Automatic Level of Detail (LOD) generator and mesh simplifier for Unity.**

Transform complex 3D meshes into optimized LOD groups with just a few clicks. Perfect for game developers who want to improve performance without manually creating multiple mesh versions.

<img src="/Screenshot.png" width="100%">

## Features

- **One-Click LOD Generation** - Automatically create LOD groups with configurable quality levels
- **Batch Processing** - Process multiple objects at once for efficient workflow
- **6 Built-in Presets** - Pre-configured settings for different platforms and use cases
- **Customizable LOD Levels** - Configure 2-6 LOD levels with individual quality settings
- **Real-time Preview** - See estimated vertex/triangle counts before generation
- **Drag & Drop Support** - Simply drag objects from hierarchy or project window
- **Context Menu Integration** - Right-click on objects for quick access
- **Keyboard Shortcuts** - Speed up your workflow with hotkeys
- **Undo Support** - Full integration with Unity's undo system

## Installation

### Step 1: Install UnityMeshSimplifier (Required Dependency)

This plugin requires [UnityMeshSimplifier](https://github.com/Whinarn/UnityMeshSimplifier/).

1. Open **Window > Package Manager**
2. Click **+ > Add package from git URL**
3. Enter: `https://github.com/Whinarn/UnityMeshSimplifier.git`
4. Click **Add**

### Step 2: Install Auto LOD Generator

**Option A: Import Unity Package**
1. Download `Auto-LOD-Generator.unitypackage` from this repository
2. In Unity, go to **Assets > Import Package > Custom Package**
3. Select the downloaded file and import

**Option B: Clone Repository**
1. Clone this repository into your project's `Assets/Plugins` folder

## Quick Start

### Using the Main Window

1. Open **Tools > Auto LOD Generator > Open Window**
2. Select a GameObject with a mesh in your scene
3. Choose a preset or customize settings
4. Click **Generate LOD Group**

### Using Context Menu (Fastest)

1. Right-click on any mesh object in the Hierarchy
2. Select **Auto LOD > Generate LOD Group**

### Using Keyboard Shortcuts

- **Ctrl+Alt+L** (Windows) / **Cmd+Alt+L** (Mac) - Quick Generate LOD Group

## Presets

| Preset | LOD Levels | Best For |
|--------|------------|----------|
| **Performance** | 3 | Maximum FPS, aggressive simplification |
| **Balanced** | 4 | General use, good quality/performance balance |
| **Quality** | 5 | Visual fidelity, gradual transitions |
| **Mobile (Low-end)** | 2 | Budget mobile devices |
| **Mobile (High-end)** | 3 | Modern mobile devices |
| **VR** | 4 | VR applications, avoids LOD popping |

## Menu Reference

### Tools Menu
- `Tools > Auto LOD Generator > Open Window` - Main interface
- `Tools > Auto LOD Generator > Quick Generate LOD Group` - Generate with Balanced preset
- `Tools > Auto LOD Generator > Quick Simplify (50%)` - Create 50% simplified mesh
- `Tools > Auto LOD Generator > Generate with Preset > ...` - Generate with specific preset

### Context Menu (Right-click in Hierarchy)
- `Auto LOD > Generate LOD Group` - Generate LOD group
- `Auto LOD > Simplify Mesh (50%)` - Create 50% simplified version
- `Auto LOD > Simplify Mesh (25%)` - Create 25% simplified version
- `Auto LOD > Open Generator Window...` - Open main window

## Advanced Configuration

### Custom LOD Settings

In the main window, expand **Advanced Settings** to customize:

- **Quality Factors** - Mesh quality for each LOD level (0.0 - 1.0)
- **Screen Transition Heights** - When each LOD level activates based on screen coverage
- **Culled Level** - Optional level that completely hides the object at distance

### Batch Processing

1. Open the main window and go to the **Batch Process** tab
2. Drag multiple objects or click **Add from Selection**
3. Configure settings
4. Click **Process X Objects**

## API Usage

For programmatic access, use the `LODGeneratorCore` class:

```csharp
using Plugins.AutoLODGenerator.Editor;

// Generate LOD group with default settings
var settings = new LODGeneratorSettings();
settings.ApplyPreset(LODPreset.Balanced);

var result = LODGeneratorCore.GenerateLODGroup(myGameObject, settings);

if (result.Success)
{
    Debug.Log($"Generated LOD group: {result.GeneratedLODGroup.name}");
    Debug.Log($"Original vertices: {result.OriginalVertexCount}");
}

// Simplify a single mesh
var simplifyResult = LODGeneratorCore.GenerateSimplifiedMesh(myGameObject, 0.5f);

// Batch processing
var batchResult = LODGeneratorCore.ProcessBatch(
    gameObjectArray,
    settings,
    (progress, status) => Debug.Log($"{progress:P0} - {status}")
);
```

## Requirements

- Unity 2021.3 LTS or newer
- [UnityMeshSimplifier](https://github.com/Whinarn/UnityMeshSimplifier/) package

## Troubleshooting

**"Object does not have a valid MeshFilter"**
- Ensure the selected object has both `MeshFilter` and `MeshRenderer` components

**LOD transitions are too aggressive/subtle**
- Adjust Screen Transition Heights in Advanced Settings
- Try a different preset

**Simplified mesh looks bad**
- Increase the quality factor
- Some mesh topologies don't simplify well - try a higher quality setting

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

## Credits

- Based on [UnityMeshSimplifier](https://github.com/Whinarn/UnityMeshSimplifier/) by Mattias Edlund
- [Icon Source](https://pixabay.com/vectors/crop-circle-glyph-sacred-geometry-5147211/)

## Video Tutorial

https://youtu.be/9YOd-lwKeXU
