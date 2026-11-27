using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Plants;

public class Plant : IBundlable
{
    public string PlantName;

    public int Image;
    public int Pos;
    public IPlantView Sprite = NullPlantView.Instance;

    public virtual void Activate(Char ch)
    {
        // TODO: Warden gains Barkskin at level HT / 3

        Wither();
    }

    public void Wither()
    {
        Dungeon.Level.Uproot(Pos);

        Sprite.Kill();
        if (Dungeon.Visible[Pos])
        {
            GameScene.Splash(Pos, 0xFF448822, 6);
        }

        // TODO: Warden, Random.Int(5) == 0 drops a random seed and Random.Int(5) == 0 drops a dewdrop
    }

    private const string TagPos = "pos";

    public virtual void RestoreFromBundle(Bundle bundle)
    {
        Pos = bundle.GetInt(TagPos);
    }

    public virtual void StoreInBundle(Bundle bundle)
    {
        bundle.Put(TagPos, Pos);
    }

    public virtual string Desc()
    {
        return null;
    }

    public class Seed : Item
    {
        public const string AcPlant = "PLANT";

        private const string TxtInfo = "Throw this seed to the place where you want to grow {0}.\n\n{1}";

        private const float TimeToPlant = 1f;

        public Seed()
        {
            Stackable = true;
            DefaultAction = AcThrow;
        }

        public Type PlantClass;
        public string PlantName;

        public Type AlchemyClass;

        public override List<string> Actions(Hero hero)
        {
            var actions = base.Actions(hero);
            actions.Add(AcPlant);
            return actions;
        }

        protected override void OnThrow(int cell)
        {
            if (Dungeon.Level.Map[cell] == Terrain.Alchemy || Levels.Level.Pit[cell])
            {
                base.OnThrow(cell);
            }
            else
            {
                Dungeon.Level.PlantSeed(this, cell);
            }
        }

        public override void Execute(Hero hero, string action)
        {
            if (action == AcPlant)
            {
                hero.Spend(TimeToPlant);
                hero.Busy();
                ((Seed)Detach(hero.Belongings.Backpack)).OnThrow(hero.Pos);

                hero.Sprite.Operate(hero.Pos);
            }
            else
            {
                base.Execute(hero, action);
            }
        }

        public virtual Plant Couch(int pos)
        {
            if (Dungeon.Visible[pos])
            {
                Sample.Play(Assets.SndPlant);
            }

            var plant = (Plant)Activator.CreateInstance(PlantClass);
            plant.Pos = pos;
            return plant;
        }

        public override bool IsUpgradable() => false;

        public override bool IsIdentified() => true;

        public override int Price() => 10 * QuantityValue;

        public override string Info() =>
            TextUtils.Format(TxtInfo, TextUtils.Indefinite(PlantName), Desc());
    }
}