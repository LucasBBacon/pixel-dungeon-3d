using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfMagicMapping : Scroll
{
    private const string TxtLayout = "You are now aware of the level layout.";
    
    public ScrollOfMagicMapping()
    {
        NameValue = "Scroll of Magic Mapping";
    }

    protected override void DoRead()
    {
        const int length = Levels.Level.Length;
        var map = Dungeon.Level.Map;
        var mapped = Dungeon.Level.Mapped;
        var discoverable = Levels.Level.Discoverable;

        var noticed = false;

        for (var i = 0; i < length; i++)
        {
            var terr = map[i];

            if (!discoverable[i]) continue;
            
            mapped[i] = true;
            
            if ((Terrain.Flags[terr] & Terrain.Secret) == 0) continue;
            
            Levels.Level.Set(i, Terrain.Discover(terr));
            GameScene.UpdateMap(i);

            if (!Dungeon.Visible[i]) continue;
            
            GameScene.DiscoverTile(i, terr);
            Discover(i);

            noticed = true;
        }
        
        Dungeon.Observe();
        
        GLog.I(TxtLayout);
        if (noticed)
        {
            Sample.Play(Assets.SndSecret);
        }
        
        CurUser.Sprite.Burst(0xFFFFFFFF, 1);
        Sample.Play(Assets.SndRead);
        Invisibility.Dispel();
        
        SetKnown();
        
        ReadAnimation();
    }

    public override string Desc() =>
        "When this scroll is read, an image of crystal clarity will be etched into your memory, " +
        "alerting you to the precise layout of the level and revealing all hidden secrets. " +
        "The locations of items and creatures will remain unknown.";

    public override int Price() => IsKnown() ? 25 * QuantityValue : base.Price();

    public static void Discover(int cell) => GameScene.Effect(EffectKind.Discover, cell);
}