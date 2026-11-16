using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;

namespace US13.Mobs.BrainAI.Actions
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "WaitMobInterval")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Wait For Mob Interval", story: "Wait for interval [Interval]", category: "Mob/Timing", id: "ad1dc1c1a2b34c5d8000000000000001")]
	public partial class WaitMobInterval : BrainMobState
	{
		// 0: wander, 1: search, 2: follow, 3: slime, 4: throw.
		[SerializeReference] public BlackboardVariable<int> Interval = new();
		private float until;
		protected override Status OnStart()
		{
			if (!CanAct || !AI.TryGetInterval(Interval.Value, out float seconds)) return Status.Failure;
			until = Time.time + Mathf.Max(0.02f, seconds);
			return Status.Running;
		}
		protected override Status OnUpdate() => !CanAct ? Status.Failure : Time.time >= until ? Status.Success : Status.Running;
	}
}
