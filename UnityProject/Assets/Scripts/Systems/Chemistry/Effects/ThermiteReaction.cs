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
		[SerializeField] private float heatTemp = 12950f;

		private List<Vector2Int> directions = new List<Vector2Int>()
		{
			Vector2Int.zero,
			Vector2Int.down,
			Vector2Int.up,
			Vector2Int.left,
			Vector2Int.right
		};

		public override void Apply(GameObject sender, float amount)
		{
			var matrix = sender.gameObject.GetMatrixRoot();
			var reactionManager = matrix.ReactionManager;
			var worldPosition = sender.TileWorldPosition();
			if (reactionManager == null) return;

			for (int i = 0; i < directions.Count; i++)
			{
				var dir = directions[i];
				reactionManager.ExposeHotspotWorldPosition(worldPosition + dir, heatTemp, true);
				Damage(sender.GetMatrixRoot(), worldPosition + directions[i], sender.AssumedWorldPosServer().RoundToInt() + dir.To3Int());
			}

			Chat.AddActionMsgToChat(sender,
				"<color=red>The reagent lets out a fizzling sound before letting out extreme heat to everything around it.</color>");
			_ = Despawn.ServerSingle(sender);
		}

		public override void HeatExposure(GameObject sender, float heat, ReagentMix inMix)
		{
			Apply(sender, inMix.Total);
		}

		private void Damage(Matrix matrix, Vector2Int localPosition, Vector3Int worldPos)
		{
			var mobs = matrix.Get<LivingHealthMasterBase>(localPosition.To3Int(), CustomNetworkManager.IsServer);
			foreach (var mob in mobs)
			{
				mob.ApplyDamageAll(null, heatTemp / 400, AttackType.Fire, DamageType.Burn);
				mob.ChangeFireStacks(mob.FireStacks + 8f);
			}
			matrix.TileChangeManager.MetaTileMap.ApplyDamage(
				localPosition.To3Int(),
				heatTemp / 100,
				worldPos, AttackType.Bomb);
		}
	}
}