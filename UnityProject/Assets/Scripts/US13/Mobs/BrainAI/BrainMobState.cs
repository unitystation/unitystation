using System;
using UnityEngine;
using US13.HealthV2.Living;

namespace US13.Mobs.BrainAI
{
	/// <summary>Shared context for graph actions, without an organ lifecycle or independent scheduler.</summary>
	[Serializable]
	public abstract class BrainMobState : Unity.Behavior.Action
	{
		protected BrainMobAI AI => GameObject.GetBodyPartBehavior<BrainMobAI>();
		protected MobBehaviorContext Context => AI?.Context;
		protected bool CanAct => AI != null && AI.CanRun;
	}
}
