using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Potions;

public class PotionOfMindVision : Potion
{
    public PotionOfMindVision()
    {
        NameValue = "Potion of Mind Vision";
    }

    protected override void Apply(Hero hero)
    {
        SetKnown();
        Buff.Affect<MindVision>(hero, MindVision.Duration);
        Dungeon.Observe();

        GLog.I(Dungeon.Level.Mobs.Count > 0
            ? "You can somehow feel the presence of other creatures' minds!"
            : "You can somehow tell that you are alone on this level at the moment.");
    }

    public override string Desc() =>
        "After drinking this, your mind will become attuned to the psychic signature " +
        "of distant creatures, enabling you to sense biological presences through walls. " +
        "Also this potion will permit you to see through nearby walls and doors.";

    public override int Price() => IsKnown() ? 35 * QuantityValue : base.Price();
}