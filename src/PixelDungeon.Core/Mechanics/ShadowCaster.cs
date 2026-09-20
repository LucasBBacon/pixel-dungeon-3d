using PixelDungeon.Core.Levels;

namespace PixelDungeon.Core.Mechanics;

public static class ShadowCaster
{
    private const int MaxDistance = 8;

    private const int Width = Level.Width;
    private const int Height = Level.Height;

    private static int _distance;
    private static int[] _limits;

    private static bool[] _losBlocking;
    private static bool[] _fieldOfView;

    private static readonly int[][] Rounding;

    static ShadowCaster()
    {
        Rounding = new int[MaxDistance + 1][];
        for (int i = 1; i <= MaxDistance; i++)
        {
            Rounding[i] = new int[i + 1];
            for (int j = 1; j <= i; j++)
            {
                // java Math.round rounds half u, values are positive so AwayFromZero matches
                Rounding[i][j] = (int)Math.Min(
                    j,
                    Math.Round(
                        i * Math.Cos(Math.Asin(j / (i + 0.5))),
                        MidpointRounding.AwayFromZero
                    )
                );
            }
        }
    }

    private static readonly Obstacles Obs = new();

    public static void CastShadow(int x, int y, bool[] fieldOfView, int distance)
    {
        _losBlocking = Level.LosBlocking;
        _distance = distance;
        _limits = Rounding[distance];

        _fieldOfView = fieldOfView;
        Array.Fill(fieldOfView, false);
        fieldOfView[y * Width + x] = true;

        ScanSector(x, y, +1, +1, 0, 0);
        ScanSector(x, y, -1, +1, 0, 0);
        ScanSector(x, y, +1, -1, 0, 0);
        ScanSector(x, y, -1, -1, 0, 0);
        ScanSector(x, y, 0, 0, +1, +1);
        ScanSector(x, y, 0, 0, -1, +1);
        ScanSector(x, y, 0, 0, +1, -1);
        ScanSector(x, y, 0, 0, -1, -1);
    }

    private static void ScanSector(int cx, int cy, int m1, int m2, int m3, int m4)
    {
        Obs.Reset();

        for (var p = 1; p <= _distance; p++)
        {
            var dq2 = 0.5f / p;

            var pp = _limits[p];
            for (var q = 0; q <= pp; q++)
            {
                var x = cx + q * m1 + p * m3;
                var y = cy + p * m2 + q * m4;

                if (y is < 0 or >= Height || x is < 0 or >= Width) continue;

                var a0 = (float)q / p;
                var a1 = a0 - dq2;
                var a2 = a0 + dq2;

                var pos = y * Width + x;

                if (Obs.IsBlocked(a0) && Obs.IsBlocked(a1) && Obs.IsBlocked(a2))
                {
                    // do nothing
                }
                else
                {
                    _fieldOfView[pos] = true;
                }

                if (_losBlocking[pos])
                {
                    Obs.Add(a1, a2);
                }
            }

            Obs.NextRow();
        }
    }


    private sealed class Obstacles
    {
        private const int Size = (MaxDistance + 1) * (MaxDistance + 1) / 2;
        private static readonly float[] A1 = new float[Size];
        private static readonly float[] A2 = new float[Size];

        private int _length;
        private int _limit;

        public void Reset()
        {
            _length = 0;
            _limit = 0;
        }

        public void Add(float o1, float o2)
        {
            if (_length > _limit && o1 <= A2[_length - 1])
            {
                // merging several blocking cells
                A2[_length - 1] = o2;
            }
            else
            {
                A1[_length] = o1;
                A2[_length++] = o2;
            }
        }

        public bool IsBlocked(float a)
        {
            for (var i = 0; i < _limit; i++)
            {
                if (a >= A1[i] && a <= A2[i])
                {
                    return true;
                }
            }

            return false;
        }

        public void NextRow()
        {
            _limit = _length;
        }
    }
}