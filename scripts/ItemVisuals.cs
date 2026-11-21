using System.Collections.Generic;
using Godot;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Client;

public static class ItemVisuals
{
    private readonly record struct Style(Color Color, Vector3 Size);

    private static readonly Vector3 KeySize = new(0.3f, 0.08f, 0.12f);
    private static readonly Vector3 ChestSize = new(0.6f, 0.45f, 0.45f);
    private static readonly Vector3 ArmorSize = new(0.35f, 0.35f, 0.1f);
    private static readonly Vector3 DartSize = new(0.15f, 0.04f, 0.04f);
    private static readonly Vector3 FoodSize = new(0.22f, 0.1f, 0.16f);
    private static readonly Vector3 PotionSize = new(0.18f, 0.3f, 0.18f);
    private static readonly Vector3 ScrollSize = new(0.3f, 0.1f, 0.1f);

    private static readonly Color Parchment = CharView.FromArgb(0xFFE8DCB5);
    private const float RuneBlend = 0.3f;
    
    private static readonly Dictionary<int, Style> Styles = new()
    {
        [ItemSpriteSheet.Gold] = new Style(new Color(1f, 0.84f, 0.1f), new Vector3(0.3f, 0.12f, 0.3f)),
        [ItemSpriteSheet.Dewdrop] = new Style(new Color(0.7f, 0.95f, 1f), new Vector3(0.15f, 0.15f, 0.15f)),
        [ItemSpriteSheet.Vial] = new Style(new Color(0.55f, 0.75f, 1f), new Vector3(0.18f, 0.35f, 0.18f)),
        [ItemSpriteSheet.IronKey] = new Style(new Color(0.55f, 0.55f, 0.6f), KeySize),
        [ItemSpriteSheet.GoldenKey] = new Style(new Color(0.95f, 0.75f, 0.2f), KeySize),
        [ItemSpriteSheet.SkeletonKey] = new Style(new Color(0.9f, 0.88f, 0.8f), KeySize),
        [ItemSpriteSheet.Keyring] = new Style(new Color(0.72f, 0.45f, 0.2f), new Vector3(0.25f, 0.06f, 0.25f)),
        [ItemSpriteSheet.Chest] = new Style(new Color(0.45f, 0.28f, 0.12f), ChestSize),
        [ItemSpriteSheet.LockedChest] = new Style(new Color(0.35f, 0.22f, 0.1f), ChestSize),
        [ItemSpriteSheet.CrystalChest] = new Style(new Color(0.5f, 0.9f, 0.95f, 0.6f), ChestSize),
        [ItemSpriteSheet.Tomb] = new Style(new Color(0.5f, 0.5f, 0.52f), new Vector3(0.8f, 0.3f, 0.5f)),
        [ItemSpriteSheet.Bones] = new Style(new Color(0.92f, 0.9f, 0.82f), new Vector3(0.6f, 0.06f, 0.4f)),
        [ItemSpriteSheet.Weapon] = new Style(new Color(0.35f, 0.35f, 0.38f), new Vector3(0.4f, 0.08f, 0.1f)),
        [ItemSpriteSheet.Armor] = new Style(new Color(0.35f, 0.35f, 0.38f), new Vector3(0.35f, 0.35f, 0.1f)),
        [ItemSpriteSheet.Ring] = new Style(new Color(0.35f, 0.35f, 0.38f), new Vector3(0.15f, 0.05f, 0.15f)),

        // Melee weapons: steel grays by size, the quarterstaff and spear in wood, the knuckleduster in brass.
        [ItemSpriteSheet.Dagger] = new Style(new Color(0.80f, 0.82f, 0.86f), new Vector3(0.25f, 0.05f, 0.07f)),
        [ItemSpriteSheet.Knuckleduster] = new Style(new Color(0.72f, 0.58f, 0.28f), new Vector3(0.18f, 0.08f, 0.12f)),
        [ItemSpriteSheet.Quarterstaff] = new Style(new Color(0.55f, 0.38f, 0.20f), new Vector3(0.6f, 0.06f, 0.06f)),
        [ItemSpriteSheet.Spear] = new Style(new Color(0.62f, 0.50f, 0.34f), new Vector3(0.65f, 0.05f, 0.05f)),
        [ItemSpriteSheet.Mace] = new Style(new Color(0.48f, 0.48f, 0.52f), new Vector3(0.4f, 0.12f, 0.12f)),
        [ItemSpriteSheet.Sword] = new Style(new Color(0.74f, 0.76f, 0.80f), new Vector3(0.45f, 0.06f, 0.09f)),
        [ItemSpriteSheet.LongSword] = new Style(new Color(0.84f, 0.86f, 0.90f), new Vector3(0.55f, 0.06f, 0.09f)),
        [ItemSpriteSheet.BattleAxe] = new Style(new Color(0.55f, 0.55f, 0.58f), new Vector3(0.5f, 0.08f, 0.2f)),
        [ItemSpriteSheet.WarHammer] = new Style(new Color(0.40f, 0.40f, 0.44f), new Vector3(0.5f, 0.14f, 0.14f)),
        [ItemSpriteSheet.Glaive] = new Style(new Color(0.62f, 0.64f, 0.70f), new Vector3(0.65f, 0.06f, 0.12f)),
        [ItemSpriteSheet.ShortSword] = new Style(new Color(0.68f, 0.70f, 0.74f), new Vector3(0.35f, 0.06f, 0.08f)),

        // Missiles
        [ItemSpriteSheet.Dart] = new Style(new Color(0.78f, 0.78f, 0.72f), DartSize),
        [ItemSpriteSheet.CurareDart] = new Style(new Color(0.35f, 0.72f, 0.40f), DartSize),
        [ItemSpriteSheet.Shuriken] = new Style(new Color(0.66f, 0.68f, 0.74f), new Vector3(0.14f, 0.03f, 0.14f)),
        [ItemSpriteSheet.Javelin] = new Style(new Color(0.66f, 0.52f, 0.34f), new Vector3(0.5f, 0.04f, 0.04f)),
        [ItemSpriteSheet.Tomahawk] = new Style(new Color(0.52f, 0.40f, 0.30f), new Vector3(0.2f, 0.05f, 0.15f)),

        // Armour: CharView.UpdateArmor tints the hero toward these colors
        [ItemSpriteSheet.ArmorCloth] = new Style(new Color(0.93f, 0.88f, 0.72f), ArmorSize),
        [ItemSpriteSheet.ArmorLeather] = new Style(new Color(0.50f, 0.30f, 0.12f), ArmorSize),
        [ItemSpriteSheet.ArmorMail] = new Style(new Color(0.72f, 0.74f, 0.78f), ArmorSize),
        [ItemSpriteSheet.ArmorScale] = new Style(new Color(0.80f, 0.60f, 0.20f), ArmorSize),
        [ItemSpriteSheet.ArmorPlate] = new Style(new Color(0.30f, 0.32f, 0.38f), ArmorSize),

        // Food
        // 0xFFCC66, the burst Food.Execute shows while eating
        [ItemSpriteSheet.Ration] = new Style(new Color(1f, 0.8f, 0.4f), FoodSize),
        [ItemSpriteSheet.Pasty] = new Style(new Color(0.85f, 0.58f, 0.25f), FoodSize),
        [ItemSpriteSheet.Meat] = new Style(new Color(0.70f, 0.28f, 0.30f), FoodSize),

        // Potions
        // each flask in the color Potion.SplashColor gives its shatter.
        [ItemSpriteSheet.PotionTurquoise] = Flask(ItemSpriteSheet.PotionTurquoise),
        [ItemSpriteSheet.PotionCrimson] = Flask(ItemSpriteSheet.PotionCrimson),
        [ItemSpriteSheet.PotionAzure] = Flask(ItemSpriteSheet.PotionAzure),
        [ItemSpriteSheet.PotionJade] = Flask(ItemSpriteSheet.PotionJade),
        [ItemSpriteSheet.PotionGolden] = Flask(ItemSpriteSheet.PotionGolden),
        [ItemSpriteSheet.PotionMagenta] = Flask(ItemSpriteSheet.PotionMagenta),
        [ItemSpriteSheet.PotionCharcoal] = Flask(ItemSpriteSheet.PotionCharcoal),
        [ItemSpriteSheet.PotionIvory] = Flask(ItemSpriteSheet.PotionIvory),
        [ItemSpriteSheet.PotionAmber] = Flask(ItemSpriteSheet.PotionAmber),
        [ItemSpriteSheet.PotionBistre] = Flask(ItemSpriteSheet.PotionBistre),
        [ItemSpriteSheet.PotionIndigo] = Flask(ItemSpriteSheet.PotionIndigo),
        [ItemSpriteSheet.PotionSilver] = Flask(ItemSpriteSheet.PotionSilver),

        // Scrolls
        [ItemSpriteSheet.ScrollKaunan] = Rune(0xFFC0392B),
        [ItemSpriteSheet.ScrollSowilo] = Rune(0xFFF1C40F),
        [ItemSpriteSheet.ScrollLaguz] = Rune(0xFF2980B9),
        [ItemSpriteSheet.ScrollYngvi] = Rune(0xFF27AE60),
        [ItemSpriteSheet.ScrollGyfu] = Rune(0xFF8E44AD),
        [ItemSpriteSheet.ScrollRaido] = Rune(0xFFD35400),
        [ItemSpriteSheet.ScrollIsaz] = Rune(0xFF7FB3D5),
        [ItemSpriteSheet.ScrollMannaz] = Rune(0xFF7F8C8D),
        [ItemSpriteSheet.ScrollNaudiz] = Rune(0xFF6E2C00),
        [ItemSpriteSheet.ScrollBerkanan] = Rune(0xFF58D68D),
        [ItemSpriteSheet.ScrollOdal] = Rune(0xFFA04000),
        [ItemSpriteSheet.ScrollTiwaz] = Rune(0xFF1B2631),

        // The catalogue's unknown rows (WndCatalogus)
        [ItemSpriteSheet.Smth] = new Style(CharView.FromArgb(0xFF808080), new Vector3(0.25f, 0.25f, 0.25f)),
    };

    private static Style Flask(int image)
    {
        return new Style(CharView.FromArgb(Potion.SplashColor(image)), PotionSize);
    }

    private static Style Rune(uint argb)
    {
        return new Style(Parchment.Lerp(CharView.FromArgb(argb), RuneBlend), ScrollSize);
    }

    private static readonly Style Unknown = new(new Color(0.8f, 0.3f, 0.8f), new Vector3(0.25f, 0.25f, 0.25f));

    public static Color ColorOf(int image) => Styles.GetValueOrDefault(image, Unknown).Color;

    public static Vector3 SizeOf(int image) => Styles.GetValueOrDefault(image, Unknown).Size;

    public static bool IsDrawn(int image) => image != ItemSpriteSheet.Hidden;
}