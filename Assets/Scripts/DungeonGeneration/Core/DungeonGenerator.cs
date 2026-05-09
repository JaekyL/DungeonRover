using UnityEngine;

namespace DungeonGeneration.Core
{
    /// <summary>
    /// Main dungeon generator MonoBehaviour. Entry point for the generation pipeline.
    /// Attach to a GameObject in the scene to generate dungeons.
    /// </summary>
    [ExecuteInEditMode]
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private DungeonConfig _config;
        [SerializeField] private int _seed = -1;
        [SerializeField] private bool _useRandomSeed = true;
        [SerializeField] private bool _generateOnStart = false;
        [Header("Debug")]
        [SerializeField] private bool _showDebugVisualization = true;
        private GenerationPipeline _pipeline;
        private GenerationContext _lastContext;
        public GenerationContext LastContext => _lastContext;
        public DungeonConfig Config => _config;
        public int LastSeed => _seed;
        public bool ShowDebug => _showDebugVisualization;
        private void Awake()
        {
            _pipeline = new GenerationPipeline();
            RegisterDefaultStages();
        }
        private void Start()
        {
            if (_generateOnStart)
            {
                Generate();
            }
        }
        private void RegisterDefaultStages()
        {
            _pipeline.AddStage(new MacroGraph.MacroGraphStage());
            _pipeline.AddStage(new SpatialLayout.SpatialLayoutStage());
            _pipeline.AddStage(new History.HistorySimulationStage());
            _pipeline.AddStage(new RoomPurpose.RoomPurposeStage());
            _pipeline.AddStage(new Validation.TraversalValidationStage());
            _pipeline.AddStage(new Storytelling.StorytellingStage());
            _pipeline.AddStage(new Decoration.DecorationStage());
            _pipeline.AddStage(new Encounters.EncounterStage());
            _pipeline.AddStage(new Optimization.OptimizationStage());
        }
        [ContextMenu("Generate Dungeon")]
        public GenerationContext Generate()
        {
            int seed = _useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : _seed;
            _seed = seed;
            
            if(_pipeline == null)
            {
                _pipeline = new GenerationPipeline();
                RegisterDefaultStages();
            }
            
            _lastContext = _pipeline.Execute(_config, seed);
            AutoRender();
            return _lastContext;
        }
        public GenerationContext Generate(int seed)
        {
            _seed = seed;
            _useRandomSeed = false;
            
            if(_pipeline == null)
            {
                _pipeline = new GenerationPipeline();
                RegisterDefaultStages();
            }
            
            _lastContext = _pipeline.Execute(_config, seed);
            AutoRender();
            return _lastContext;
        }

        private void AutoRender()
        {
            if (_lastContext == null) return;

            // Try prefab-based renderer first
            var prefabRenderer = GetComponent<Rendering.DungeonRenderer>();
            if (prefabRenderer != null)
            {
                prefabRenderer.Render(_lastContext);
                return;
            }

            // Fall back to procedural renderer
            var proceduralRenderer = GetComponent<Rendering.DungeonProceduralRenderer>();
            if (proceduralRenderer != null)
            {
                proceduralRenderer.Render(_lastContext);
            }
        }
        [ContextMenu("Replay Last Seed")]
        public void ReplayLastSeed()
        {
            if (_seed != -1)
                Generate(_seed);
        }
        public void AddStage(IGenerationStage stage) => _pipeline.AddStage(stage);
        public void RemoveStage<T>() where T : IGenerationStage => _pipeline.RemoveStage<T>();
    }
}
