using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Mobs.Npcs;
using PixelDungeon.Core.Items.Wands;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfMirrorImage : Scroll
{
    private const int NImages = 3;

    public ScrollOfMirrorImage()
    {
        NameValue = "Scroll of Mirror Image";
    }

    protected override void DoRead()
    {
        var respawnPoints = Levels.Level.Neighbours8
            .Select(neighbour => CurUser.Pos + neighbour)
            .Where(p => Actor.FindChar(p) == null && (Levels.Level.Passable[p] || Levels.Level.Avoid[p]))
            .ToList();

        var nImages = NImages;
        while (nImages > 0 && respawnPoints.Count > 0)
        {
            var index = Utils.Random.Index(respawnPoints);

            var mob = new MirrorImage();
            mob.Duplicate(CurUser);
            GameScene.Add(mob);
            WandOfBlink.Appear(mob, respawnPoints[index]);

            respawnPoints.RemoveAt(index);
            nImages--;
        }

        if (nImages < NImages)
        {
            SetKnown();
        }

        Sample.Play(Assets.SndRead);
        Invisibility.Dispel();

        ReadAnimation();
    }

    public override string Desc() =>
        "The incantation on this scroll will create illusionary twins of the reader, which will chase his enemies.";
}