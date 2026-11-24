using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Missiles;

public class IncendiaryDart : MissileWeapon
{
    public IncendiaryDart() : this(1)
    {
    }

    public IncendiaryDart(int number)
    {
        NameValue = "incendiary dart";
        ImageValue = ItemSpriteSheet.IncendiaryDart;

        Str = 12;

        QuantityValue = number;
    }

    public override int Min() => 1;

    public override int Max() => 2;

    protected override void OnThrow(int cell)
    {
        var enemy = Actor.FindChar(cell);
        if (enemy == null || enemy == CurUser)
        {
            if (Levels.Level.Flammable[cell])
            {
                GameScene.Add(Blob.Seed<Fire>(cell, 4));
            }
            else
            {
                base.OnThrow(cell);
            }
        }
        else
        {
            if (!CurUser.Shoot(enemy, this))
            {
                Dungeon.Level.Drop(this, cell).Sprite.Drop();
            }
        }
    }

    public override void Proc(Char attacker, Char defender, int damage)
    {
        Buff.Affect<Burning>(defender).Reignite(defender);
        base.Proc(attacker, defender, damage);
    }

    public override string Desc() =>
        "The spike on each of these darts is designed to pin it to its target " +
        "while the unstable compounds strapped to its length burst into brilliant flames.";

    public override Item Random()
    {
        QuantityValue = Utils.Random.Int(3, 6);
        return this;
    }

    public override int Price() => 10 * QuantityValue;
}