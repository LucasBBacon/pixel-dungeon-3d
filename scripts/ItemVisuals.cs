using System.Collections.Generic;
using Godot;
using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Client;

public static class ItemVisuals
{
    private readonly record struct Style(Color Color, Vector3 Size);

    private static readonly Vector3 KeySize = new(0.3f, 0.08f, 0.12f);
    private static readonly Vector3 ChestSize = new(0.6f, 0.45f, 0.45f);

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
    };

    private static readonly Style Unknown = new(new Color(0.8f, 0.3f, 0.8f), new Vector3(0.25f, 0.25f, 0.25f));

    public static Color ColorOf(int image) => Styles.GetValueOrDefault(image, Unknown).Color;

    public static Vector3 SizeOf(int image) => Styles.GetValueOrDefault(image, Unknown).Size;

    public static bool IsDrawn(int image) => image != ItemSpriteSheet.Hidden;
}