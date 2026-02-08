using System;

internal class DisposableProfiler : IDisposable
{
	public DisposableProfiler(string iSampleName)
	{
		UnityEngine.Profiling.Profiler.BeginSample(iSampleName);
	}

	public void Dispose()
	{
		UnityEngine.Profiling.Profiler.EndSample();
	}
}