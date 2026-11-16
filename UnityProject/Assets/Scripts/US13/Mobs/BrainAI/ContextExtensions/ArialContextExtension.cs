using System;

namespace US13.Mobs.BrainAI.ContextExtensions
{
	[Serializable]
	public sealed class ArialContextExtension : MobBehaviorContextExtension, IMobFollowPolicy
	{
		[NonSerialized] public int ActivityTicks;
		[NonSerialized] public bool IsStalking = true;
		public bool CanFollowTarget => IsStalking;
		public override IMobBehaviorContextExtension CreateRuntime() => new ArialContextExtension();
	}
}
