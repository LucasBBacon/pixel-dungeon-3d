using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests;

public abstract class DungeonFixture : IDisposable
{
    protected readonly RecordingGameView View;

    protected DungeonFixture()
    {
        Dungeon.Reset();
        PathFinder.SetMapSize(Level.Width, Level.Height);
        Random.Seed(1);
        View = new RecordingGameView();
        GameScene.Instance = View;
        Dungeon.Hero = new Hero(); // unscheduled until PlaceHero
    }

    public void Dispose()
    {
        GameScene.Instance = NullGameView.Instance;
        Dungeon.Reset();
    }

    protected TestLevel LoadLevel(params string[] rows)
    {
        var level = TestLevel.FromRows(rows);
        Dungeon.Level = level;
        return level;
    }

    protected Hero PlaceHero(int cell)
    {
        var hero = Dungeon.Hero;
        hero.Pos = cell;
        Actor.Add(hero);
        Actor.OccupyCell(hero);
        Dungeon.Observe();
        return hero;
    }
}