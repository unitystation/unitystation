using System;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

namespace Chemistry.Effects
{
	[Serializable]
	[CreateAssetMenu(fileName = "ThermiteReaction", menuName = "ScriptableObjects/Chemistry/Effect/ThermiteReaction")]
	public class ThermiteReaction : Effect
	{
		[SerializeField] private float heatTemp = 19950f;

		private List<Vector3Int> directions = new List<Vector3Int>()
		{
			Vector3Int.zero,
			Vector3Int.down,
			Vector3Int.up,
			Vector3Int.left,
			Vector3Int.right
		};

		public override void Apply(GameObject sender, float amount)
		{
			var matrix = sender.gameObject.GetMatrixRoot();
			var reactionManager = matrix.ReactionManager;
			var localPosition = sender.TileLocalPosition().To3Int();
			var worldPosition = sender.AssumedWorldPosServer();
			if (reactionManager == null) return;

			foreach (var dir in directions)
			{
				reactionManager.ExposeHotspotWorldPosition(localPosition + dir, heatTemp, true);
				Damage(sender.GetMatrixRoot(),
					localPosition + dir, worldPosition.CutToInt());
			}

			Chat.AddActionMsgToChat(sender,
				"<color=red>The reagent lets out a fizzling sound before letting out extreme heat to everything around it.</color>");
			_ = Despawn.ServerSingle(sender);
		}

		public override void HeatExposure(GameObject sender, float heat, ReagentMix inMix)
		{
			Apply(sender, inMix.Total);
		}

		private void Damage(Matrix matrix, Vector3Int localPosition, Vector3Int worldPos)
		{
			var mobs = matrix.Get<LivingHealthMasterBase>(localPosition, CustomNetworkManager.IsServer);
			foreach (var mob in mobs)
			{
				mob.ApplyDamageAll(null, heatTemp / 200, AttackType.Fire, DamageType.Burn);
				mob.ChangeFireStacks(mob.FireStacks + 8f);
			}
			matrix.TileChangeManager.MetaTileMap.ApplyDamage(
				localPosition,
				heatTemp / 100,
				worldPos, AttackType.Bomb);
		}
	}
}