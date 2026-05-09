using System.Collections.Generic;
using DungeonGeneration.Data;

namespace DungeonGeneration.Core
{
    /// <summary>
    /// Shared context passed through all generation pipeline stages.
    /// </summary>
    public class GenerationContext
    {
        public SeededRandom Random { get; }
        public DungeonConfig Config { get; }
        public DungeonGraph Graph { get; set; }
        public SpatialMap SpatialMap { get; set; }
        public HistoryLog HistoryLog { get; set; }
        public List<StoryMarker> StoryMarkers { get; } = new List<StoryMarker>();
        public List<DecorationInstance> Decorations { get; } = new List<DecorationInstance>();
        public List<EncounterInstance> Encounters { get; } = new List<EncounterInstance>();
        public Dictionary<string, object> CustomData { get; } = new Dictionary<string, object>();
        public GenerationContext(DungeonConfig config, int seed)
        {
            Config = config;
            Random = new SeededRandom(seed);
        }
        public T GetCustomData<T>(string key) where T : class
        {
            return CustomData.TryGetValue(key, out var value) ? value as T : null;
        }
        public void SetCustomData(string key, object value)
        {
            CustomData[key] = value;
        }
    }
}
