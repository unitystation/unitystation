using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using Util;
using US13.Mobs.BrainAI.ContextExtensions;

namespace US13.Mobs.BrainAI.Actions.Arial
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "PreparePrankApproach")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Prepare Prank Approach", story: "Prepare to approach a prank target", category: "Mob/Targeting", id: "ad1dc1c1a2b34c5d8000000000000023")]
	[RequiresMobContextExtension(typeof(ArialContextExtension))]
	public partial class PreparePrankApproach : BrainMobState
	{
		protected override Status OnStart()
		{
			if (!CanAct || !Context.TryGetExtension<ArialContextExtension>(out var arial) || Context.DesiredTarget == null || arial.IsStalking) return Status.Failure;
			Context.Target = Context.DesiredTarget;
			Context.TargetWasObject = true;
			Context.TargetRange = 3.75f;
			Context.TargetMatrix = AI.Body.RegisterTile.Matrix;
			Context.TargetCell = Context.Target.AssumedWorldPosServer().ToLocalInt(Context.TargetMatrix);
			Context.Path = null;
			return Status.Success;
		}
	}
}
