using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using US13.Mobs.BrainAI.ContextExtensions;

namespace US13.Mobs.BrainAI.Actions.Slimes
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "SplitSlime")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Split Slime", story: "Split if the slime is ready", category: "Mob/Slime", id: "ad1dc1c1a2b34c5d8000000000000011")]
	[RequiresMobContextExtension(typeof(SlimeContextExtension))]
	public partial class SplitSlime : BrainMobState
	{
		protected override Status OnStart()
		{
			if (!CanAct || !Context.TryGetExtension<SlimeContextExtension>(out var slime) || slime.SlimeCore == null || slime.SlimeEat == null || !slime.SlimeCore.CanSlimesSplit()) return Status.Failure;
			slime.SlimeEat.StopEating();
			slime.SlimeCore.SlimesSplit();
			return Status.Success;
		}
	}
}
