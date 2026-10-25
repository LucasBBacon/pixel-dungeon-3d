using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items;

public class Gold : Item
{
    private const string TxtCollect = "Collect gold coins to spend them later in a shop.";
    private const string TxtInfo = "A pile of {0} gold coins. " + TxtCollect;
    private const string TxtInfo1 = "One gold coin. " + TxtCollect;
    private const string TxtValue = "{0:+#;-#;+0}";

    public Gold() : this(1)
    {
    }

    public Gold(int value)
    {
        NameValue = "gold";
        ImageValue = ItemSpriteSheet.Gold;
        Stackable = true;

        QuantityValue = value;
    }

    public override List<string> Actions(Hero hero) => [];

    public override bool DoPickUp(Hero hero)
    {
        Dungeon.Gold += QuantityValue;
        Statistics.GoldCollected += QuantityValue;
        // TODO: Badges.ValidateGoldCollected()

        GameScene.PickUp(this);
        hero.Sprite.ShowStatus(StatusColor.Neutral, TextUtils.Format(TxtValue, QuantityValue));
        hero.SpendAndNext(TimeToPickUp);

        Sample.Play(Assets.SndGold, Utils.Random.Float(0.9f, 1.1f));

        return true;
    }

    public override bool IsUpgradable() => false;

    public override bool IsIdentified() => true;

    public override string Info() =>
        QuantityValue switch
        {
            0 => TxtCollect,
            1 => TxtInfo1,
            _ => TextUtils.Format(TxtInfo, QuantityValue)
        };

    public override Item Random()
    {
        QuantityValue = Utils.Random.Int(20 + Dungeon.Depth * 10, 40 + Dungeon.Depth * 20);
        return this;
    }

    private const string TagValue = "value";

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);
        QuantityValue = bundle.GetInt(TagValue);
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);
        bundle.Put(TagValue, QuantityValue);
    }
}