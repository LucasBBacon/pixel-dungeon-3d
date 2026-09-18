namespace PixelDungeon.Core.Utils;

public static class PathFinder
{
    public static int[] Distance;

    private static bool[] _goals;
    private static int[] _queue;

    private static int _size = 0;

    private static int[] _dir;

    public static void SetMapSize(int width, int height)
    {
        var size = width * height;
        
        // just like java, everything including _dir is rebuilt only when the cell count changes,
        // two shapes of equal area would share neighbor offsets. Game is always 32x32.
        if (_size == size) return;

        _size = size;

        Distance = new int[size];
        _goals = new bool[size];
        _queue = new int[size];

        _dir = [-1, +1, -width, +width, -width - 1, -width + 1, +width - 1, +width + 1];
    }

    public static List<int> Find(int from, int to, bool[] passable)
    {
        if (!BuildDistanceMap(from, to, passable))
        {
            return null;
        }

        var result = new List<int>();
        var s = from;

        // from the starting position moving downwards, until reach the ending point
        do
        {
            var minD = Distance[s];
            var mins = s;

            foreach (var t in _dir)
            {
                var n = s + t;
                var thisD = Distance[n];

                if (thisD >= minD) continue;

                minD = thisD;
                mins = n;
            }

            s = mins;
            result.Add(s);
        } while (s != to);

        return result;
    }

    public static int GetStep(int from, int to, bool[] passable)
    {
        if (!BuildDistanceMap(from, to, passable))
        {
            return -1;
        }

        // from the starting position make one step downwards
        var minD = Distance[from];
        var best = from;

        foreach (var t in _dir)
        {
            var step = from + t;
            var stepD = Distance[step];

            if (stepD >= minD) continue;

            minD = stepD;
            best = step;
        }

        return best;
    }

    public static int GetStepBack(int cur, int from, bool[] passable)
    {
        var d = BuildEscapeDistanceMap(cur, from, 2f, passable);
        for (var i = 0; i < _size; i++)
        {
            _goals[i] = Distance[i] == d;
        }

        if (!BuildDistanceMap(cur, _goals, passable))
        {
            return -1;
        }

        // from the starting position, take one step down
        var minD = Distance[cur];
        var mins = cur;

        foreach (var t in _dir)
        {
            var n = cur + t;
            var thisD = Distance[n];

            if (thisD >= minD) continue;

            minD = thisD;
            mins = n;
        }

        return mins;
    }

    private static bool BuildDistanceMap(int from, int to, bool[] passable)
    {
        if (from == to)
        {
            return false;
        }

        Array.Fill(Distance, int.MaxValue);

        var pathFound = false;

        var head = 0;
        var tail = 0;

        // add to queue
        _queue[tail++] = to;
        Distance[to] = 0;

        while (head < tail)
        {
            // remove from queue
            var step = _queue[head++];
            if (step == from)
            {
                pathFound = true;
                break;
            }

            var nextDistance = Distance[step] + 1;

            foreach (var t in _dir)
            {
                var n = step + t;

                if (n != from &&
                    (n < 0 || n >= _size || !passable[n] || Distance[n] <= nextDistance)
                   )
                {
                    continue;
                }

                // add to queue
                _queue[tail++] = n;
                Distance[n] = nextDistance;
            }
        }

        return pathFound;
    }

    public static void BuildDistanceMap(int to, bool[] passable, int limit)
    {
        Array.Fill(Distance, int.MaxValue);

        var head = 0;
        var tail = 0;

        // add to queue
        _queue[tail++] = to;
        Distance[to] = 0;

        while (head < tail)
        {
            // remove from queue
            var step = _queue[head++];

            var nextDistance = Distance[step] + 1;
            if (nextDistance > limit)
            {
                return;
            }

            foreach (var t in _dir)
            {
                var n = step + t;

                if (
                    n < 0 ||
                    n >= _size ||
                    !passable[n] ||
                    Distance[n] <= nextDistance
                ) continue;

                // add to queue
                _queue[tail++] = n;
                Distance[n] = nextDistance;
            }
        }
    }

    private static bool BuildDistanceMap(int from, bool[] to, bool[] passable)
    {
        if (to[from])
        {
            return false;
        }

        Array.Fill(Distance, int.MaxValue);

        var pathFound = false;

        var head = 0;
        var tail = 0;

        // add to queue
        for (var i = 0; i < _size; i++)
        {
            if (!to[i]) continue;

            _queue[tail++] = i;
            Distance[i] = 0;
        }

        while (head < tail)
        {
            // remove from queue
            var step = _queue[head++];
            if (step == from)
            {
                pathFound = true;
                break;
            }

            var nextDistance = Distance[step] + 1;

            foreach (var t in _dir)
            {
                var n = step + t;
                if (
                    n != from
                    && (
                        n < 0 ||
                        n >= _size ||
                        !passable[n] ||
                        Distance[n] <= nextDistance
                    )
                ) continue;

                // add to queue
                _queue[tail++] = n;
                Distance[n] = nextDistance;
            }
        }

        return pathFound;
    }

    private static int BuildEscapeDistanceMap(int cur,
        int from,
        float factor,
        bool[] passable)
    {
        Array.Fill(Distance, int.MaxValue);

        var destDist = int.MaxValue;

        var head = 0;
        var tail = 0;

        // add to queue
        _queue[tail++] = from;
        Distance[from] = 0;

        var dist = 0;

        while (head < tail)
        {
            // remove from queue
            var step = _queue[head++];
            dist = Distance[step];

            if (dist > destDist)
            {
                return destDist;
            }

            if (step == cur)
            {
                destDist = (int)(dist * factor) + 1;
            }

            var nextDistance = dist + 1;

            foreach (var t in _dir)
            {
                var n = step + t;

                if (
                    n < 0 ||
                    n >= _size ||
                    !passable[n] ||
                    Distance[n] <= nextDistance
                ) continue;

                // add to queue
                _queue[tail++] = n;
                Distance[n] = nextDistance;
            }
        }

        return dist;
    }
}