using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DungeonGeneration.Core
{
    /// <summary>
    /// Executes generation stages in priority order.
    /// </summary>
    public class GenerationPipeline
    {
        private readonly List<IGenerationStage> _stages = new List<IGenerationStage>();
        public event Action<IGenerationStage, long> OnStageCompleted;
        public event Action<GenerationContext> OnPipelineCompleted;
        public void AddStage(IGenerationStage stage)
        {
            _stages.Add(stage);
            _stages.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }
        public void RemoveStage<T>() where T : IGenerationStage
        {
            _stages.RemoveAll(s => s is T);
        }
        public GenerationContext Execute(DungeonConfig config, int seed)
        {
            var context = new GenerationContext(config, seed);
            var totalSw = Stopwatch.StartNew();
            UnityEngine.Debug.LogWarning($"[DungeonGen] Pipeline start | seed={seed} | stages={_stages.Count}");
            foreach (var stage in _stages)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    stage.Execute(context);
                    sw.Stop();
                    UnityEngine.Debug.Log($"[DungeonGen] {stage.StageName} completed in {sw.ElapsedMilliseconds}ms");
                    OnStageCompleted?.Invoke(stage, sw.ElapsedMilliseconds);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[DungeonGen] {stage.StageName} FAILED: {e}");
                    throw;
                }
            }
            totalSw.Stop();
            UnityEngine.Debug.Log($"[DungeonGen] Pipeline done in {totalSw.ElapsedMilliseconds}ms");
            OnPipelineCompleted?.Invoke(context);
            return context;
        }
    }
}
