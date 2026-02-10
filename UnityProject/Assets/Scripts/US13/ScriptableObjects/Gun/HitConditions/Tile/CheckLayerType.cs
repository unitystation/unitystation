using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.Managers.MatrixManager;
using US13.Tilemaps.Behaviours;
using US13.Tilemaps.Utils;

namespace US13.ScriptableObjects.Gun.HitConditions.Tile
{
	[CreateAssetMenu(fileName = "CheckLayerType", menuName = "ScriptableObjects/Gun/HitConditions/Tile/CheckLayerType", order = 0)]
	public class CheckLayerType : HitInteractTileCondition
	{
		[SerializeField] private List<LayerType>  layerTypes = new List<LayerType>();

		public override bool CheckCondition(MatrixManager.CustomPhysicsHit hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			var layers = interactableTiles.MetaTileMap.DamageableLayers;
			foreach (var layer in layers)
			{
				if (CheckType(layer.LayerType)) return true;
			}

			return false;
		}

		public bool CheckType(LayerType layerType) => layerTypes.Any(l => l == layerType);
	}
}
