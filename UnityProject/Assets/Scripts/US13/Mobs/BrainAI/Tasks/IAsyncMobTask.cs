using System.Threading;
using Cysharp.Threading.Tasks;

namespace US13.Mobs.BrainAI.Tasks
{
	/// <summary>A task whose completion is determined by awaited work rather than a fixed graph timer.</summary>
	public interface IAsyncMobTask
	{
		// Observe cancellation and release invocation-owned resources in finally; the synchronous Cancel hook is not used.
		UniTask ExecuteAsync(MobBehaviorContext context, CancellationToken cancellationToken);
	}
}
