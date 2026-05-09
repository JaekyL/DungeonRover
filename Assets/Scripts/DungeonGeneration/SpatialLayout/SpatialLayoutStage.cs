using System.Collections.Generic;
using DungeonGeneration.Core;
using DungeonGeneration.Data;

namespace DungeonGeneration.SpatialLayout
{
    /// <summary>
    /// Stage 2: Converts the abstract graph into spatial dungeon geometry.
    /// Selects and executes layout algorithms based on config.
    /// </summary>
    public class SpatialLayoutStage : IGenerationStage
    {
        public string StageName => "Spatial Layout Generation";
        public int Priority => 200;
        private readonly Dictionary<LayoutAlgorithm, ISpatialLayoutAlgorithm> _algorithms;
        public SpatialLayoutStage()
        {
            _algorithms = new Dictionary<LayoutAlgorithm, ISpatialLayoutAlgorithm>
            {
                { LayoutAlgorithm.BSP, new BSPLayoutAlgorithm() },
                { LayoutAlgorithm.CellularAutomata, new CellularAutomataAlgorithm() }
            };
        }
        public void RegisterAlgorithm(LayoutAlgorithm type, ISpatialLayoutAlgorithm algorithm)
        {
            _algorithms[type] = algorithm;
        }
        public void Execute(GenerationContext context)
        {
            var config = context.Config;
            var rng = context.Random.Fork("spatial");
            var map = new SpatialMap(config.dungeonSize.x, config.dungeonSize.y);
            if (_algorithms.TryGetValue(config.primaryAlgorithm, out var algo))
            {
                algo.Generate(map, context.Graph, config, rng);
            }
            else
            {
                // Fallback to BSP
                _algorithms[LayoutAlgorithm.BSP].Generate(map, context.Graph, config, rng);
            }
            context.SpatialMap = map;
        }
    }
}
