using PixelDungeon.Core.Levels;

namespace PixelDungeon.Core.Tests.Levels;

public sealed class TestLevel : Level
{
    private string[] _rows;

    public static int At(int x, int y) => x + y * Width;

    public static TestLevel FromRows(params string[] rows)
    {
        var level = new TestLevel();
        level.Load(rows);
        return level;
    }

    public static TestLevel Create(params string[] rows)
    {
        var level = new TestLevel { _rows = rows };
        ((Level)level).Create();
        return level;
    }

    private void Load(params string[] rows)
    {
        Map = new int[Length];
        Array.Fill(Map, Terrain.Wall);
        Visited = new bool[Length];
        Mapped = new bool[Length];
        WriteRows(rows);
        BuildFlagMaps();
        CleanWalls();
    }

    public int SpawnCount => ItemsToSpawn.Count;

    private void WriteRows(string[] rows)
    {
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                var code = CodeFor(rows[y][x]);
                Map[At(x, y)] = code;
                switch (code)
                {
                    case Terrain.Entrance:
                        Entrance = At(x, y);
                        break;
                    case Terrain.Exit:
                        Exit = At(x, y);
                        break;
                }
            }
        }
    }

    public static int CodeFor(char c) =>
        c switch
        {
            '#' => Terrain.Wall,
            '.' => Terrain.Empty,
            '+' => Terrain.Door,
            '/' => Terrain.OpenDoor,
            'S' => Terrain.SecretDoor,
            '<' => Terrain.Entrance,
            '>' => Terrain.Exit,
            '~' => Terrain.Water,
            '"' => Terrain.HighGrass,
            ',' => Terrain.Grass,
            'x' => Terrain.Chasm,
            '^' => Terrain.SecretToxicTrap,
            'T' => Terrain.ToxicTrap,
            'L' => Terrain.LockedDoor,
            '!' => Terrain.Sign,
            'A' => Terrain.Alchemy,
            'W' => Terrain.Well,
            'P' => Terrain.EmptySp,
            _ => throw new ArgumentException($"No terrain for '{c}'")
        };

    protected override bool Build()
    {
        if (_rows != null)
        {
            WriteRows(_rows);
        }

        return true;
    }

    protected override void Decorate()
    {
    }

    protected override void CreateMobs()
    {
    }

    protected override void CreateItems()
    {
    }
}