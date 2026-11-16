using Unity.Behavior;
using UnityEngine;
using US13.Mobs.BrainAI.Tasks;
using US13.Mobs.BrainAI.ContextExtensions;

namespace US13.Mobs.BrainAI
{
	/// <summary>A single cancellable task invocation. Time is supplied by the graph; no detached work survives it.</summary>
	public sealed class MobTaskExecution
	{
		private readonly MobBehaviorContext context;
		private readonly MobTask task;
		private readonly TaskContextExtension taskContext;
		private readonly int generation;
		private readonly float started;
		private readonly float duration;
		private bool cleaned;
		public Node.Status Status { get; private set; }

		public MobTaskExecution(MobBehaviorContext context, float now)
		{
			this.context = context;
			context.TryGetExtension(out taskContext);
			task = taskContext?.Task;
			generation = context.Generation;
			started = now;
			if (task == null || !task.IsValid(context)) { Status = Node.Status.Failure; return; }
			Status = Node.Status.Running;
			duration = Mathf.Max(0.02f, task.Begin(context));
		}

		public Node.Status Tick(float now)
		{
			if (Status != Node.Status.Running) return Status;
			if (context.Generation != generation || !task.IsValid(context)) return Status = Node.Status.Failure;
			float elapsed = now - started;
			task.Tick(context, elapsed);
			if (elapsed < duration) return Status;
			task.Complete(context);
			taskContext.NearbySearch = true;
			taskContext.NextSearchTime = 0;
			return Status = Node.Status.Success;
		}

		public void Cancel()
		{
			if (cleaned) return;
			cleaned = true;
			task?.Cancel();
			if (Status == Node.Status.Running) Status = Node.Status.Failure;
		}
	}
}
