using System;
using UnityEngine;
using US13.HealthV2.Living;
using US13.Mobs.BrainAI.Tasks;
using Random = UnityEngine.Random;

namespace US13.Mobs.BrainAI.ContextExtensions
{
	[Serializable]
	public sealed class TaskContextExtension : MobBehaviorContextExtension, IMobMovementFailureHandler
	{
		[NonSerialized] public MobTask Task;
		[NonSerialized] public float NextSearchTime;
		[NonSerialized] public bool NearbySearch;

		public override IMobBehaviorContextExtension CreateRuntime() => new TaskContextExtension();
		public override void Bind(MobBehaviorContext context, LivingHealthMasterBase health)
		{
			base.Bind(context, health);
			Task = MobTask.Create(context.AI.Settings.Task.Kind);
		}
		public override void BeginRun()
		{
			NextSearchTime = 0;
			NearbySearch = false;
		}
		public override void Unbind()
		{
			Task = null;
			base.Unbind();
		}
		public void OnMovementFailed(MobBehaviorContext context)
		{
			var bounds = context.AI.Settings.Hesitation;
			NextSearchTime = Time.time + Random.Range(bounds.x, Mathf.Max(bounds.x, bounds.y) + 1) * context.AI.Settings.WanderInterval;
		}
	}
}
