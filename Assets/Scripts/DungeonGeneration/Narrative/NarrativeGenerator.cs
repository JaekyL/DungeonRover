using DungeonGeneration.Core;
using DungeonGeneration.Narrative.Authoring;
using DungeonGeneration.Narrative.EnvironmentalStorytelling;
using DungeonGeneration.Narrative.Interpretation;
using DungeonGeneration.Narrative.PropMutation;
using DungeonGeneration.Narrative.Semantics;
using DungeonGeneration.Narrative.Simulation;
using DungeonGeneration.Narrative.Validation;
using UnityEngine;

namespace DungeonGeneration.Narrative
{
    /// <summary>
    /// Main integration component for the narrative simulation framework.
    /// Attach alongside DungeonGenerator to enable narrative-driven historical simulation.
    /// Registers all narrative pipeline stages and provides the NarrativeConfig to the pipeline.
    ///
    /// Usage:
    /// 1. Create a NarrativeConfig ScriptableObject and author your factions, characters, events
    /// 2. Add this component to your DungeonGenerator GameObject
    /// 3. Assign the NarrativeConfig
    /// 4. Generate — the narrative system will run automatically as part of the pipeline
    /// </summary>
    [RequireComponent(typeof(DungeonGenerator))]
    [ExecuteInEditMode]
    public class NarrativeGenerator : MonoBehaviour
    {
        [Header("Narrative Configuration")]
        [SerializeField] private NarrativeConfig _narrativeConfig;

        [Header("Stage Control")]
        [SerializeField] private bool _enableSemanticExtraction = true;
        [SerializeField] private bool _enableNarrativeSimulation = true;
        [SerializeField] private bool _enableSpatialInterpretation = true;
        [SerializeField] private bool _enableEnvironmentalStorytelling = true;
        [SerializeField] private bool _enablePropMutation = true;
        [SerializeField] private bool _enableNarrativeValidation = true;

        [Header("Disable Existing Stages")]
        [Tooltip("Disable the existing HistorySimulationStage (replaced by NarrativeSimulation)")]
        [SerializeField] private bool _replaceExistingHistory = true;

        private DungeonGenerator _generator;
        private bool _stagesRegistered;

        public NarrativeConfig NarrativeConfig => _narrativeConfig;

        private void Awake()
        {
            RegisterStages();
        }

        private void OnEnable()
        {
            RegisterStages();
        }

        /// <summary>
        /// Registers all narrative pipeline stages with the DungeonGenerator.
        /// Call this before Generate() if you need to ensure stages are set up.
        /// </summary>
        public void RegisterStages()
        {
            if (_stagesRegistered) return;

            _generator = GetComponent<DungeonGenerator>();
            if (_generator == null) return;

            // Optionally remove existing history stage to avoid double-processing
            if (_replaceExistingHistory)
            {
                _generator.RemoveStage<History.HistorySimulationStage>();
            }

            // Register narrative stages in pipeline order
            if (_enableSemanticExtraction)
                _generator.AddStage(new SemanticExtractionStage());

            if (_enableNarrativeSimulation)
                _generator.AddStage(new NarrativeSimulationStage());

            if (_enableSpatialInterpretation)
                _generator.AddStage(new SpatialInterpretationStage());

            if (_enableEnvironmentalStorytelling)
                _generator.AddStage(new EnvironmentalStorytellingStage());

            if (_enablePropMutation)
                _generator.AddStage(new PropMutationStage());

            if (_enableNarrativeValidation)
                _generator.AddStage(new NarrativeValidationStage());

            _stagesRegistered = true;
        }

        /// <summary>
        /// Injects the NarrativeConfig into the GenerationContext before pipeline execution.
        /// Called internally by the pre-generation hook.
        /// </summary>
        public void InjectConfig(GenerationContext context)
        {
            if (_narrativeConfig != null)
                context.SetCustomData("narrative_config", _narrativeConfig);
        }

        /// <summary>
        /// Generates a dungeon with the narrative framework active.
        /// Convenience method that injects config and delegates to DungeonGenerator.
        /// </summary>
        [ContextMenu("Generate with Narrative")]
        public GenerationContext GenerateWithNarrative()
        {
            if (_generator == null)
                _generator = GetComponent<DungeonGenerator>();

            if (!_stagesRegistered)
                RegisterStages();

            // Hook into pipeline to inject config
            var pipeline = GetPipeline();
            if (pipeline != null)
            {
                // We inject config via a pre-stage that runs very early
                _generator.AddStage(new NarrativeConfigInjector(_narrativeConfig));
            }

            return _generator.Generate();
        }

        /// <summary>
        /// Replay the last seed with narrative generation.
        /// </summary>
        [ContextMenu("Replay with Narrative")]
        public void ReplayWithNarrative()
        {
            if (_generator == null)
                _generator = GetComponent<DungeonGenerator>();

            if (!_stagesRegistered)
                RegisterStages();

            _generator.AddStage(new NarrativeConfigInjector(_narrativeConfig));
            _generator.ReplayLastSeed();
        }

        private GenerationPipeline GetPipeline()
        {
            // Pipeline is private in DungeonGenerator, so we use the AddStage method
            return null; // We use AddStage instead
        }

        private void OnDisable()
        {
            _stagesRegistered = false;
        }
    }

    /// <summary>
    /// Tiny pipeline stage that injects NarrativeConfig into the GenerationContext.
    /// Runs at priority 1 (very first) to make the config available to all narrative stages.
    /// </summary>
    internal class NarrativeConfigInjector : IGenerationStage
    {
        public string StageName => "Narrative Config Injection";
        public int Priority => 1;

        private readonly NarrativeConfig _config;

        public NarrativeConfigInjector(NarrativeConfig config)
        {
            _config = config;
        }

        public void Execute(GenerationContext context)
        {
            if (_config != null)
                context.SetCustomData("narrative_config", _config);
        }
    }
}

