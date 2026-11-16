using System;
using System.Linq;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using US13.HealthV2.Living;
using US13.Items;
using US13.Items.Food;
using US13.Items.Implants.Organs;
using Util;
using US13.Mobs.BrainAI.ContextExtensions;
using US13.Mobs.BrainAI.SettingsExtensions;

namespace US13.Mobs.BrainAI.Actions.Slimes
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "SelectSlimeFood")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Select Slime Food", story: "Select food or a nearby creature", category: "Mob/Targeting", id: "ad1dc1c1a2b34c5d8000000000000014")]
	[RequiresMobContextExtension(typeof(SlimeContextExtension))]
	[RequiresMobSettings(typeof(SlimeBehaviorSettings))]
	public partial class SelectSlimeFood : BrainMobState
	{
		protected override Status OnStart()
		{
			if (!CanAct || !AI.TryGetSettings<SlimeBehaviorSettings>(out _) || !Context.TryGetExtension<SlimeContextExtension>(out var slime) || slime.SlimeEat == null) return Status.Failure;
			Context.Target = null;
			var players = Physics2D.OverlapCircleAll(AI.Body.transform.position, 20f, AI.GetSettings<SlimeBehaviorSettings>().HitMask);
			if (players.Length == 0 || AI.GetSettings<SlimeBehaviorSettings>().Friendly)
			{
				var food = AI.Body.RegisterTile.Matrix.Get<ItemAttributesV2>(AI.Body.RegisterTile.LocalPositionServer, true)
					.FirstOrDefault(item => item != null && SweetExtensions.GetCachedComponent<Edible>((GameObject)item.gameObject, includeDisabled: false) != null);
				Context.Target = food != null ? food.gameObject : null;
			}
			else
			{
				foreach (var player in players)
				{
					var living = player.GetComponent<LivingHealthMasterBase>();
					if (living == null || living == Context.Health || living.IsDead || living.brain == null || living.brain.GetBodyPartBehavior<SlimeCore>() != null) continue;
					slime.GoodPlayers.TryGetValue(living.gameObject, out int familiarity);
					slime.GoodPlayers[living.gameObject] = ++familiarity;
					if (familiarity < 300 && Vector3.Distance(living.transform.position, AI.Body.transform.position) < 1.2f) { Context.Target = living.gameObject; break; }
				}
			}
			return Context.Target != null ? Status.Success : Status.Failure;
		}
	}
}
