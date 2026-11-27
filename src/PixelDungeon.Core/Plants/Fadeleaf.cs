using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Items.Scrolls;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Plants;

public class Fadeleaf : Plant
{
    private const string TxtDesc =
        "Touching a Fadeleaf will teleport any creature " +
        "to a random place on the current level.";

    public Fadeleaf()
    {
        Image = 6;
        PlantName = "Fadeleaf";
    }

    public override void Activate(Char ch)
    {
        base.Activate(ch);

        switch (ch)
        {
            case Hero hero:
                ScrollOfTeleportation.TeleportHero(hero);
                hero.CurAction = null;
                break;
            case Mob:
            {
                var count = 10;
                int newPos;
                do
                {
                    newPos = Dungeon.Level.RandomRespawnCell();
                    if (count-- <= 0)
                    {
                        break;
                    }
                } while (newPos == -1);

                if (newPos != -1)
                {
                    ch.Pos = newPos;
                    ch.Sprite.Place(ch.Pos);
                    ch.Sprite.Visible = Dungeon.Visible[Pos];
                }

                break;
            }
        }

        if (Dungeon.Visible[Pos])
        {
            GameScene.Splash(Pos, 0xFFFFFFFF, 3);
        }
    }

    public override string Desc() => TxtDesc;

    public new class Seed : Plant.Seed
    {
        public Seed()
        {
            PlantName = "Fadeleaf";

            NameValue = "seed of " + PlantName;
            ImageValue = ItemSpriteSheet.SeedFadeleaf;

            PlantClass = typeof(Fadeleaf);
            AlchemyClass = typeof(PotionOfMindVision);
        }

        public override string Desc() => TxtDesc;
    }
}