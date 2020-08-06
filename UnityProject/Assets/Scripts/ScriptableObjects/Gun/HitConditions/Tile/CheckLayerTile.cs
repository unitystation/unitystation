using System.Linq;
using UnityEngine;

namespace ScriptableObjects.Gun.HitConditions.Tile
{
	[CreateAssetMenu(fileName = "CheckTileLayerTypes", menuName = "ScriptableObjects/Gun/HitConditions/Tile/CheckLayerTile", order = 0)]
	public class CheckLayerTile : HitInteractTileCondition
	{
		[SerializeField] private LayerTile[] layerTiles;

		public override bool CheckCondition(RaycastHit2D hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			return CheckLayerTileAtPosition(interactableTiles, worldPosition);
		}

		/// <summary>
		/// Checks if any tile in this world position is in the list
		/// </summary>
		/// <param name="interactableTiles"></param>
		/// <param name="worldPosition"></param>
		/// <returns></returns>
		private bool CheckLayerTileAtPosition(InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			//TODO: Improve this iteration by getting all Tiles from position and checking if they are in the SO list.
			// Currently it gets tile from world position each time it checks the list which is not efficient
			// if there will be a big list of tiles to check from and they would be of the same LayerType
			return layerTiles.Any(lt => interactableTiles.MetaTileMap.GetTileAtWorldPos(worldPosition, lt.LayerType) == lt);
		}
	}
}