using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Plants;

public class Rotberry : Plant
{
    private const string TxtDesc = "Berries of this shrub taste like sweet, sweet death.";


    public Rotberry()
    {
        Image = 7;
        PlantName = "Rotberry";
    }

    public override void Activate(Char ch)
    {
        base.Activate(ch);
        
        GameScene.Add(Blob.Seed<ToxicGas>(Pos, 100));

        Dungeon.Level.Drop(new Seed(), Pos).Sprite.Drop();

        if (ch != null)
        {
            Buff.Prolong<Roots>(ch, Roots.Tick * 3);
        }
    }

    public override string Desc() => TxtDesc;

    public new class Seed : Plant.Seed
    {
        public Seed()
        {
            PlantName = "Rotberry";

            NameValue = "seed of " + PlantName;
            ImageValue = ItemSpriteSheet.SeedRotberry;

            PlantClass = typeof(Rotberry);
            AlchemyClass = typeof(PotionOfStrength);
        }

        public override bool Collect(Bag container)
        {
            if (base.Collect(container))
            {
                // TODO: loadGame sets Dungeon.Level to null before the hero, so load never roars
                if (Dungeon.Level != null)
                {
                    foreach (var mob in Dungeon.Level.Mobs.ToArray())
                    {
                        mob.Beckon(Dungeon.Hero.Pos);
                    }
                    
                    GLog.W("The seed emits a roar that echoes throughout the dungeon!");
                    GameScene.Splash(Dungeon.Hero.Pos, 0xFFFFFFFF, 3);
                    Sample.Play(Assets.SndChallenge);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public override string Desc() => TxtDesc;
    }
}