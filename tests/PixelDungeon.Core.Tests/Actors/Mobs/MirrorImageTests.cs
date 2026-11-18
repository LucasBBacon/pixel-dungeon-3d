using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Actors.Mobs.Npcs;
using PixelDungeon.Core.Items.Armor;
using PixelDungeon.Core.Items.Wands;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Items;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Actors.Mobs;

public class MirrorImageTests : DungeonFixture
{
    private static readonly string[] Corridor =
    [
        "#######",
        "#.....#",
        "#######"
    ];

    private static int C(int x) => TestLevel.At(x, 1);

    private sealed class TestNpc : Npc
    {
        public int Interactions;

        public override void Interact() => Interactions++;

        public void ThrowItemForTest() => ThrowItem();

        public int TargetForTest => Target;

        public int ExpForTest => Exp;
    }

    private sealed class TestImage : MirrorImage
    {
        public Char EnemyForTest
        {
            get => Enemy;
            set => Enemy = value;
        }

        public Char ChooseEnemyForTest() => ChooseEnemy();
    }

    [Fact]
    public void NPC_IsOneHpPassiveHarmlessMob_WorthNoExperience()
    {
        var npc = new TestNpc();

        Assert.Equal(1, npc.HP);
        Assert.Equal(1, npc.HT);
        Assert.Equal(0, npc.ExpForTest);
        Assert.False(npc.Hostile);
        Assert.Same(npc.PassiveState, npc.State);
    }

    [Fact]
    public void NpcBeckon_DoesNothing()
    {
        var npc = new TestNpc();

        npc.Beckon(C(3));

        Assert.Same(npc.PassiveState, npc.State);
        Assert.Equal(-1, npc.TargetForTest);
    }

    [Fact]
    public void NpcThrowItem_PushesTheHeapOnItsCellOntoAnOpenNeighbour()
    {
        LoadLevel(Corridor);
        var npc = new TestNpc { Pos = C(3) };
        var item = new TestItem();
        Dungeon.Level.Drop(item, C(3));

        npc.ThrowItemForTest();

        Assert.False(Dungeon.Level.Heaps.ContainsKey(C(3)));
        var cell = Assert.Single(Dungeon.Level.Heaps.Keys);
        Assert.Contains(cell, new[] { C(2), C(4) });
        Assert.Same(item, Dungeon.Level.Heaps[cell].Peek());
        Assert.Equal(new[] { C(3) }, ((FakeHeapView)Dungeon.Level.Heaps[cell].Sprite).DropsFrom);
    }

    // ---- MirrorImage ----

    [Fact]
    public void MirrorImage_HuntsFromTheStart_AsAOneHpNpcImmuneToToxicGas()
    {
        var image = new MirrorImage();

        Assert.Equal("mirror image", image.Name);
        Assert.Same(image.HuntingState, image.State);
        Assert.Equal(1, image.HT);
        Assert.False(image.Hostile);
        Assert.Contains(typeof(ToxicGas), image.Immunities());
        Assert.Equal("This illusion bears a close resemblance to you, but it's paler and twitches a little.",
            image.Description());
    }

    [Fact]
    public void Duplicate_CopiesTheHeroTierAccuracyAndOneDamageRoll()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(C(1));
        hero.Belongings.Armor = new ClothArmor();
        var image = new MirrorImage();

        Random.Seed(3);
        var damage = hero.DamageRoll();
        Random.Seed(3);
        image.Duplicate(hero);

        Assert.Equal(1, image.Tier);
        Assert.Equal(hero.AttackSkill(hero), image.AttackSkill(null));
        Assert.Equal(damage, image.DamageRoll());
        Assert.Equal(damage, image.DamageRoll()); // kept, not rolled again
    }

    [Fact]
    public void AttackProc_AfterItsOneHit_DestroysTheImage()
    {
        LoadLevel(Corridor);
        PlaceHero(C(1));
        var image = new MirrorImage { Pos = C(3), Sprite = new FakeCharView() };
        GameScene.Add(image);
        var rat = new Rat { Pos = C(4), Sprite = new FakeCharView() };

        var dealt = image.AttackProc(rat, 3);

        Assert.Equal(3, dealt);
        Assert.False(image.IsAlive());
        Assert.DoesNotContain(image, Actor.All());
        Assert.DoesNotContain(image, Dungeon.Level.Mobs);
        Assert.True(((FakeCharView)image.Sprite).Died);
    }

    [Fact]
    public void ChooseEnemy_WithoutALivingEnemy_PicksAHostileMobInView_NotAnotherNpc()
    {
        LoadLevel(Corridor);
        PlaceHero(C(1));
        var image = new TestImage { Pos = C(2), Sprite = new FakeCharView() };
        GameScene.Add(image);
        var rat = new Rat { Pos = C(5), Sprite = new FakeCharView() };
        GameScene.Add(rat);
        GameScene.Add(new TestNpc { Pos = C(4), Sprite = new FakeCharView() });
        Dungeon.Level.UpdateFieldOfView(image);

        Assert.Same(rat, image.ChooseEnemyForTest());
    }

    [Fact]
    public void ChooseEnemy_WithNoHostileMobInView_IsNull()
    {
        LoadLevel(Corridor);
        PlaceHero(C(1));
        var image = new TestImage { Pos = C(2), Sprite = new FakeCharView() };
        GameScene.Add(image);
        Dungeon.Level.UpdateFieldOfView(image);

        Assert.Null(image.ChooseEnemyForTest());
    }

    [Fact]
    public void ChooseEnemy_KeepsALivingEnemy_EvenOutOfView()
    {
        LoadLevel(Corridor);
        PlaceHero(C(1));
        var image = new TestImage { Pos = C(2) };
        var rat = new Rat { Pos = C(5) };
        image.EnemyForTest = rat;
        Array.Fill(Level.FieldOfView, false);

        Assert.Same(rat, image.ChooseEnemyForTest());
    }

    [Fact]
    public void Interact_SwapsPlacesWithTheHero_WhoSpendsOneMove()
    {
        LoadLevel(Corridor);
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(C(1));
        var heroView = new FakeCharView();
        hero.Sprite = heroView;
        var image = new MirrorImage { Pos = C(2), Sprite = new FakeCharView() };
        GameScene.Add(image);

        image.Interact();

        Assert.Equal(C(1), image.Pos);
        Assert.Equal(C(2), hero.Pos);
        Assert.Equal(new[] { (C(1), C(2)) }, heroView.Moves);
        Assert.Equal(new[] { (C(2), C(1)) },
            ((FakeCharView)image.Sprite).Moves); // both cells in sight, so the image's sprite walks
        Assert.Equal(1f, hero.TimeLeft); // 1 / Speed()
        Assert.False(hero.Ready);
    }

    [Fact]
    public void HeroHandle_OnAnAdjacentImage_SwapsWithIt()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(C(1));
        hero.Sprite = new FakeCharView();
        var image = new MirrorImage { Pos = C(2), Sprite = new FakeCharView() };
        GameScene.Add(image);
        Dungeon.Observe();

        hero.Handle(image.Pos);

        Assert.Equal(C(2), hero.Pos);
        Assert.Equal(C(1), image.Pos);
        Assert.Null(hero.CurAction); // ActInteract readies the hero before the image makes it busy
        Assert.False(hero.Ready);
    }

    [Fact]
    public void HeroHandle_OnAnImageFurtherAway_StepsTowardIt()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(C(1));
        hero.Sprite = new FakeCharView();
        var image = new MirrorImage { Pos = C(4), Sprite = new FakeCharView() };
        GameScene.Add(image);
        Dungeon.Observe();

        hero.Handle(image.Pos);

        Assert.Equal(C(2), hero.Pos);
        Assert.IsType<HeroAction.Interact>(hero.CurAction);
        Assert.Equal(C(4), image.Pos);
    }

    [Fact]
    public void MirrorImage_RoundTripsItsTierAttackAndDamage()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(C(1));
        hero.Belongings.Armor = new ClothArmor();
        var image = new MirrorImage();
        image.Duplicate(hero);
        var bundle = new Bundle();

        image.StoreInBundle(bundle);
        var restored = new MirrorImage();
        restored.RestoreFromBundle(bundle);

        Assert.Equal(1, bundle.GetInt("tier"));
        Assert.Equal(image.Tier, restored.Tier);
        Assert.Equal(image.AttackSkill(null), restored.AttackSkill(null));
        Assert.Equal(image.DamageRoll(), restored.DamageRoll());
    }

    // ---- WandOfBlink.Appear ----

    [Fact]
    public void WandOfBlinkAppear_MovesAndPlacesTheChar_FadingItWithALightBurst()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(C(1));
        var view = new FakeCharView { IsMovingFlag = true };
        hero.Sprite = view;

        WandOfBlink.Appear(hero, C(4));

        Assert.Equal(C(4), hero.Pos);
        Assert.Equal(1, view.Interrupts);
        Assert.Equal(new[] { C(4) }, view.Placed);
        Assert.Equal(1, view.FadeIns);
        Assert.Contains((0xFFFFFFFFu, 3), view.BurstLog);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndTeleport);
    }

    [Fact]
    public void WandOfBlinkAppear_AnInvisibleChar_DoesNotFadeIn()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(C(1));
        var view = new FakeCharView();
        hero.Sprite = view;
        Buff.Affect<Invisibility>(hero, Invisibility.Duration);

        WandOfBlink.Appear(hero, C(4));

        Assert.Equal(0, view.FadeIns);
        Assert.Equal(C(4), hero.Pos);
    }
}