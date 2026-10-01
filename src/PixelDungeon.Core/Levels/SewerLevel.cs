namespace PixelDungeon.Core.Levels;

public class SewerLevel : RegularLevel
{
    public SewerLevel()
    {
        Color1 = 0x48763c;
        Color2 = 0x59994a;
    }

    protected override bool[] WaterMap() => Patch.Generate(Feeling == LevelFeeling.Water ? 0.60f : 0.45f, 5);

    protected override bool[] GrassMap() => Patch.Generate(Feeling == LevelFeeling.Grass ? 0.60f : 0.45f, 4);

    protected override void Decorate()
    {
        for (var i = 0; i < Width; i++)
        {
            if (Map[i] == Terrain.Wall &&
                Map[i + Width] == Terrain.Water &&
                Random.Int(4) == 0)
            {
                Map[i] = Terrain.WallDeco;
            }
        }

        for (var i = Width; i < Length - Width; i++)
        {
            if (Map[i] == Terrain.Wall &&
                Map[i - Width] == Terrain.Wall &&
                Map[i + Width] == Terrain.Water &&
                Random.Int(2) == 0)
            {
                Map[i] = Terrain.WallDeco;
            }
        }

        for (var i = Width + 1; i < Length - Width - 1; i++)
        {
            if (Map[i] != Terrain.Empty) continue;

            var count = (Map[i + 1] == Terrain.Wall ? 1 : 0) +
                        (Map[i - 1] == Terrain.Wall ? 1 : 0) +
                        (Map[i + Width] == Terrain.Wall ? 1 : 0) +
                        (Map[i - Width] == Terrain.Wall ? 1 : 0);

            if (Random.Int(16) < count * count)
            {
                Map[i] = Terrain.EmptyDeco;
            }
        }

        while (true)
        {
            var pos = RoomEntrance.RandomCell();

            if (pos == Entrance) continue;

            Map[pos] = Terrain.Sign;
            break;
        }
    }

    public override string TileName(int tile)
    {
        return tile switch
        {
            Terrain.Water => "Murky water",
            _ => base.TileName(tile)
        };
    }

    public override string TileDesc(int tile)
    {
        return tile switch
        {
            Terrain.EmptyDeco => "Wet yellowish moss covers the floor.",
            Terrain.Bookshelf => "the bookshelf is packed with cheap useless books. Might it burn?",
            _ => base.TileDesc(tile)
        };
    }
}