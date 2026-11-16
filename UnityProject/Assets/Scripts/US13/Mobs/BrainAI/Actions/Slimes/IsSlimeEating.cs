using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using US13.Mobs.BrainAI.ContextExtensions;

namespace US13.Mobs.BrainAI.Actions.Slimes
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "IsSlimeEating")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Is Slime Eating", story: "Check whether the slime is eating", category: "Mob/Slime", id: "ad1dc1c1a2b34c5d8000000000000013")]
	[RequiresMobContextExtension(typeof(SlimeContextExtension))]
	public partial class IsSlimeEating : BrainMobState
	{
		protected override Status OnStart() => CanAct && Context.TryGetExtension<SlimeContextExtension>(out var slime) && slime.SlimeEat != null && slime.SlimeEat.CurrentlyEating ? Status.Success : Status.Failure;
	}
}
