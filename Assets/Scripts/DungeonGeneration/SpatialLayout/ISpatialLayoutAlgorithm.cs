using DungeonGeneration.Core;
using DungeonGeneration.Data;

namespace DungeonGeneration.SpatialLayout
{
    /// <summary>
    /// Interface for spatial layout algorithms.
    /// Each algorithm converts graph nodes into physical room geometry.
    /// </summary>
    public interface ISpatialLayoutAlgorithm
    {
        string AlgorithmName { get; }
        void Generate(SpatialMap map, DungeonGraph graph, DungeonConfig config, SeededRandom rng);
    }
}
