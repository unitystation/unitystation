using Unity.Behavior;
using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Logs;
using US13.Mobs.BrainAI.Tasks;
using US13.Mobs.BrainAI.ContextExtensions;

namespace US13.Mobs.BrainAI
{
	/// <summary>A single cancellable task invocation, including awaited tasks owned by a graph action.</summary>
	public sealed class MobTaskExecution
	{
		private readonly MobBehaviorContext context;
		private readonly MobTask task;
		private readonly TaskContextExtension taskContext;
		private readonly int generation;
		private readonly float started;
		private readonly float duration;
		private bool cleaned;
		private readonly bool asynchronous;
		private CancellationTokenSource cancellation;
		private UniTask.Awaiter asyncAwaiter;
		private bool asyncConsumed;
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
			if (task is IAsyncMobTask asyncTask)
			{
				asynchronous = true;
				cancellation = new CancellationTokenSource();
				asyncAwaiter = asyncTask.ExecuteAsync(context, cancellation.Token).GetAwaiter();
				return;
			}
			duration = Mathf.Max(0.02f, task.Begin(context));
		}

		public Node.Status Tick(float now)
		{
			if (Status != Node.Status.Running) return Status;
			if (context.Generation != generation) { Cancel(); return Status; }
			if (asynchronous)
			{
				// Completion may have cleared its target cache. Consume it before rechecking validity.
				if (asyncAwaiter.IsCompleted)
				{
					if (ConsumeAsyncResult()) Finish();
					else { Status = Node.Status.Failure; Cancel(); }
				}
				else if (!task.IsValid(context)) Cancel();
				return Status;
			}
			if (!task.IsValid(context)) { Cancel(); return Status; }
			float elapsed = now - started;
			task.Tick(context, elapsed);
			if (elapsed < duration) return Status;
			Finish();
			return Status;
		}

		private void Finish()
		{
			task.Complete(context);
			taskContext.NearbySearch = true;
			taskContext.NextSearchTime = 0;
			Status = Node.Status.Success;
		}

		private bool ConsumeAsyncResult()
		{
			if (asyncConsumed) return false;
			asyncConsumed = true;
			try { asyncAwaiter.GetResult(); return true; }
			catch (OperationCanceledException) { return false; }
			catch (Exception error) { Loggy.Error($"Async mob task {task.GetType().Name} failed: {error}", Category.Mobs); return false; }
			finally { cancellation?.Dispose(); cancellation = null; }
		}

		public void Cancel()
		{
			if (cleaned) return;
			cleaned = true;
			if (Status == Node.Status.Running) Status = Node.Status.Failure;
			try { cancellation?.Cancel(); }
			finally
			{
				// Async work owns its cleanup in finally. A late stop must not cancel a newer invocation of the same task.
				if (!asynchronous) task?.Cancel();
				if (asynchronous && !asyncConsumed)
				{
					if (asyncAwaiter.IsCompleted) ConsumeAsyncResult();
					else asyncAwaiter.OnCompleted(() => ConsumeAsyncResult());
				}
			}
		}
	}
}
