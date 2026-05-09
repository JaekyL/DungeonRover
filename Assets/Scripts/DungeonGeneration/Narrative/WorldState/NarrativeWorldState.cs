using System.Collections.Generic;
using System.Linq;

namespace DungeonGeneration.Narrative.WorldState
{
    /// <summary>
    /// Persistent world state tracking all simulation data independently from visuals.
    /// Provides queryable access to territory, room history, faction state, and environmental data.
    /// </summary>
    public class NarrativeWorldState
    {
        public Dictionary<int, RoomNarrativeState> RoomStates { get; } = new Dictionary<int, RoomNarrativeState>();
        public Dictionary<string, FactionState> FactionStates { get; } = new Dictionary<string, FactionState>();
        public Dictionary<string, CharacterState> CharacterStates { get; } = new Dictionary<string, CharacterState>();
        public Dictionary<string, float> Resources { get; } = new Dictionary<string, float>();
        public WorldTimeline Timeline { get; } = new WorldTimeline();
        public TerritoryMap Territories { get; } = new TerritoryMap();
        public int CurrentStep { get; set; }

        // --- Room Queries ---

        public RoomNarrativeState GetRoomState(int roomId)
        {
            if (!RoomStates.ContainsKey(roomId))
                RoomStates[roomId] = new RoomNarrativeState { RoomId = roomId };
            return RoomStates[roomId];
        }

        public List<int> GetRoomsByFaction(string factionId)
        {
            return RoomStates.Values
                .Where(r => r.CurrentOwner == factionId)
                .Select(r => r.RoomId)
                .ToList();
        }

        public List<int> GetAbandonedRooms()
        {
            return RoomStates.Values
                .Where(r => string.IsNullOrEmpty(r.CurrentOwner) && r.OwnershipHistory.Count > 0)
                .Select(r => r.RoomId)
                .ToList();
        }

        public List<int> GetContestedRooms()
        {
            return RoomStates.Values
                .Where(r => r.ConflictIntensity > 0.3f)
                .Select(r => r.RoomId)
                .ToList();
        }

        public List<int> GetCorruptedRooms(float minCorruption = 0.1f)
        {
            return RoomStates.Values
                .Where(r => r.Corruption >= minCorruption)
                .Select(r => r.RoomId)
                .ToList();
        }

        public List<int> GetDamagedRooms(float minDamage = 0.2f)
        {
            return RoomStates.Values
                .Where(r => r.StructuralDamage >= minDamage)
                .Select(r => r.RoomId)
                .ToList();
        }

        // --- Faction Queries ---

        public FactionState GetFactionState(string factionId)
        {
            if (!FactionStates.ContainsKey(factionId))
                FactionStates[factionId] = new FactionState { FactionId = factionId };
            return FactionStates[factionId];
        }

        public float GetFactionDesperation(string factionId)
        {
            return FactionStates.ContainsKey(factionId) ? FactionStates[factionId].Desperation : 0f;
        }

        public float GetDisposition(string factionA, string factionB)
        {
            var state = GetFactionState(factionA);
            return state.Dispositions.ContainsKey(factionB) ? state.Dispositions[factionB] : 0f;
        }

        // --- Resource Queries ---

        public float GetResource(string resourceId)
        {
            return Resources.ContainsKey(resourceId) ? Resources[resourceId] : 0f;
        }

        public void ConsumeResource(string resourceId, float amount)
        {
            if (!Resources.ContainsKey(resourceId)) Resources[resourceId] = 0f;
            Resources[resourceId] = System.Math.Max(0f, Resources[resourceId] - amount);
        }

        public bool IsResourceScarce(string resourceId, float threshold = 20f)
        {
            return GetResource(resourceId) < threshold;
        }
    }

    /// <summary>
    /// Narrative state for a single room, tracking ownership, damage, and history.
    /// </summary>
    [System.Serializable]
    public class RoomNarrativeState
    {
        public int RoomId;
        public string CurrentOwner;
        public List<OwnershipRecord> OwnershipHistory = new List<OwnershipRecord>();

        // Environmental state values (0..1)
        public float DangerLevel;
        public float Corruption;
        public float StructuralDamage;
        public float Scarcity;
        public float Morale;
        public float WaterLevel;
        public float FireDamage;
        public float Decay;
        public float ConflictIntensity;
        public float TraversalSafety = 1f;

        // Semantic layers
        public List<string> SemanticTags = new List<string>();
        public List<string> EnvironmentalHazards = new List<string>();
        public List<RoomHistoryEntry> History = new List<RoomHistoryEntry>();

        // Spatial modifications
        public bool IsBarricaded;
        public bool IsCollapsed;
        public bool IsSealed;
        public bool IsFlooded;
        public bool IsRitualSite;
        public bool IsAbandoned;
        public bool IsWarzone;
        public bool IsSafeZone;

        public string RoomSemanticLabel;

        public void AddHistoryEntry(int step, string description, string actor, float impact)
        {
            History.Add(new RoomHistoryEntry
            {
                Step = step,
                Description = description,
                Actor = actor,
                Impact = impact
            });
        }

        public void SetOwner(string factionId, int step)
        {
            if (CurrentOwner == factionId) return;
            OwnershipHistory.Add(new OwnershipRecord
            {
                FactionId = factionId,
                FromStep = step,
                PreviousOwner = CurrentOwner
            });
            CurrentOwner = factionId;
        }
    }

    [System.Serializable]
    public class OwnershipRecord
    {
        public string FactionId;
        public int FromStep;
        public string PreviousOwner;
    }

    [System.Serializable]
    public class RoomHistoryEntry
    {
        public int Step;
        public string Description;
        public string Actor;
        public float Impact;
    }

    /// <summary>
    /// Tracks faction state during simulation: morale, resources, territory size, desperation.
    /// </summary>
    [System.Serializable]
    public class FactionState
    {
        public string FactionId;
        public bool IsActive = true;
        public bool IsEliminated;
        public float Morale = 1f;
        public float Desperation;
        public float Strength = 1f;
        public int TerritorySize;
        public int MemberCount = 10;
        public Dictionary<string, float> Dispositions = new Dictionary<string, float>();
        public List<int> ControlledRoomIds = new List<int>();
        public List<string> ActiveGoals = new List<string>();
        public int StepEliminated = -1;
    }

    /// <summary>
    /// Tracks individual character state during simulation.
    /// </summary>
    [System.Serializable]
    public class CharacterState
    {
        public string CharacterId;
        public string FactionId;
        public bool IsAlive = true;
        public int CurrentRoomId = -1;
        public float Health = 1f;
        public float Desperation;
        public int StepDied = -1;
        public string CauseOfDeath;
        public int DeathRoomId = -1;
    }

    /// <summary>
    /// Tracks territory ownership across the entire dungeon over time.
    /// </summary>
    public class TerritoryMap
    {
        private readonly Dictionary<int, string> _currentOwners = new Dictionary<int, string>();
        private readonly Dictionary<int, List<TerritoryChange>> _history = new Dictionary<int, List<TerritoryChange>>();

        public void SetOwner(int roomId, string factionId, int step)
        {
            _currentOwners[roomId] = factionId;
            if (!_history.ContainsKey(roomId))
                _history[roomId] = new List<TerritoryChange>();
            _history[roomId].Add(new TerritoryChange
            {
                FactionId = factionId,
                Step = step
            });
        }

        public string GetOwner(int roomId)
        {
            return _currentOwners.ContainsKey(roomId) ? _currentOwners[roomId] : null;
        }

        public List<TerritoryChange> GetHistory(int roomId)
        {
            return _history.ContainsKey(roomId) ? _history[roomId] : new List<TerritoryChange>();
        }

        public Dictionary<string, int> GetTerritoryCount()
        {
            var counts = new Dictionary<string, int>();
            foreach (var kvp in _currentOwners)
            {
                if (string.IsNullOrEmpty(kvp.Value)) continue;
                if (!counts.ContainsKey(kvp.Value)) counts[kvp.Value] = 0;
                counts[kvp.Value]++;
            }
            return counts;
        }

        public List<int> GetFrontierRooms(string factionId, List<int> allNeighborIds)
        {
            return allNeighborIds.Where(id =>
            {
                var owner = GetOwner(id);
                return owner != factionId;
            }).ToList();
        }
    }

    [System.Serializable]
    public class TerritoryChange
    {
        public string FactionId;
        public int Step;
    }

    /// <summary>
    /// Records the complete timeline of all narrative events during simulation.
    /// </summary>
    public class WorldTimeline
    {
        public List<TimelineEntry> Entries { get; } = new List<TimelineEntry>();

        public void Record(int step, string eventType, string description, string actor,
            int roomId = -1, float impact = 0f, Dictionary<string, string> data = null)
        {
            Entries.Add(new TimelineEntry
            {
                Step = step,
                EventType = eventType,
                Description = description,
                Actor = actor,
                AffectedRoomId = roomId,
                Impact = impact,
                Data = data ?? new Dictionary<string, string>()
            });
        }

        public List<TimelineEntry> GetEntriesForStep(int step)
        {
            return Entries.Where(e => e.Step == step).ToList();
        }

        public List<TimelineEntry> GetEntriesByActor(string actor)
        {
            return Entries.Where(e => e.Actor == actor).ToList();
        }

        public List<TimelineEntry> GetEntriesForRoom(int roomId)
        {
            return Entries.Where(e => e.AffectedRoomId == roomId).ToList();
        }

        public List<TimelineEntry> GetEntriesByType(string eventType)
        {
            return Entries.Where(e => e.EventType == eventType).ToList();
        }
    }

    [System.Serializable]
    public class TimelineEntry
    {
        public int Step;
        public string EventType;
        public string Description;
        public string Actor;
        public int AffectedRoomId = -1;
        public float Impact;
        public Dictionary<string, string> Data = new Dictionary<string, string>();
    }
}

