using System.Collections.Generic;
using System.Linq;

namespace DungeonGeneration.Data
{
    [System.Serializable]
    public class DungeonGraph
    {
        public List<GraphNode> Nodes { get; } = new List<GraphNode>();
        public List<GraphEdge> Edges { get; } = new List<GraphEdge>();
        public GraphNode AddNode(RoomType type = RoomType.Normal)
        {
            var node = new GraphNode { Id = Nodes.Count, RoomType = type };
            Nodes.Add(node);
            return node;
        }
        public GraphEdge AddEdge(int fromId, int toId, EdgeType type = EdgeType.Normal)
        {
            var edge = new GraphEdge { FromNodeId = fromId, ToNodeId = toId, Type = type };
            Edges.Add(edge);
            return edge;
        }
        public List<GraphNode> GetNeighbors(int nodeId)
        {
            var ids = new HashSet<int>();
            foreach (var e in Edges)
            {
                if (e.FromNodeId == nodeId) ids.Add(e.ToNodeId);
                if (e.ToNodeId == nodeId) ids.Add(e.FromNodeId);
            }
            return Nodes.Where(n => ids.Contains(n.Id)).ToList();
        }
        public List<GraphEdge> GetEdgesFor(int nodeId)
        {
            return Edges.Where(e => e.FromNodeId == nodeId || e.ToNodeId == nodeId).ToList();
        }
        public GraphNode GetNode(int id) => Nodes.FirstOrDefault(n => n.Id == id);
    }
    [System.Serializable]
    public class GraphNode
    {
        public int Id;
        public RoomType RoomType;
        public int Importance;
        public int DifficultyTier;
        public bool IsCriticalPath;
        public bool IsSecret;
        public string FactionOwner;
        public RoomPurposeType Purpose = RoomPurposeType.None;
        public List<string> NarrativeTags = new List<string>();
        public List<string> EnvironmentalStates = new List<string>();
        public Dictionary<string, string> Metadata = new Dictionary<string, string>();
        public UnityEngine.RectInt Bounds;
    }
    [System.Serializable]
    public class GraphEdge
    {
        public int FromNodeId;
        public int ToNodeId;
        public EdgeType Type;
        public string RequiredKey;
        public bool IsSecret;
    }
}
