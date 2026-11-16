using System;
using UnityEngine;
using US13.HealthV2.Living;
using US13.Mobs.BrainAI.Tasks;
using US13.Mobs.BrainAI.SettingsExtensions;
using Random = UnityEngine.Random;

namespace US13.Mobs.BrainAI.ContextExtensions
{
	[Serializable]
	[RequiresMobSettings(typeof(BotTaskSettings))]
	public sealed class TaskContextExtension : MobBehaviorContextExtension, IMobMovementFailureHandler
	{
		[NonSerialized] public MobTask Task;
		[NonSerialized] public float NextSearchTime;
		[NonSerialized] public bool NearbySearch;
		[NonSerialized] public LivingHealthMasterBase TargetHealth;

		public override IMobBehaviorContextExtension CreateRuntime() => new TaskContextExtension();
		public override void Bind(MobBehaviorContext context, LivingHealthMasterBase health)
		{
			base.Bind(context, health);
			Task = context.AI.TryGetSettings<BotTaskSettings>(out var settings) ? MobTask.Create(settings.Kind) : null;
		}
		public override void BeginRun()
		{
			NextSearchTime = 0;
			NearbySearch = false;
			TargetHealth = null;
		}
		public override void EndRun() => TargetHealth = null;
		public override void Unbind()
		{
			TargetHealth = null;
			Task = null;
			base.Unbind();
		}
		public void OnMovementFailed(MobBehaviorContext context)
		{
			TargetHealth = null;
			var bounds = context.AI.Settings.Hesitation;
			NextSearchTime = Time.time + Random.Range(bounds.x, Mathf.Max(bounds.x, bounds.y) + 1) * context.AI.Settings.WanderInterval;
		}
	}
}
