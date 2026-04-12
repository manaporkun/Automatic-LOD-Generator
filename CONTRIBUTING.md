# Contributing to Auto LOD Generator

Thank you for your interest in contributing to Auto LOD Generator! This document provides guidelines and information for contributors.

## Table of Contents
- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How to Contribute](#how-to-contribute)
- [Development Setup](#development-setup)
- [Coding Standards](#coding-standards)
- [Submitting Changes](#submitting-changes)
- [Releasing](#releasing)
- [Reporting Bugs](#reporting-bugs)
- [Requesting Features](#requesting-features)

## Code of Conduct

Please be respectful and constructive in all interactions. We're all here to make this project better.

## Getting Started

1. Fork the repository
2. Clone your fork locally
3. Set up the development environment (see below)
4. Create a new branch for your changes
5. Make your changes
6. Submit a pull request

## How to Contribute

### Ways to Contribute
- **Report bugs** - Found a bug? Open an issue!
- **Suggest features** - Have an idea? We'd love to hear it!
- **Fix bugs** - Check the issues labeled `bug` and `good first issue`
- **Add features** - Check the issues labeled `enhancement`
- **Improve documentation** - Help make our docs better
- **Write tests** - Help improve code coverage

## Development Setup

### Prerequisites
- Unity 2021.3 LTS or newer
- Git
- A code editor (Visual Studio, Rider, or VS Code recommended)

### Setup Steps

1. **Clone the repository:**
   ```bash
   git clone https://github.com/manaporkun/Automatic-LOD-Generator.git
   ```

2. **Create or open a Unity project:**
   - Open Unity Hub
   - Create a new project or open an existing one (Unity 2021.3+)

3. **Add the package locally:**
   - Open **Window > Package Manager**
   - Click **+ > Add package from disk**
   - Select the `package.json` at the repository root

4. **Install dependencies:**
   - In Package Manager, click **+ > Add package from git URL**
   - Enter: `https://github.com/Whinarn/UnityMeshSimplifier.git`

5. **Import demo scene (optional):**
   - In Package Manager, find **Auto LOD Generator**
   - Expand **Samples** and click **Import** next to "Demo Scene"

6. **Verify setup:**
   - Open Tools > Auto LOD Generator > Open Window
   - The window should open without errors

## Coding Standards

### C# Style Guide

- Use C# 8.0+ features where appropriate
- Follow Microsoft's [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Add XML documentation comments for public APIs

### Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Classes | PascalCase | `LODGeneratorCore` |
| Methods | PascalCase | `GenerateLODGroup` |
| Properties | PascalCase | `VertexCount` |
| Private fields | _camelCase | `_selectedObjects` |
| Local variables | camelCase | `meshFilter` |
| Constants | PascalCase | `MaxLODLevels` |

### Code Organization

```csharp
// 1. Using statements
using UnityEngine;

// 2. Namespace
namespace Plugins.AutoLODGenerator.Editor
{
    // 3. XML documentation
    /// <summary>
    /// Brief description of the class.
    /// </summary>
    public class MyClass
    {
        // 4. Constants
        private const int MaxValue = 100;

        // 5. Private fields
        private int _myField;

        // 6. Properties
        public int MyProperty { get; set; }

        // 7. Unity lifecycle methods
        private void OnEnable() { }

        // 8. Public methods
        public void PublicMethod() { }

        // 9. Private methods
        private void PrivateMethod() { }
    }
}
```

### Best Practices

- Keep methods short and focused (< 30 lines ideally)
- Use early returns to reduce nesting
- Handle errors gracefully with try-catch where appropriate
- Register undo operations for editor changes
- Validate input parameters

## Submitting Changes

### Branch Naming
- `feature/description` - For new features
- `fix/description` - For bug fixes
- `docs/description` - For documentation changes
- `refactor/description` - For code refactoring

### Commit Messages
Write clear, concise commit messages:
```
Short summary (50 chars or less)

More detailed explanation if needed. Wrap at 72 characters.
Explain the problem this commit solves and why.

- Bullet points are okay
- Use present tense ("Add feature" not "Added feature")
```

### Pull Request Process

1. Update the CHANGELOG.md with your changes
2. Ensure all tests pass
3. Update documentation if needed
4. Request review from maintainers
5. Address any feedback
6. Squash commits if requested

## Releasing

Releases are automated with GitHub Actions once a new version lands on the default branch.

### Maintainer flow

1. **Changelog and version** — The version in `package.json` must match the **first** non-`[Unreleased]` section heading in `CHANGELOG.md` (for example `## [2.2.0]`). CI enforces this on every push and pull request.
2. **Merge to default branch** — Once that version change is merged, the **Release** workflow checks whether `v<package.json version>` exists. If it does not, the workflow creates and pushes the tag automatically.
3. **GitHub Release** — The `v*` tag trigger in the same workflow validates tag/package consistency, extracts that version’s changelog section, and publishes the GitHub Release.
4. **Optional helper** — You can still run **Version Bump** (Actions → *Version Bump* → Run workflow) to bump patch/minor/major, scaffold changelog headings, and push the bump commit/tag in one action.

If `CHANGELOG.md` is edited by hand, update `package.json` to the same version so CI stays green. Automation requires `GITHUB_TOKEN` to have `contents: write` so tags and releases can be created.

## Reporting Bugs

When reporting bugs, please include:

1. **Unity version** you're using
2. **Steps to reproduce** the bug
3. **Expected behavior** vs **actual behavior**
4. **Error messages** from the console
5. **Screenshots** if applicable
6. **Mesh information** (vertex count, type, etc.)

Use the bug report template when creating an issue.

## Requesting Features

When requesting features:

1. Check if the feature already exists or is planned
2. Describe the problem the feature would solve
3. Propose a solution
4. Consider alternative approaches
5. Explain your use case

Use the feature request template when creating an issue.

## Questions?

Feel free to open an issue with the `question` label if you have any questions about contributing.

Thank you for contributing to Auto LOD Generator!
