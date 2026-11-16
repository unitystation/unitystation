using UnityEngine;
using US13.Core;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Tilemaps.Utils;
using US13.UI.Objects.Security.SecurityRecordsConsole;
using US13.UI.Systems.Jobs;
using Util;

namespace US13.Mobs.BrainAI.Tasks
{
	public sealed class SecurityMobTask : MobTask
	{
		public override bool FindTarget(MobBehaviorContext context, int radius)
		{
			var targets = ComponentsTracker<LivingHealthMasterBase>.GetAllNearbyTypesToTarget(context.AI.Body.gameObject, radius, bypassInventories: false);
			if (targets == null) return false;
			var matrix = MatrixOf(context);
			foreach (var living in targets)
			{
				if (living == null || living == context.Health || living.playerScript == null || living.playerScript.PlayerSync.IsCuffed) continue;
				bool wanted = context.AI.IsEmagged;
				if (!wanted && CrewManifestManager.Instance != null)
					foreach (var record in CrewManifestManager.Instance.SecurityRecords)
						if (record.characterSettings.Name == living.playerScript.visibleName && (record.Status == SecurityStatus.Criminal || record.Status == SecurityStatus.Arrest)) { wanted = true; break; }
				if (wanted && Select(context, living.gameObject.AssumedWorldPosServer().ToLocalInt(matrix), matrix, living.gameObject)) return true;
			}
			return false;
		}
		public override bool IsValid(MobBehaviorContext context) => context.Target != null && context.Target.GetComponent<LivingHealthMasterBase>()?.playerScript != null &&
		                                                            Vector3.Distance(context.Target.AssumedWorldPosServer(), context.AI.Body.gameObject.AssumedWorldPosServer()) <= Range;
		public override float Begin(MobBehaviorContext context)
		{
			var settings = context.AI.Settings.Task;
			var storage = settings.InternalStorage;
			var baton = storage?.GetIndexedItemSlot(0)?.ItemAttributes;
			if (baton == null) return base.Begin(context);
			var player = context.Target.GetComponent<LivingHealthMasterBase>().playerScript;
			if (player.RegisterPlayer.IsLayingDown && !player.playerMove.IsCuffed)
			{
				foreach (var slot in storage.GetOccupiedSlots())
				{
					var item = slot.ItemAttributes;
					if (item == null || item == baton || !item.HasTrait(settings.HandcuffTrait)) continue;
					player.playerMove.Cuff(item.gameObject, player.gameObject);
					return settings.Duration * 1.5f;
				}
			}
			var relative = player.AssumedWorldPos - context.Health.playerScript.AssumedWorldPos;
			context.Health.playerScript.WeaponNetworkActions.ServerPerformMeleeAttack(player.gameObject, relative.To2().normalized, BodyPartType.Chest, LayerType.None, baton, true);
			return base.Begin(context);
		}
		public override void Complete(MobBehaviorContext context) { }
	}
}