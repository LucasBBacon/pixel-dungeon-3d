using System;
using System.Collections.Generic;
using Godot;
using PixelDungeon.Core;
using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Levels;

namespace PixelDungeon.Client;

public partial class BlobView : Node3D
{
    private const float Height = 0.8f;

    private const int FullAt = 200;

    private static readonly Dictionary<Type, Color> Tints = new()
    {
        [typeof(ToxicGas)] = CharView.FromArgb(0xFF50FF60),
        [typeof(ParalyticGas)] = CharView.FromArgb(0xFFFFFF66),
    };

    private Blob _blob;
    private Color _tint;
    private MultiMesh _cells;

    public static BlobView For(Blob blob)
    {
        var view = new BlobView { Name = blob.GetType().Name, _blob = blob };
        view._tint = Tints.TryGetValue(blob.GetType(), out var tint) ? tint : Colors.White;

        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            VertexColorUseAsAlbedo = true,
        };
        view._cells = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseColors = true,
            Mesh = new BoxMesh { Size = new Vector3(1f, Height, 1f), Material = material },
            InstanceCount = Level.Length,
            VisibleInstanceCount = 0,
        };
        view.AddChild(new MultiMeshInstance3D { Multimesh = view._cells });
        return view;
    }

    public override void _Process(double delta)
    {
        if (Dungeon.Level == null)
        {
            return;
        }

        var cur = _blob.Cur;
        var shown = 0;
        for (var cell = 0; cell < Level.Length; cell++)
        {
            if (cur[cell] > 0 && Dungeon.Visible[cell])
            {
                var position = LevelRenderer.CellToWorld(cell);
                position.Y = Height / 2;
                _cells.SetInstanceTransform(shown, new Transform3D(Basis.Identity, position));
                var alpha = 0.1f + 0.5f * Mathf.Min(cur[cell], FullAt) / FullAt;
                _cells.SetInstanceColor(shown, new Color(_tint.R, _tint.G, _tint.B, alpha));
                shown++;
            }
        }

        _cells.VisibleInstanceCount = shown;
    }
}