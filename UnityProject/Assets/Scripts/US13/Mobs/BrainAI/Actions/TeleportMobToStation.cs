using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using US13.Managers.NetworkManagement;
using Util;
using US13.Mobs.BrainAI.SettingsExtensions;

namespace US13.Mobs.BrainAI.Actions
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "TeleportMobToStation")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Teleport To Station", story: "Try teleporting to a random station location", category: "Mob/Movement", id: "ad1dc1c1a2b34c5d8000000000000024")]
	[RequiresMobSettings(typeof(ArialBehaviorSettings))]
	public partial class TeleportMobToStation : BrainMobState
	{
		protected override Status OnStart()
		{
			if (!CanAct || !AI.TryGetSettings<ArialBehaviorSettings>(out _) || !DMMath.Prob(9)) return Status.Failure;
			var points = new List<Transform>();
			foreach (var category in new[] { SpawnPointCategory.Bartender, SpawnPointCategory.Assistant, SpawnPointCategory.Botanist, SpawnPointCategory.Chaplain, SpawnPointCategory.Cook })
				points.AddRange(SpawnPoint.GetPointsForCategory(category).Where(point => point != null));
			if (points.Count == 0 || Context.Health.playerScript?.playerMove == null) return Status.Failure;
			Context.Health.playerScript.playerMove.SetTransform(points.PickRandom().gameObject.AssumedWorldPosServer(), true);
			if (AI.GetSettings<ArialBehaviorSettings>().EnterSounds.Count > 0) MobDialogue.Sound(AI, AI.GetSettings<ArialBehaviorSettings>().EnterSounds.PickRandom());
			Context.ClearTarget();
			Context.DesiredTarget = null;
			return Status.Success;
		}
	}
}
