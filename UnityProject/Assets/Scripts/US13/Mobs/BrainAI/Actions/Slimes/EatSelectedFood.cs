using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using US13.Mobs.BrainAI.ContextExtensions;
using US13.Mobs.BrainAI.SettingsExtensions;

namespace US13.Mobs.BrainAI.Actions.Slimes
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "EatSelectedFood")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Eat Selected Food", story: "Climb onto and eat the target", category: "Mob/Slime", id: "ad1dc1c1a2b34c5d8000000000000015")]
	[RequiresMobContextExtension(typeof(SlimeContextExtension))]
	[RequiresMobSettings(typeof(SlimeBehaviorSettings))]
	public partial class EatSelectedFood : BrainMobState
	{
		protected override Status OnStart()
		{
			if (!CanAct || !AI.TryGetSettings<SlimeBehaviorSettings>(out _) || !Context.TryGetExtension<SlimeContextExtension>(out var slime) || Context.Target == null || slime.SlimeEat == null || Context.Target.GetComponent<US13.Core.Physics.UniversalObjectPhysics>() == null) return Status.Failure;
			slime.SlimeEat.ClimbAndEat(Context.Target);
			if (!slime.SlimeEat.CurrentlyEating) return Status.Failure;
			MobDialogue.Sound(AI, AI.GetSettings<SlimeBehaviorSettings>().EatFoodSound);
			return Status.Success;
		}
	}
}
