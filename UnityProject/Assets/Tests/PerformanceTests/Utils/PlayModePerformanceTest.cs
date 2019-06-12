using System.Collections;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Tests
{
	abstract class PlayModePerformanceTest : PlayModeTest
	{
		protected const int DefaultSamples = 100;
		protected float SettleSeconds = 2;

		protected const string FpsName = "FPS";
		protected const string TotalMemoryName = "TotalAllocatedMemory";

		protected abstract SampleGroupDefinition[] SampleGroupDefinitions { get; }

		protected WaitForSecondsRealtime Settle() => WaitFor.SecondsRealtime(SettleSeconds);

		protected IEnumerator UpdateBenchmark(int sampleCount = DefaultSamples)
		{
			if (SampleGroupDefinitions == null)
			{
				yield return CustomUpdateBenchmark(DefaultSamples);
				yield break;
			}
			using (Measure.ProfilerMarkers(SampleGroupDefinitions))
			{
				yield return CustomUpdateBenchmark(sampleCount);
			}
		}

		protected virtual IEnumerator CustomUpdateBenchmark(int sampleCount)
		{
			yield return new WaitWhile(LoopFunction);

			bool LoopFunction()
			{
				sampleCount--;
				return sampleCount > 0;
			}
		}
	}
}