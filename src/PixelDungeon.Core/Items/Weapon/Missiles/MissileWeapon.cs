using System.Text;
using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items.Weapon.Missiles;

public abstract class MissileWeapon : Weapon
{
    private const string TxtMissiles = "Missile weapon";
    private const string TxtYes = "Yes, I know what I'm doing";
    private const string TxtNo = "No, I changed my mind";
    private const string TxtRUSure = "Do you really want to equip it as a melee weapon?";

    protected MissileWeapon()
    {
        Stackable = true;
        LevelKnown = true;
        DefaultAction = AcThrow;
    }

    public override List<string> Actions(Hero hero)
    {
        var actions = base.Actions(hero);
        if (hero.HeroClass != HeroClass.Huntress && hero.HeroClass != HeroClass.Rogue)
        {
            actions.Remove(AcEquip);
            actions.Remove(AcUnequip);
        }

        return actions;
    }

    protected override void OnThrow(int cell)
    {
        var enemy = Actor.FindChar(cell);
        if (enemy == null || enemy == CurUser)
        {
            base.OnThrow(cell);
        }
        else
        {
            if (!CurUser.Shoot(enemy, this))
            {
                Miss(cell);
            }
        }
    }


    protected virtual void Miss(int cell)
    {
        base.OnThrow(cell);
    }

    public override void Proc(Char attacker, Char defender, int damage)
    {
        base.Proc(attacker, defender, damage);

        var hero = (Hero)attacker;
        if (hero.RangedWeapon == null && Stackable)
        {
            if (QuantityValue == 1)
            {
                DoUnequip(hero, false, false);
            }
            else
            {
                Detach(null);
            }
        }
    }

    public override bool DoEquip(Hero hero)
    {
        GameScene.Show(
            new WindowRequest(TxtMissiles,
                TxtRUSure,
                new[]
                {
                    TxtYes,
                    TxtNo
                },
                index =>
                {
                    if (index == 0)
                    {
                        base.DoEquip(hero);
                    }
                }));
        return false;
    }

    public override Item Random() => this;

    public override bool IsUpgradable() => false;

    public override bool IsIdentified() => true;

    public override string Info()
    {
        var info = new StringBuilder(Desc());

        var min = Min();
        var max = Max();
        info.Append("\n\nAverage damage of this weapon equals to " + (min + (max - min) / 2) + " points per hit. ");

        if (Dungeon.Hero.Belongings.Backpack.Items.Contains(this))
        {
            if (Str > Dungeon.Hero.Str())
            {
                info.Append(
                    "Because of your inadequate strength the accuracy and speed " +
                    "of your attack with this " + NameValue + " is decreased.");
            }

            if (Str < Dungeon.Hero.Str())
            {
                info.Append(
                    "Because of your excess strength the damage " +
                    "of your attack with this " + NameValue + " is increased.");
            }
        }

        if (IsEquipped(Dungeon.Hero))
        {
            info.Append("\n\nYou hold the " + NameValue + " at the ready.");
        }

        return info.ToString();
    }
}