using System.Collections.Generic;
using Godot;
using PixelDungeon.Core.Levels;

namespace PixelDungeon.Client;

public enum VisualKind
{
    None,
    Wall,
    Floor,
    FloorSpecial,
    FloorDeco,
    Embers,
    Grass,
    HighGrass,
    Water,
    Door,
    OpenDoor,
    LockedDoor,
    StairsUp,
    StairsDown,
    Solid,
    Bookshelf,
    Trap,
    Sign,
    Well,
}

public static class TerrainVisuals
{
    public static VisualKind KindOf(int terrain)
    {
        if (terrain >= Terrain.WaterTiles)
        {
            return VisualKind.Water;
        }

        return terrain switch
        {
            Terrain.Wall or Terrain.WallDeco or Terrain.SecretDoor or Terrain.LockedExit => VisualKind.Wall,
            Terrain.Empty or Terrain.InactiveTrap or Terrain.Pedestal or Terrain.EmptyWell or Terrain.SecretToxicTrap
                or Terrain.SecretFireTrap or Terrain.SecretParalyticTrap or Terrain.SecretPoisonTrap
                or Terrain.SecretAlarmTrap or Terrain.SecretLightningTrap or Terrain.SecretGrippingTrap
                or Terrain.SecretSummoningTrap => VisualKind.Floor,
            Terrain.EmptySp => VisualKind.FloorSpecial,
            Terrain.EmptyDeco => VisualKind.FloorDeco,
            Terrain.Embers => VisualKind.Embers,
            Terrain.Grass => VisualKind.Grass,
            Terrain.HighGrass => VisualKind.HighGrass,
            Terrain.Water => VisualKind.Water,
            Terrain.Chasm or Terrain.ChasmFloor or Terrain.ChasmFloorSp or Terrain.ChasmWall or Terrain.ChasmWater =>
                VisualKind.None,
            Terrain.Door => VisualKind.Door,
            Terrain.OpenDoor => VisualKind.OpenDoor,
            Terrain.LockedDoor => VisualKind.LockedDoor,
            Terrain.Entrance => VisualKind.StairsUp,
            Terrain.Exit or Terrain.UnlockedExit => VisualKind.StairsDown,
            Terrain.Statue or Terrain.StatueSp or Terrain.Barricade => VisualKind.Solid,
            Terrain.Bookshelf => VisualKind.Bookshelf,
            Terrain.ToxicTrap or Terrain.FireTrap or Terrain.ParalyticTrap or Terrain.PoisonTrap or Terrain.AlarmTrap
                or Terrain.LightningTrap or Terrain.GrippingTrap or Terrain.SummoningTrap => VisualKind.Trap,
            Terrain.Sign => VisualKind.Sign,
            Terrain.Well or Terrain.Alchemy => VisualKind.Well,
            _ => VisualKind.Floor
        };
    }

    private sealed record Style(Mesh Mesh, Color Color, float Y);

    private static readonly Dictionary<VisualKind, Style> Styles = new();
    private static readonly Dictionary<VisualKind, StandardMaterial3D> Lit = new();
    private static readonly Dictionary<VisualKind, StandardMaterial3D> Dim = new();

    public static readonly StandardMaterial3D Flash = Unshaded(Colors.White);

    public static StandardMaterial3D Unshaded(Color color) => new()
        { AlbedoColor = color, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };

    private static Style StyleOf(VisualKind kind)
    {
        if (!Styles.TryGetValue(kind, out var style))
        {
            style = Create(kind);
            Styles[kind] = style;
        }

        return style;
    }

    private static Style Create(VisualKind kind)
    {
        return kind switch
        {
            VisualKind.Wall => new Style(
                new BoxMesh { Size = Vector3.One },
                new Color(0.55f, 0.55f, 0.62f),
                0.5f
            ),
            VisualKind.Floor => new Style(
                new PlaneMesh { Size = Vector2.One },
                new Color(0.30f, 0.30f, 0.33f),
                0f
            ),
            VisualKind.FloorSpecial => new Style(
                new PlaneMesh { Size = Vector2.One },
                new Color(0.38f, 0.35f, 0.30f),
                0f
            ),
            VisualKind.FloorDeco => new Style(
                new PlaneMesh { Size = Vector2.One },
                new Color(0.32f, 0.36f, 0.28f),
                0f
            ),
            VisualKind.Embers => new Style(
                new PlaneMesh { Size = Vector2.One },
                new Color(0.22f, 0.16f, 0.12f),
                0f
            ),
            VisualKind.Grass => new Style(
                new PlaneMesh { Size = Vector2.One },
                new Color(0.20f, 0.50f, 0.20f),
                0f),
            VisualKind.HighGrass => new Style(
                new BoxMesh { Size = new Vector3(1f, 0.5f, 1f) },
                new Color(0.15f, 0.40f, 0.15f),
                0.25f
            ),
            VisualKind.Water => new Style(
                new PlaneMesh { Size = Vector2.One },
                new Color(0.15f, 0.30f, 0.60f),
                0f
            ),
            VisualKind.Door => new Style(
                new BoxMesh { Size = new Vector3(0.7f, 1f, 0.7f) },
                new Color(0.50f, 0.32f, 0.15f),
                0.5f
            ),
            VisualKind.OpenDoor => new Style(
                new BoxMesh { Size = new Vector3(0.8f, 0.1f, 0.8f) },
                new Color(0.50f, 0.32f, 0.15f),
                0.05f
            ),
            VisualKind.LockedDoor => new Style(
                new BoxMesh { Size = new Vector3(0.7f, 1f, 0.7f) },
                new Color(0.25f, 0.15f, 0.08f),
                0.5f
            ),
            VisualKind.StairsUp => new Style(
                new PlaneMesh { Size = Vector2.One },
                new Color(0.92f, 0.92f, 0.85f),
                0f
            ),
            VisualKind.StairsDown => new Style(
                new PlaneMesh { Size = Vector2.One },
                new Color(0.05f, 0.05f, 0.06f),
                0f
            ),
            VisualKind.Solid => new Style(
                new BoxMesh { Size = new Vector3(0.8f, 0.8f, 0.8f) },
                new Color(0.45f, 0.45f, 0.45f),
                0.4f
            ),
            VisualKind.Bookshelf => new Style(
                new BoxMesh { Size = new Vector3(0.9f, 0.9f, 0.9f) },
                new Color(0.40f, 0.25f, 0.12f),
                0.45f
            ),
            VisualKind.Trap => new Style(
                new PlaneMesh { Size = Vector2.One },
                new Color(0.60f, 0.15f, 0.15f),
                0f
            ),
            VisualKind.Sign => new Style(
                new BoxMesh { Size = new Vector3(0.2f, 0.7f, 0.2f) },
                new Color(0.60f, 0.50f, 0.30f),
                0.35f
            ),
            VisualKind.Well => new Style(
                new CylinderMesh { TopRadius = 0.35f, BottomRadius = 0.35f, Height = 0.4f },
                new Color(0.40f, 0.40f, 0.55f),
                0.2f
            ),
            _ => null
        };
    }

    public static Mesh MeshFor(VisualKind kind) => StyleOf(kind)?.Mesh;

    public static float YOf(VisualKind kind) => StyleOf(kind)?.Y ?? 0f;

    public static StandardMaterial3D MaterialFor(VisualKind kind, bool lit)
    {
        var table = lit ? Lit : Dim;
        if (!table.TryGetValue(kind, out var material))
        {
            var c = StyleOf(kind).Color;
            material = Unshaded(lit ? c : new Color(c.R * 0.35f, c.G * 0.35f, c.B * 0.35f));
            table[kind] = material;
        }

        return material;
    }
}