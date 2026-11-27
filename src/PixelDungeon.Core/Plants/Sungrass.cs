using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Ui;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Plants;

public class Sungrass : Plant
{
    private const string TxtDesc =
        "Sungrass is renowned for its sap's healing properties.";

    public Sungrass()
    {
        Image = 4;
        PlantName = "Sungrass";
    }

    public override void Activate(Char ch)
    {
        base.Activate(ch);

        if (ch != null)
        {
            Buff.Affect<Health>(ch);
        }

        if (Dungeon.Visible[Pos])
        {
            GameScene.Splash(Pos, 0xFFFFFF66, 3);
        }
    }

    public override string Desc() => TxtDesc;

    public new class Seed : Plant.Seed
    {
        public Seed()
        {
            PlantName = "Sungrass";

            NameValue = "seed of " + PlantName;
            ImageValue = ItemSpriteSheet.SeedSungrass;

            PlantClass = typeof(Sungrass);
            AlchemyClass = typeof(PotionOfHealing);
        }

        public override string Desc() => TxtDesc;
    }

    public class Health : Buff
    {
        private const float Step = 5f;

        private int _pos;

        public override bool AttachTo(Char target)
        {
            _pos = target.Pos;
            return base.AttachTo(target);
        }

        protected override bool Act()
        {
            if (Target.Pos != _pos || Target.HP >= Target.HT)
            {
                Detach();
            }
            else
            {
                Target.HP = Math.Min(Target.HT, Target.HP + Target.HT / 10);
                Target.Sprite.Burst(0xFF33FF33, 1);
            }

            Spend(Step);
            return true;
        }

        public override int Icon() => BuffIndicator.Healing;

        public override string ToString() => "Herbal healing";

        private const string PosTag = "pos";

        public override void RestoreFromBundle(Bundle bundle)
        {
            base.RestoreFromBundle(bundle);
            _pos = bundle.GetInt(PosTag);
        }

        public override void StoreInBundle(Bundle bundle)
        {
            base.StoreInBundle(bundle);
            bundle.Put(PosTag, _pos);
        }
    }
}