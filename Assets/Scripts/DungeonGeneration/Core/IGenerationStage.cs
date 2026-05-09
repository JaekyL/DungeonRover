namespace DungeonGeneration.Core
{
    /// <summary>
    /// Interface for a single stage in the dungeon generation pipeline.
    /// Each stage consumes and produces data via the shared GenerationContext.
    /// </summary>
    public interface IGenerationStage
    {
        string StageName { get; }
        int Priority { get; }
        void Execute(GenerationContext context);
    }
}

