using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Unity.Behavior;
using US13.Mobs.BrainAI;
using US13.Mobs.BrainAI.ContextExtensions;
using US13.Mobs.BrainAI.Tasks;

namespace Tests
{
	public class AsyncMobTaskTests
	{
		private sealed class AwaitedTask : MobTask, IAsyncMobTask
		{
			public readonly UniTaskCompletionSource Source = new();
			public CancellationToken Token;
			public int Effects, Completed;
			public bool Valid = true;
			public override bool FindTarget(MobBehaviorContext context, int radius) => true;
			public override bool IsValid(MobBehaviorContext context) => Valid;
			public async UniTask ExecuteAsync(MobBehaviorContext context, CancellationToken cancellationToken)
			{
				Token = cancellationToken;
				await Source.Task.AttachExternalCancellation(cancellationToken);
				Effects++;
			}
			public override void Complete(MobBehaviorContext context) => Completed++;
		}

		private static MobBehaviorContext Create(AwaitedTask task)
		{
			var context = new MobBehaviorContext(new BrainMobAI { ContextExtensions = new() { new TaskContextExtension() } });
			context.TryGetExtension<TaskContextExtension>(out var tasks); tasks.Task = task;
			return context;
		}

		[Test]
		public void GraphWaitsForAsyncCompletionInsteadOfItsDuration()
		{
			var task = new AwaitedTask(); var context = Create(task);
			var execution = new MobTaskExecution(context, 0);
			Assert.That(execution.Tick(1000), Is.EqualTo(Node.Status.Running));
			Assert.That(task.Completed, Is.Zero);
			task.Source.TrySetResult();
			Assert.That(execution.Tick(1001), Is.EqualTo(Node.Status.Success));
			Assert.That(execution.Tick(1002), Is.EqualTo(Node.Status.Success));
			Assert.That(task.Completed, Is.EqualTo(1));
			execution.Cancel();
		}

		[Test]
		public void CancelledAsyncWorkCannotApplyDelayedEffects()
		{
			var task = new AwaitedTask(); var context = Create(task);
			var execution = new MobTaskExecution(context, 0);
			execution.Cancel(); execution.Cancel();
			Assert.That(task.Token.IsCancellationRequested, Is.True);
			task.Source.TrySetResult();
			Assert.That(execution.Tick(100), Is.EqualTo(Node.Status.Failure));
			Assert.That(task.Effects, Is.Zero); Assert.That(task.Completed, Is.Zero);
		}

		[Test]
		public void InvalidatingTheContextCancelsItsAsyncExecution()
		{
			var task = new AwaitedTask(); var context = Create(task);
			var execution = new MobTaskExecution(context, 0);
			context.EndRun(); context.BeginRun();
			Assert.That(execution.Tick(100), Is.EqualTo(Node.Status.Failure));
			Assert.That(task.Token.IsCancellationRequested, Is.True);
			task.Source.TrySetResult();
			Assert.That(task.Effects, Is.Zero);
		}
	}
}
