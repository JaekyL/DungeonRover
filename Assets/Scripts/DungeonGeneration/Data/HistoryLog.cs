using System.Collections.Generic;

namespace DungeonGeneration.Data
{
    public class HistoryLog
    {
        public List<HistoryEvent> Events { get; } = new List<HistoryEvent>();
        public Dictionary<int, List<string>> RoomFactions { get; } = new Dictionary<int, List<string>>();
        public Dictionary<int, List<string>> RoomModifications { get; } = new Dictionary<int, List<string>>();
        public void AddEvent(HistoryEvent evt) => Events.Add(evt);
        public void RecordFaction(int roomId, string faction)
        {
            if (!RoomFactions.ContainsKey(roomId))
                RoomFactions[roomId] = new List<string>();
            if (!RoomFactions[roomId].Contains(faction))
                RoomFactions[roomId].Add(faction);
        }
        public void RecordModification(int roomId, string modification)
        {
            if (!RoomModifications.ContainsKey(roomId))
                RoomModifications[roomId] = new List<string>();
            RoomModifications[roomId].Add(modification);
        }
        public List<HistoryEvent> GetEventsForRoom(int roomId)
        {
            return Events.FindAll(e => e.AffectedRoomId == roomId);
        }
    }
    [System.Serializable]
    public class HistoryEvent
    {
        public int Step;
        public string AgentType;
        public string EventType;
        public int AffectedRoomId;
        public string Description;
        public Dictionary<string, string> Data = new Dictionary<string, string>();
    }
}
