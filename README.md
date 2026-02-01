# jj-mediate

A port of [git-mediate](https://github.com/Peaker/git-mediate/) to C# for use with [Jujutsu VCS](https://docs.jj-vcs.dev/).

## What is it?

`jj-mediate` helps you resolve merge conflicts intelligently. When you have a 3-way merge conflict (showing base, side A, and side B), it can:

- **Auto-resolve trivial conflicts** where only one side changed
- **Reduce complex conflicts** by removing matching lines from start/end
- **Handle special cases** like indentation differences, line endings, and tabs

## How it works

Given a diff3-style conflict:
```
<<<<<<< Side A
Modified by A
||||||| Base
Original
======= Side B  
Modified by B
>>>>>>>
```

The algorithm detects:
- If A == Base → use B (only B changed)
- If B == Base → use A (only A changed)  
- If A == B → use either (both made same change)

For complex conflicts, it finds matching prefix/suffix lines and removes them to simplify the conflict.

## Installation

### As a .NET Global Tool (Recommended)

```bash
# Install from NuGet
dotnet tool install -g jj-mediate

# Update to latest version
dotnet tool update -g jj-mediate

# Uninstall
dotnet tool uninstall -g jj-mediate
```

### From Source

```bash
dotnet build -c Release
# Binary will be in jj-mediate/bin/Release/net10.0/
```

## Development

### Quick Start

The repository includes PowerShell scripts for common development tasks:

```bash
# Format code
./format.ps1

# Check code formatting (lint)
./lint.ps1

# Build project
./build.ps1

# Run tests
./test.ps1

# Run full CI pipeline locally (lint + build + test)
./ci.ps1
```

**Script Options:**
- `build.ps1 -Configuration Debug` - Build in Debug mode
- `test.ps1 -Verbosity detailed` - Run tests with detailed output

### Versioning

This project uses [Nerdbank.GitVersioning (nbgv)](https://github.com/dotnet/Nerdbank.GitVersioning) for automatic version management based on git history.

```bash
# Get current version
nbgv get-version

# Create a new version tag
git tag v1.0.0
git push origin v1.0.0

# Prepare next version
nbgv prepare-release
```

Version numbers are automatically calculated from:
- `version.json` base version
- Git commit height
- Git commit hash for pre-release versions

### Testing

Run unit tests:

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run in release mode
dotnet test --configuration Release
```

### Linting

This project uses [CSharpier](https://csharpier.com/) for code formatting.

```bash
# Check formatting
dotnet csharpier check .

# Format code
dotnet csharpier format .
```

### GitHub Actions

The project uses a unified CI/CD workflow (`.github/workflows/ci-cd.yml`) with multiple jobs:

**On every push/PR:**
- **build-and-test** - Builds and runs all tests
- **lint** - Checks code formatting with CSharpier

**On release:**
- **publish-nuget** - Publishes the tool to NuGet.org (requires `NUGET_API_KEY` secret)
- **publish-binaries** - Creates cross-platform binaries (win-x64, linux-x64, osx-x64, osx-arm64)
- **create-release** - Attaches binary archives to the GitHub Release

All jobs use nbgv for automatic version management.

### Dependabot

Dependabot is configured to automatically check for updates to:
- NuGet packages (weekly)
- GitHub Actions versions (weekly)

## Usage

```bash
# Resolve conflicts in current jj repository
jj-mediate

# Resolve specific file
jj-mediate path/to/conflicted-file.cs
```

## Options

- `--trivial` - Only resolve trivial conflicts (one side changed)
- `--reduce` - Reduce conflicts by removing common prefix/suffix
- `--indentation` - Handle indentation-only differences
- `--added-lines` - Detect lines added by both sides
- `--line-endings` - Normalize line endings (CRLF/LF)
- `--untabify <width>` - Expand tabs to spaces
- `--split-markers` - Split conflicts on `~~~~~~~` separators
- `--show-diffs` - Show diffs from base version
- `-e, --editor` - Open editor on remaining conflicts

## Example

```bash
$ jj status
...
file.cs (conflicted)
...

$ jj-mediate
file.cs: Successfully resolved 3 conflicts (failed to resolve 1 conflict)

$ jj status
...
file.cs (conflicted)  # Still has 1 unresolved conflict
...
```

## Differences from git-mediate

- Uses `jj` commands instead of `git` 
- Focused on conflict resolution (core algorithm)
- No delete/modify conflict handling (Jujutsu handles this differently)
- Native C# / .NET instead of Haskell

## License

GPL-2.0 (same as original git-mediate)

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; version 2 of the License only.

See the [LICENSE](LICENSE) file for the complete license text.

## Credits

Based on [git-mediate](https://github.com/Peaker/git-mediate/) by Eyal Lotem.

