using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using US13.Core;
using US13.HealthV2.Living;
using US13.Items.Food;
using Util;
using US13.Mobs.BrainAI.ContextExtensions;
using US13.Mobs.BrainAI.SettingsExtensions;

namespace US13.Mobs.BrainAI.Actions.Arial
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "ChooseArialTarget")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Choose Arial Target", story: "Choose a target and activity", category: "Mob/Targeting", id: "ad1dc1c1a2b34c5d8000000000000021")]
	[RequiresMobContextExtension(typeof(ArialContextExtension))]
	[RequiresMobSettings(typeof(ArialBehaviorSettings))]
	public partial class ChooseArialTarget : BrainMobState
	{
		private bool announced;
		protected override Status OnStart()
		{
			if (!CanAct || !AI.TryGetSettings<ArialBehaviorSettings>(out _) || !Context.TryGetExtension<ArialContextExtension>(out var arial)) return Status.Failure;
			if (!announced)
			{
				announced = true;
				if (AI.GetSettings<ArialBehaviorSettings>().EnterSounds.Count > 0) MobDialogue.Sound(AI, AI.GetSettings<ArialBehaviorSettings>().EnterSounds.PickRandom());
			}
			if (++arial.ActivityTicks > 50) { arial.ActivityTicks = 0; arial.IsStalking = DMMath.Prob(50); }
			var existing = Context.DesiredTarget;
			if (existing != null && existing.activeInHierarchy &&
				(!existing.TryGetComponent<LivingHealthMasterBase>(out var health) || !health.IsDead)) return Status.Success;
			Context.DesiredTarget = null;
			foreach (var player in Context.Health.RegisterTile.Matrix.PresentPlayers)
			{
				if (player == AI.Traversal?.Mob?.RegisterPlayer || player.PlayerScript?.playerHealth == null || player.PlayerScript.playerHealth.IsDead) continue;
				if (Vector3.Distance(player.gameObject.AssumedWorldPosServer(), AI.Body.gameObject.AssumedWorldPosServer()) >= 12) continue;
				Context.DesiredTarget = player.gameObject;
				return Status.Success;
			}
			var edibles = ComponentsTracker<Edible>.GetAllNearbyTypesToTarget(AI.Body.gameObject, 20, false);
			Context.DesiredTarget = edibles?.Count > 5 ? edibles.PickRandom()?.gameObject : null;
			return Context.DesiredTarget != null ? Status.Success : Status.Failure;
		}
	}
}
