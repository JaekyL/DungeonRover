using System.Collections.Generic;
using UnityEngine;

namespace TraversalAI.Core
{
    /// <summary>
    /// AI-facing representation of a dungeon node.
    /// Wraps the generation system's GraphNode and adds traversal metadata.
    /// Designed for serialization and future ECS conversion.
    /// </summary>
    [System.Serializable]
    public class DungeonNode
    {
        /// <summary>Unique identifier matching the generation graph node ID.</summary>
        public int Id;

        /// <summary>World-space center position of this node.</summary>
        public Vector3 WorldPosition;

        /// <summary>Bounding rect in grid coordinates.</summary>
        public RectInt GridBounds;

        /// <summary>Current tags applied to this node.</summary>
        public NodeTag Tags = NodeTag.Unexplored;

        /// <summary>Base danger level [0..1].</summary>
        public float BaseDangerLevel;

        /// <summary>Base loot value estimate.</summary>
        public float BaseLootValue;

        /// <summary>Floor/level this node is on.</summary>
        public int FloorIndex;

        /// <summary>Descriptive label for debugging.</summary>
        public string Label;

        /// <summary>Arbitrary metadata for extensibility.</summary>
        public Dictionary<string, string> Metadata = new Dictionary<string, string>();

        /// <summary>IDs of edges connected to this node (cached for fast lookup).</summary>
        public List<int> ConnectedEdgeIds = new List<int>();

        public bool HasTag(NodeTag tag) => (Tags & tag) != 0;
        public void AddTag(NodeTag tag) => Tags |= tag;
        public void RemoveTag(NodeTag tag) => Tags &= ~tag;

        public override string ToString() => $"DungeonNode({Id}: {Label ?? "unnamed"}, Tags={Tags})";
    }
}

