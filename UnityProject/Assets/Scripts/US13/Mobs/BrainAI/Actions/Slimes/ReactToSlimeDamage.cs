using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using US13.Mobs.BrainAI.Actions.Slimes;
using US13.Mobs.BrainAI.ContextExtensions;

namespace US13.Mobs.BrainAI
{
	[Serializable, GeneratePropertyBag, NodeDescription(name: "React To Slime Damage", story: "React to recent damage", category: "Mob/Slime", id: "ad1dc1c1a2b34c5d8000000000000012")]
	[RequiresMobContextExtension(typeof(SlimeContextExtension))]
	public partial class ReactToSlimeDamage : BrainMobState
	{
		protected override Status OnStart()
		{
			if (!CanAct || !Context.TryGetExtension<SlimeContextExtension>(out var slime) || Context.RecentDamage <= 0) return Status.Failure;
			if (Context.RecentDamage > 5)
			{
				slime.SlimeEat?.StopEating();
				RandomMobStep.Push(AI);
			}
			Context.RecentDamage = Mathf.Max(0, Context.RecentDamage - 1);
			return Status.Success;
		}
	}
}
