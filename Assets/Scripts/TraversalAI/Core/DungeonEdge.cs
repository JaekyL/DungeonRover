namespace TraversalAI.Core
{
    /// <summary>
    /// AI-facing representation of a connection between dungeon nodes.
    /// </summary>
    [System.Serializable]
    public class DungeonEdge
    {
        public int Id;
        public int FromNodeId;
        public int ToNodeId;

        /// <summary>Traversal cost modifier (1.0 = normal).</summary>
        public float TraversalCost = 1f;

        /// <summary>Whether this edge is currently passable.</summary>
        public bool IsPassable = true;

        /// <summary>Whether this edge requires a key/item to traverse.</summary>
        public bool RequiresKey;
        public string RequiredKeyId;

        /// <summary>Whether this is a one-way edge.</summary>
        public bool IsOneWay;

        /// <summary>Whether this edge is hidden/secret.</summary>
        public bool IsSecret;

        /// <summary>Whether this edge changes floors (stairs).</summary>
        public bool IsVertical;

        /// <summary>Estimated danger of traversing this edge [0..1].</summary>
        public float DangerLevel;

        /// <summary>Gets the other node connected by this edge.</summary>
        public int GetOtherNode(int nodeId) => nodeId == FromNodeId ? ToNodeId : FromNodeId;

        public override string ToString() => $"DungeonEdge({Id}: {FromNodeId} -> {ToNodeId}, cost={TraversalCost})";
    }
}

