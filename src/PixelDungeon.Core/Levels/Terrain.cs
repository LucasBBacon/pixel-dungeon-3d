namespace PixelDungeon.Core.Levels;

public static class Terrain
{
    public const int Chasm = 0;
    public const int Empty = 1;
    public const int Grass = 2;
    public const int EmptyWell = 3;
    public const int Wall = 4;
    public const int Door = 5;
    public const int OpenDoor = 6;
    public const int Entrance = 7;
    public const int Exit = 8;
    public const int Embers = 9;
    public const int LockedDoor = 10;
    public const int Pedestal = 11;
    public const int WallDeco = 12;
    public const int Barricade = 13;
    public const int EmptySp = 14;
    public const int HighGrass = 15;
    public const int EmptyDeco = 24;
    public const int LockedExit = 25;
    public const int UnlockedExit = 26;
    public const int Sign = 29;
    public const int Well = 34;
    public const int Statue = 35;
    public const int StatueSp = 36;
    public const int Bookshelf = 41;
    public const int Alchemy = 42;
    public const int ChasmFloor = 43;
    public const int ChasmFloorSp = 44;
    public const int ChasmWall = 45;
    public const int ChasmWater = 46;

    public const int SecretDoor = 16;
    public const int ToxicTrap = 17;
    public const int SecretToxicTrap = 18;
    public const int FireTrap = 19;
    public const int SecretFireTrap = 20;
    public const int ParalyticTrap = 21;
    public const int SecretParalyticTrap = 22;
    public const int InactiveTrap = 23;
    public const int PoisonTrap = 27;
    public const int SecretPoisonTrap = 28;
    public const int AlarmTrap = 30;
    public const int SecretAlarmTrap = 31;
    public const int LightningTrap = 32;
    public const int SecretLightningTrap = 33;
    public const int GrippingTrap = 37;
    public const int SecretGrippingTrap = 38;
    public const int SummoningTrap = 39;
    public const int SecretSummoningTrap = 40;

    public const int WaterTiles = 48;
    public const int Water = 63;

    public const int Passable = 0x01;
    public const int LosBlocking = 0x02;
    public const int Flammable = 0x04;
    public const int Secret = 0x08;
    public const int Solid = 0x10;
    public const int Avoid = 0x20;
    public const int Liquid = 0x40;
    public const int Pit = 0x80;
    public const int Unstitchable = 0x100;

    public static readonly int[] Flags = new int[256];

    static Terrain()
    {
        Flags[Chasm] = Avoid | Pit | Unstitchable;
        Flags[Empty] = Passable;
        Flags[Grass] = Passable | Flammable;
        Flags[EmptyWell] = Passable;
        Flags[Water] = Passable | Liquid | Unstitchable;
        Flags[Wall] = LosBlocking | Solid | Unstitchable;
        Flags[Door] = Passable | LosBlocking | Flammable | Solid | Unstitchable;
        Flags[OpenDoor] = Passable | Flammable | Unstitchable;
        Flags[Entrance] = Passable;
        Flags[Exit] = Passable;
        Flags[Embers] = Passable;
        Flags[LockedDoor] = LosBlocking | Solid | Unstitchable;
        Flags[Pedestal] = Passable | Unstitchable;
        Flags[WallDeco] = Flags[Wall];
        Flags[Barricade] = Flammable | Solid | LosBlocking;
        Flags[EmptySp] = Flags[Empty] | Unstitchable;
        Flags[HighGrass] = Passable | LosBlocking | Flammable;
        Flags[EmptyDeco] = Flags[Empty];
        Flags[LockedExit] = Solid;
        Flags[UnlockedExit] = Passable;
        Flags[Sign] = Passable | Flammable;
        Flags[Well] = Avoid;
        Flags[Statue] = Solid;
        Flags[StatueSp] = Flags[Statue] | Unstitchable;
        Flags[Bookshelf] = Flags[Barricade] | Unstitchable;
        Flags[Alchemy] = Passable;

        Flags[ChasmWall] = Flags[Chasm];
        Flags[ChasmFloor] = Flags[Chasm];
        Flags[ChasmFloorSp] = Flags[Chasm];
        Flags[ChasmWater] = Flags[Chasm];

        Flags[SecretDoor] = Flags[Wall] | Secret | Unstitchable;
        Flags[ToxicTrap] = Avoid;
        Flags[SecretToxicTrap] = Flags[Empty] | Secret;
        Flags[FireTrap] = Avoid;
        Flags[SecretFireTrap] = Flags[Empty] | Secret;
        Flags[ParalyticTrap] = Avoid;
        Flags[SecretParalyticTrap] = Flags[Empty] | Secret;
        Flags[PoisonTrap] = Avoid;
        Flags[SecretPoisonTrap] = Flags[Empty] | Secret;
        Flags[AlarmTrap] = Avoid;
        Flags[SecretAlarmTrap] = Flags[Empty] | Secret;
        Flags[LightningTrap] = Avoid;
        Flags[SecretLightningTrap] = Flags[Empty] | Secret;
        Flags[GrippingTrap] = Avoid;
        Flags[SecretGrippingTrap] = Flags[Empty] | Secret;
        Flags[SummoningTrap] = Avoid;
        Flags[SecretSummoningTrap] = Flags[Empty] | Secret;
        Flags[InactiveTrap] = Flags[Empty];

        for (var i = WaterTiles; i < WaterTiles + 16; i++)
        {
            Flags[i] = Flags[Water];
        }
    }

    public static int Discover(int terr)
    {
        return terr switch
        {
            SecretDoor => Door,
            SecretFireTrap => FireTrap,
            SecretParalyticTrap => ParalyticTrap,
            SecretToxicTrap => ToxicTrap,
            SecretPoisonTrap => PoisonTrap,
            SecretAlarmTrap => AlarmTrap,
            SecretLightningTrap => LightningTrap,
            SecretGrippingTrap => GrippingTrap,
            SecretSummoningTrap => SummoningTrap,
            _ => terr
        };
    }
}