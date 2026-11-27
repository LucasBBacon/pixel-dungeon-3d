using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Plants;

public class Sorrowmoss : Plant
{
    private const string TxtDesc =
        "A Sorrowmoss is a flower (not a moss) with razor-sharp petals, coated with a deadly venom.";

    public Sorrowmoss()
    {
        Image = 2;
        PlantName = "Sorrowmoss";
    }

    public override void Activate(Char ch)
    {
        base.Activate(ch);

        if (ch != null)
        {
            Buff.Affect<Poison>(ch).Set(Poison.DurationFactor(ch) * (4 + Dungeon.Depth / 2));
        }

        if (Dungeon.Visible[Pos])
        {
            GameScene.Splash(Pos, 0xFF8844FF, 3);
        }
    }

    public override string Desc() => TxtDesc;

    public new class Seed : Plant.Seed
    {
        public Seed()
        {
            PlantName = "Sorrowmoss";

            NameValue = "seed of " + PlantName;
            ImageValue = ItemSpriteSheet.SeedSorrowmoss;

            PlantClass = typeof(Sorrowmoss);
            AlchemyClass = typeof(PotionOfToxicGas);
        }

        public override string Desc() => TxtDesc;
    }
}