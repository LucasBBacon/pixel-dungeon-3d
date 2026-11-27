using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Ui;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Plants;

public class Earthroot : Plant
{
    private const string TxtDesc =
        "When a creature touches an Earthroot, its roots " +
        "create a kind of natural armor around it.";

    public Earthroot()
    {
        Image = 5;
        PlantName = "Earthroot";
    }

    public override void Activate(Char ch)
    {
        base.Activate(ch);

        if (ch != null)
        {
            Buff.Affect<Armor>(ch).LevelValue = ch.HT;
        }

        if (!Dungeon.Visible[Pos]) return;
        
        GameScene.Splash(Pos, 0xFF5D5D55, 8);
        GameScene.Shake(1, 0.4f);
    }

    public override string Desc() => TxtDesc;

    public new class Seed : Plant.Seed
    {
        public Seed()
        {
            PlantName = "Earthroot";

            NameValue = "seed of " + PlantName;
            ImageValue = ItemSpriteSheet.SeedEarthroot;

            PlantClass = typeof(Earthroot);
            AlchemyClass = typeof(PotionOfParalyticGas);
        }

        public override string Desc() => TxtDesc;
    }

    public class Armor : Buff
    {
        private const float Step = 1f;

        private int _pos;

        public int LevelValue;

        public override bool AttachTo(Char target)
        {
            _pos = target.Pos;
            return base.AttachTo(target);
        }

        protected override bool Act()
        {
            if (Target.Pos != _pos)
            {
                Detach();
            }

            Spend(Step);
            return true;
        }

        public int Absorb(int damage)
        {
            if (damage >= LevelValue)
            {
                Detach();
                return damage - LevelValue;
            }

            LevelValue -= damage;
            return 0;
        }

        public void Level(int value)
        {
            if (LevelValue < value)
            {
                LevelValue = value;
            }
        }

        public override int Icon() => BuffIndicator.Armor;

        public override string ToString() => "Herbal armor";

        private const string PosTag = "pos";
        private const string LevelTag = "level";

        public override void RestoreFromBundle(Bundle bundle)
        {
            base.RestoreFromBundle(bundle);
            _pos = bundle.GetInt(PosTag);
            LevelValue = bundle.GetInt(LevelTag);
        }

        public override void StoreInBundle(Bundle bundle)
        {
            base.StoreInBundle(bundle);
            bundle.Put(PosTag, _pos);
            bundle.Put(LevelTag, LevelValue);
        }
    }
}