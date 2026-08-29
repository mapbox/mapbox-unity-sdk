using Newtonsoft.Json.Linq;

namespace Mapbox.MapDebug.Scripts.Benchmark
{
    /// <summary>
    /// Interface for benchmark stats collectors. Each collector gathers a specific category
    /// of metrics (network, cache, tasks, tiles, system) with minimal per-frame overhead.
    /// Collectors can be used standalone as MonoBehaviours or driven by BenchmarkOrchestrator.
    /// </summary>
    public interface IBenchmarkStats
    {
        /// <summary>Reset counters and start recording.</summary>
        void Begin();

        /// <summary>Called every frame (from LateUpdate) to capture per-frame data.</summary>
        void SampleFrame();

        /// <summary>Stop recording.</summary>
        void End();

        /// <summary>Serialize all collected data to JSON. Called once after End().</summary>
        JObject Export();

        /// <summary>Optional one-line overlay text for on-screen display. Return empty to disable.</summary>
        string Overlay();
    }
}
