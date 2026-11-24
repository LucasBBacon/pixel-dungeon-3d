using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items;

public class Bomb : Item
{
    public Bomb()
    {
        NameValue = "bomb";
        ImageValue = ItemSpriteSheet.Bomb;
        DefaultAction = AcThrow;
        Stackable = true;
    }

    protected override void OnThrow(int cell)
    {
        if (Levels.Level.Pit[cell])
        {
            base.OnThrow(cell);
        }
        else
        {
            Sample.Play(Assets.SndBlast);

            if (Dungeon.Visible[cell])
            {
                GameScene.Splash(cell, 0xFFEE7722, 30);
            }

            var terrainAffected = false;
            foreach (var neighbour in Levels.Level.Neighbours9)
            {
                var c = cell + neighbour;
                if (c is >= 0 and < Levels.Level.Length)
                {
                    if (Dungeon.Visible[c])
                    {
                        GameScene.Splash(c, 0xFF222222, 4);
                    }

                    if (Levels.Level.Flammable[c])
                    {
                        Dungeon.Level.Destroy(c);
                        GameScene.UpdateMap(c);
                        terrainAffected = true;
                    }

                    var ch = Actor.FindChar(c);
                    if (ch != null)
                    {
                        var dmg = Utils.Random.Int(1 + Dungeon.Depth, 10 + Dungeon.Depth * 2) -
                                  Utils.Random.Int(ch.Dr());
                        if (dmg > 0)
                        {
                            ch.Damage(dmg, this);
                            if (ch.IsAlive())
                            {
                                Buff.Prolong<Paralysis>(ch, 2);
                            }
                            else if (ch == Dungeon.Hero)
                            {
                                Dungeon.Fail(TextUtils.Format(ResultDescriptions.Bomb, Dungeon.Depth));
                                GLog.N("You killed yourself with a bomb...");
                            }
                        }
                    }
                }

                if (terrainAffected)
                {
                    Dungeon.Observe();
                }
            }
        }
    }

    public override bool IsUpgradable() => false;

    public override bool IsIdentified() => true;

    public override int Price() => 10 * QuantityValue;

    public override Item Random()
    {
        QuantityValue = Utils.Random.IntRange(1, 3);
        return this;
    }

    public override string Info() =>
        "This is a relatively small bomb, filled with black powder. Conveniently, its fuse is lit automatically when the bomb is thrown.";
}