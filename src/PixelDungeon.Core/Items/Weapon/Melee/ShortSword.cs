using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Melee;

public class ShortSword : MeleeWeapon
{
    public const string AcReforge = "REFORGE";

    private const string TxtSelectWeapon = "Select a weapon to upgrade";

    private const string TxtReforged = "you reforged the short sword to upgrade your {0}";

    private const string TxtNotBoomerang = "you can't upgrade a boomerang this way";

    private const float TimeToReforge = 2f;

    // TODO: the private equipped flag records whether the sword was wielded when a reforge began

    public ShortSword() : base(1, 1f, 1f)
    {
        NameValue = "short sword";
        ImageValue = ItemSpriteSheet.ShortSword;

        Str = 11;
    }

    protected override int Max0() => 12;

    // TODO: actions() adds AC_REFORGE when level() > 0

    // TODO: execute(AC_REFORGE) takes the sword out of hand or pack, sets curUser and calls GameScene.selectItem(itemSelector, WndBag.Mode.WEAPON, TXT_SELECT_WEAPON)

    public override string Desc() => "It is indeed quite short, just a few inches longer, than a dagger.";

    // TODO: itemSelector plays SND_EVOKE, calls ScrollOfUpgrade.upgrade and evoke, logs TXT_REFORGED, safeUpgrade()s the chosen weapon and spends TIME_TO_REFORGE; a Boomerang logs TXT_NOT_BOOMERANG, and a cancel returns the sword to hand or pack
    // TODO: the reforge's Badges.validateItemLevelAquired(item)
}