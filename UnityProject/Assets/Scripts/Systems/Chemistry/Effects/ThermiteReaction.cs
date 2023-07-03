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
		[SerializeField] private Reagent thermiteReagent;

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
			var Matrix =  sender.gameObject.GetMatrixRoot();
			var reactionManager = Matrix.ReactionManager;
			if (reactionManager == null) return;

			foreach (var dir in directions)
			{
				var pos = sender.TileLocalPosition() + dir;
				var worldPos = sender.AssumedWorldPosServer() + dir.To3Int();
				reactionManager.ExposeHotspotWorldPosition(pos, heatTemp, true);
				DamageWalls(sender.GetMatrixRoot(), pos.To3Int(), worldPos.CutToInt());
			}
		}

		public override void HeatExposure(GameObject sender, float heat, ReagentMix inMix)
		{
			var thermiteAmount = 0f;
			foreach (var reagent in inMix.reagents)
			{
				if (reagent.Key == thermiteReagent)
				{
					thermiteAmount += reagent.Value;
					inMix.Remove(reagent.Key, reagent.Value);
				}
			}
			Apply(sender, thermiteAmount);
		}

		private void DamageWalls(Matrix matrix, Vector3Int localPosition, Vector3Int worldPos)
		{
			if (matrix.TileChangeManager.MetaTileMap.HasTile(localPosition, LayerType.Walls) == false) return;
			matrix.TileChangeManager.MetaTileMap.ApplyDamage(
				localPosition,
				heatTemp / 100,
				worldPos);
		}
	}
}