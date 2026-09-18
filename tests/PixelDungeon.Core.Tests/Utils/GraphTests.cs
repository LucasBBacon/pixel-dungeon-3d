using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Utils;

public class GraphTests
{
    private sealed class TestNode : IGraphNode
    {
        public readonly string Name;
        public readonly List<TestNode> Links = [];

        public int Distance { get; set; }
        public int Price { get; set; } = 1;

        public TestNode(string name)
        {
            Name = name;
        }

        public IEnumerable<IGraphNode> Edges() => Links;

        public override string ToString() => Name;
    }

    private static void Link(TestNode a, TestNode b)
    {
        a.Links.Add(b);
        b.Links.Add(a);
    }

    private static List<TestNode> Line(int count)
    {
        var nodes = new List<TestNode>();
        for (var i = 0; i < count; i++)
        {
            nodes.Add(new TestNode(((char)('A' + i)).ToString()));
            if (i > 0)
            {
                Link(nodes[i - 1], nodes[i]);
            }
        }

        return nodes;
    }

    [Fact]
    public void BuildDistanceMap_LineGraph_CountsHopsFromFocus()
    {
        var nodes = Line(5);
        Graph.BuildDistanceMap(nodes, nodes[0]);
        Assert.Equal(
            [0, 1, 2, 3, 4],
            nodes.Select(n => n.Distance)
        );
    }

    [Fact]
    public void BuildDistanceMap_NodePrice_IsTheCostOfLeavingIt()
    {
        var nodes = Line(3);
        nodes[1].Price = 5;
        Graph.BuildDistanceMap(nodes, nodes[0]);
        Assert.Equal(1, nodes[1].Distance);
        Assert.Equal(6, nodes[2].Distance);
    }

    [Fact]
    public void BuildPath_FollowsDecreasingDistanceToTarget()
    {
        var nodes = Line(5);
        Graph.BuildDistanceMap(nodes, nodes[0]);
        var path = Graph.BuildPath(nodes, nodes[4], nodes[0]);
        Assert.Equal(["D", "C", "B", "A"], path.Select(n => n.Name));
    }

    [Fact]
    public void BuildPath_FromEqualsTo_IsEmpty()
    {
        var nodes = Line(2);
        Graph.BuildDistanceMap(nodes, nodes[0]);
        Assert.Empty(Graph.BuildPath(nodes, nodes[0], nodes[0]));
    }

    [Fact]
    public void IsolatedNode_StaysUnreachableAndHasNoPath()
    {
        var nodes = Line(3);
        var island = new TestNode("Z");
        nodes.Add(island);
        Graph.BuildDistanceMap(nodes, nodes[0]);
        Assert.Equal(int.MaxValue, island.Distance);
        Assert.Null(Graph.BuildPath(nodes, island, nodes[0]));
    }

    [Fact]
    public void SetPrice_SetsEveryNode()
    {
        var nodes = Line(4);
        Graph.SetPrice(nodes, 7);
        Assert.All(nodes, n => Assert.Equal(7, n.Price));
    }
}