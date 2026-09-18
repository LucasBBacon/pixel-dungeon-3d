using Godot;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Client;

public partial class Main : Node3D
{
    public override void _Ready()
    {
        Random.Seed(1);
        GD.Print($"Core Random.Int(100) = {Random.Int(100)}");

        var bundle = new Bundle();
        bundle.Put("depth", 7);
        GD.Print($"Core Bundle round-trip = {bundle.GetInt("depth")}");
    }
}