using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Potions;

public class PotionOfHealing : Potion
{
    public PotionOfHealing()
    {
        NameValue = "Potion of Healing";
    }

    protected override void Apply(Hero hero)
    {
        SetKnown();
        Heal(Dungeon.Hero);
        GLog.P("Your wounds heal completely.");
    }

    public static void Heal(Hero hero)
    {
        hero.HP = hero.HT;
        Buff.Detach<Poison>(hero);
        Buff.Detach<Cripple>(hero);
        // TODO: Buff.detach(hero, Weakness.class)
        Buff.Detach<Bleeding>(hero);

        hero.Sprite.Burst(0xFF33FF33u, 4);
    }

    public override string Desc() =>
        "An elixir that will instantly return you to full health and cure poison.";

    public override int Price() => IsKnown() ? 30 * QuantityValue : base.Price();
}