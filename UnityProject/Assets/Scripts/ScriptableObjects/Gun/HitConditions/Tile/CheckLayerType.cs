using System.Linq;
using UnityEngine;

namespace ScriptableObjects.Gun.HitConditions.Tile
{
	[CreateAssetMenu(fileName = "CheckLayerType", menuName = "ScriptableObjects/Gun/HitConditions/Tile/CheckLayerType", order = 0)]
	public class CheckLayerType : HitInteractTileCondition
	{
		[SerializeField] private LayerType[] layerTypes = default;
    
		public override bool CheckCondition(RaycastHit2D hit, InteractableTiles interactableTiles, Vector3 worldPosition)
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
