using System.Collections.Generic;
using System.Linq;
using TraversalAI.Core;
using UnityEngine;

namespace TraversalAI.Perception
{
    /// <summary>
    /// Tracks the AI's exploration memory: visit history, path taken, and events encountered.
    /// Provides APIs for traversal strategy queries (e.g., "which branches haven't I tried?").
    /// </summary>
    [System.Serializable]
    public class MemorySystem
    {
        [SerializeField] private List<int> _visitHistory = new List<int>();
        private HashSet<int> _visitedSet = new HashSet<int>();
        private Dictionary<int, List<MemoryEvent>> _events = new Dictionary<int, List<MemoryEvent>>();
        private Stack<int> _backtrackStack = new Stack<int>();

        public IReadOnlyList<int> VisitHistory => _visitHistory;
        public int TotalVisits => _visitHistory.Count;
        public int UniqueVisits => _visitedSet.Count;

        /// <summary>Record visiting a node.</summary>
        public void RecordVisit(int nodeId)
        {
            _visitHistory.Add(nodeId);
            _visitedSet.Add(nodeId);
            _backtrackStack.Push(nodeId);
        }

        /// <summary>Check if a node has been visited.</summary>
        public bool HasVisited(int nodeId) => _visitedSet.Contains(nodeId);

        /// <summary>Get the number of times a specific node was visited.</summary>
        public int GetVisitCount(int nodeId) => _visitHistory.Count(id => id == nodeId);

        /// <summary>Get the last N visited nodes.</summary>
        public List<int> GetRecentVisits(int count)
        {
            return _visitHistory.Skip(Mathf.Max(0, _visitHistory.Count - count)).ToList();
        }

        /// <summary>Pop the backtrack stack to return to the previous decision point.</summary>
        public int PopBacktrack()
        {
            return _backtrackStack.Count > 0 ? _backtrackStack.Pop() : -1;
        }

        /// <summary>Get unvisited neighbors of a node from the graph.</summary>
        public List<int> GetUnvisitedNeighbors(int nodeId, TraversalDungeonGraph graph)
        {
            return graph.GetNeighbors(nodeId)
                .Where(n => !_visitedSet.Contains(n.Id))
                .Select(n => n.Id)
                .ToList();
        }

        /// <summary>Record an event at a specific node.</summary>
        public void RecordEvent(int nodeId, MemoryEventType type, string description = "")
        {
            if (!_events.ContainsKey(nodeId))
                _events[nodeId] = new List<MemoryEvent>();

            _events[nodeId].Add(new MemoryEvent
            {
                Type = type,
                NodeId = nodeId,
                Timestamp = Time.time,
                Description = description
            });
        }

        /// <summary>Get events recorded at a specific node.</summary>
        public List<MemoryEvent> GetEventsAt(int nodeId)
        {
            return _events.TryGetValue(nodeId, out var events) ? events : new List<MemoryEvent>();
        }

        /// <summary>Check if a threatening event was recorded at a node.</summary>
        public bool WasThreatEncounteredAt(int nodeId)
        {
            return _events.TryGetValue(nodeId, out var events) &&
                   events.Any(e => e.Type == MemoryEventType.ThreatEncountered);
        }

        /// <summary>Clear all memory (e.g., for a new dungeon floor).</summary>
        public void Clear()
        {
            _visitHistory.Clear();
            _visitedSet.Clear();
            _events.Clear();
            _backtrackStack.Clear();
        }

        /// <summary>Serialize for save/load.</summary>
        public MemoryData ToSaveData()
        {
            return new MemoryData
            {
                VisitHistory = new List<int>(_visitHistory),
                Events = _events.SelectMany(kvp => kvp.Value).ToList()
            };
        }

        public void LoadFromSaveData(MemoryData data)
        {
            Clear();
            _visitHistory.AddRange(data.VisitHistory);
            _visitedSet = new HashSet<int>(_visitHistory);
            foreach (var evt in data.Events)
            {
                if (!_events.ContainsKey(evt.NodeId))
                    _events[evt.NodeId] = new List<MemoryEvent>();
                _events[evt.NodeId].Add(evt);
            }
        }
    }

    [System.Serializable]
    public class MemoryEvent
    {
        public MemoryEventType Type;
        public int NodeId;
        public float Timestamp;
        public string Description;
    }

    public enum MemoryEventType
    {
        Visited,
        LootFound,
        ThreatEncountered,
        DoorLocked,
        SecretDiscovered,
        DeadEndReached,
        StairsFound,
        TrapTriggered,
        RegroupNeeded
    }

    [System.Serializable]
    public class MemoryData
    {
        public List<int> VisitHistory = new List<int>();
        public List<MemoryEvent> Events = new List<MemoryEvent>();
    }
}

