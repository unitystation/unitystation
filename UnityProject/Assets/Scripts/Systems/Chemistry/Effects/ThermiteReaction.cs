using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chemistry.Effects
{
	[Serializable]
	[CreateAssetMenu(fileName = "ThermiteReaction", menuName = "ScriptableObjects/Chemistry/Effect/ThermiteReaction")]
	public class ThermiteReaction : Effect
	{
		[SerializeField] private float heatTemp = 19950f;

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
			var localPosition = sender.TileLocalPosition();
			var worldPosition = sender.AssumedWorldPosServer();
			if (reactionManager == null) return;

			foreach (var dir in directions)
			{
				reactionManager.ExposeHotspotWorldPosition(localPosition + dir, heatTemp, true);
				DamageWalls(sender.GetMatrixRoot(), 
					localPosition.To3Int() + dir.To3Int(), worldPosition.CutToInt());
			}

			_ = Despawn.ServerSingle(sender);
		}

		public override void HeatExposure(GameObject sender, float heat, ReagentMix inMix)
		{
			Apply(sender, inMix.Total);
		}

		private void DamageWalls(Matrix matrix, Vector3Int localPosition, Vector3Int worldPos)
		{
			if (matrix.TileChangeManager.MetaTileMap.HasTile(localPosition, LayerType.Walls) == false) return;
			matrix.TileChangeManager.MetaTileMap.ApplyDamage(
				localPosition,
				heatTemp / 100,
				worldPos, AttackType.Bomb);
		}
	}
}