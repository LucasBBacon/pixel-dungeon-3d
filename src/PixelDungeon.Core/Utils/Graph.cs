namespace PixelDungeon.Core.Utils;

public interface IGraphNode
{
    int Distance { get; set; }

    int Price { get; set; }

    IEnumerable<IGraphNode> Edges();
}

public static class Graph
{
    public static void SetPrice<T>(IEnumerable<T> nodes, int value) where T : class, IGraphNode
    {
        foreach (var node in nodes)
        {
            node.Price = value;
        }
    }

    public static void BuildDistanceMap<T>(IEnumerable<T> nodes, IGraphNode focus) where T : class, IGraphNode
    {
        foreach (var node in nodes)
        {
            node.Distance = int.MaxValue;
        }

        var queue = new Queue<IGraphNode>();

        focus.Distance = 0;
        queue.Enqueue(focus);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var distance = node.Distance;
            var price = node.Price;

            foreach (var edge in node.Edges())
            {
                if (edge.Distance <= distance + price) continue;

                queue.Enqueue(edge);
                edge.Distance = distance + price;
            }
        }
    }

    public static List<T> BuildPath<T>(IEnumerable<T> nodes, T from, T to) where T : class, IGraphNode
    {
        var path = new List<T>();

        var room = from;
        while (room != to)
        {
            var min = room.Distance;
            T next = null;

            foreach (var edge in room.Edges())
            {
                var distance = edge.Distance;

                if (distance >= min) continue;

                min = distance;
                next = (T)edge;
            }

            if (next == null)
            {
                return null;
            }

            path.Add(next);
            room = next;
        }

        return path;
    }
}