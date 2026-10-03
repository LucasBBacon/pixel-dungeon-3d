using Godot;
using PixelDungeon.Core;
using PixelDungeon.Core.Levels;

namespace PixelDungeon.Client;

// One MeshInstance3D per cell at (x, y-of-kind, z). Replaces DungeonTilemap and FogOfWar.
public partial class LevelRenderer : Node3D
{
    private readonly MeshInstance3D[] _nodes = new MeshInstance3D[Level.Length];
    private readonly VisualKind[] _kinds = new VisualKind[Level.Length];
    private readonly bool[] _lit = new bool[Level.Length];
    private readonly bool[] _shown = new bool[Level.Length];

    public static Vector3 CellToWorld(int cell)
    {
        return new Vector3(cell % Level.Width, 0f, cell / Level.Width);
    }

    public static int WorldToCell(Vector3 world)
    {
        var x = Mathf.RoundToInt(world.X);
        var y = Mathf.RoundToInt(world.Z);
        if (x < 0 || x >= Level.Width || y < 0 || y >= Level.Height)
        {
            return -1;
        }

        return x + y * Level.Width;
    }

    public void Rebuild(Level level)
    {
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        for (var i = 0; i < Level.Length; i++)
        {
            _nodes[i] = null;
            _kinds[i] = VisualKind.None;
            _lit[i] = false;
            _shown[i] = false;
        }

        for (var i = 0; i < Level.Length; i++)
        {
            BuildCell(i, level.Map[i]);
        }
    }

    private void BuildCell(int cell, int terrain)
    {
        var kind = TerrainVisuals.KindOf(terrain);
        if (_kinds != null) _kinds[cell] = kind;

        MeshInstance3D node = _nodes[cell];
        if (kind == VisualKind.None)
        {
            if (node != null)
            {
                RemoveChild(node);
                node.QueueFree();
                _nodes[cell] = null;
            }

            return;
        }

        if (node == null)
        {
            node = new MeshInstance3D { Name = $"Cell{cell}" };
            AddChild(node);
            _nodes[cell] = node;
        }

        node.Mesh = TerrainVisuals.MeshFor(kind);
        var pos = CellToWorld(cell);
        pos.Y = TerrainVisuals.YOf(kind);
        node.Position = pos;
        ApplyState(cell);
    }

    private void ApplyState(int cell)
    {
        var node = _nodes[cell];
        if (node == null)
        {
            return;
        }

        node.Visible = _shown[cell];
        node.MaterialOverride = TerrainVisuals.MaterialFor(_kinds[cell], _lit[cell]);
    }

    public void UpdateMap(int cell)
    {
        BuildCell(cell, Dungeon.Level.Map[cell]);
    }

    public void UpdateMap()
    {
        for (var i = 0; i < Level.Length; i++)
        {
            BuildCell(i, Dungeon.Level.Map[i]);
        }
    }

    // FogOfWar.updateVisibility: visible = lit, visited or mapped = dimmed, otherwise hidden.
    public void UpdateVisibility(bool[] visible, bool[] visited, bool[] mapped)
    {
        for (var i = 0; i < Level.Length; i++)
        {
            var lit = visible[i];
            var shown = lit || visited[i] || mapped[i];
            if (lit != _lit[i] || shown != _shown[i])
            {
                _lit[i] = lit;
                _shown[i] = shown;
                ApplyState(i);
            }
        }
    }

    // The CheckedCell effect: a short white flash on one cell.
    public void Flash(int cell)
    {
        var node = _nodes[cell];
        if (node == null)
        {
            return;
        }

        node.MaterialOverride = TerrainVisuals.Flash;
        GetTree().CreateTimer(0.2).Timeout += () =>
        {
            if (IsInstanceValid(node) && _nodes[cell] == node)
            {
                ApplyState(cell);
            }
        };
    }
}