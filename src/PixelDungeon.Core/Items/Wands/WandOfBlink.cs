using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Wands;

public class WandOfBlink
{
    // TODO: WandOfBlink extends Wand
    
    public static void Appear(Char ch, int pos)
    {
        ch.Sprite.InterruptMotion();
        
        ch.Move(pos);
        ch.Sprite.Place(pos);

        if (ch.Invisible == 0)
        {
            ch.Sprite.FadeIn();
        }
        
        ch.Sprite.Burst(0xFFFFFFFF, 3);
        Sample.Play(Assets.SndTeleport);
    }
}