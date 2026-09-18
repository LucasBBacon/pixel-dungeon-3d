# Pixel Dungeon 3D

A faithful recreation of Classic Pixel Dungeon (Oleg Dolya / watabou) in Godot 4.7 with C#, rendered in 3D with orthographic camera, and some placeholder assets for now.
The goal is that it *plays* exactly like the original, the code does not need to match line for line.

The original Java source is vendored under `classic-pixel-dungeon/` for reference and is never compiled. This port is a derivative work and is licensed under the GPLv3 (see `LICENSE`).

## Layout

| Path                                      | What it is                                                           |
|-------------------------------------------|----------------------------------------------------------------------|
| `src/PixelDungeon.Core/`                  | The game logic, ported one C# file per Java file. No Godot reference. |
| `tests/PixelDungeon.Core.Tests/`          | xUnit Tests for the core.                                            |
| `scripts/`, `scenes/`                     | The Godot view layer (namespace `PixelDungeon.Client`                |
| `PixelDungeon.csproj`, `PixelDungeon.sln` | Godot project and solution.                                          |

### Building and Running

Open the project with the **.NET** build of Godot. The standard build cannot compile C#.

From the shell:

```bash
dotnet build PixelDungeon.sln
dotnet test PixelDungeon.sln
```

## Porting conventions

- Java package -> C# namespace (`com.watabou.utils` -> `PixelDungeon.Core.Utils`).
- Line 1 of every ported file contains its source path. GPL header follows.
- Method names in PascalCase with same overloads. Public fields stay public fields. Formulas, constants, and branch order are copied verbatim. Deliberate deviations get a comment.
- `Callback` -> `System.Action`, `SparseArray<T>` -> `Dictionary<int, T>`
- The core keeps the original's static state (`Dungeon`, `Actor`, `Level`), so tests run serially and reset through one entry point.
- `Random` is the ported, not `System.Random`, it's a global using alias in each project makes the bare name resolve to it.

## Status

Sub-proj 0 (Foundation) is complete: solution, utility ports (`Random`, `Bundle`, `PathFinder`, `Graph`, `Rect`, `Point`, `PointF`, `GameMath`), and a main scene that proves Godot can call the core.

TODO: turn engine and walkable Sewers level.