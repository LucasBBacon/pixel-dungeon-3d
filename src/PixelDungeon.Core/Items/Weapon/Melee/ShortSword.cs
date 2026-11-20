using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items.Scrolls;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items.Weapon.Melee;

public class ShortSword : MeleeWeapon
{
    public const string AcReforge = "REFORGE";

    private const string TxtSelectWeapon = "Select a weapon to upgrade";

    private const string TxtReforged = "you reforged the short sword to upgrade your {0}";

    private const string TxtNotBoomerang = "you can't upgrade a boomerang this way";

    private const float TimeToReforge = 2f;

    private bool _equipped;

    private readonly Action<Item> _itemSelector;

    public ShortSword() : base(1, 1f, 1f)
    {
        NameValue = "short sword";
        ImageValue = ItemSpriteSheet.ShortSword;

        Str = 11;

        _itemSelector = OnItemSelected;
    }

    protected override int Max0() => 12;

    public override List<string> Actions(Hero hero)
    {
        var actions = base.Actions(hero);
        if (Level() > 0)
        {
            actions.Add(AcReforge);
        }

        return actions;
    }

    public override void Execute(Hero hero, string action)
    {
        if (action == AcReforge)
        {
            if (hero.Belongings.Weapon == this)
            {
                _equipped = true;
                hero.Belongings.Weapon = null;
            }
            else
            {
                _equipped = false;
                Detach(hero.Belongings.Backpack);
            }

            CurUser = hero;

            GameScene.SelectItem(_itemSelector, BagMode.Weapon, TxtSelectWeapon);
        }
        else
        {
            base.Execute(hero, action);
        }
    }


    public override string Desc() => "It is indeed quite short, just a few inches longer, than a dagger.";

    private void OnItemSelected(Item item)
    {
        if (item != null)
        {
            Sample.Play(Assets.SndEvoke);
            ScrollOfUpgrade.Upgrade(CurUser);
            Evoke(CurUser);

            GLog.W(TxtReforged, item.Name());

            ((MeleeWeapon)item).SafeUpgrade();
            CurUser.SpendAndNext(TimeToReforge);

            // TODO: Badges.validateItemLevelAcquired(item)
        }
        else
        {
            // TODO: a chosen Boomerang logs TxtNotBoomerang

            if (_equipped)
            {
                CurUser.Belongings.Weapon = this;
            }
            else
            {
                Collect(CurUser.Belongings.Backpack);
            }
        }
    }
}