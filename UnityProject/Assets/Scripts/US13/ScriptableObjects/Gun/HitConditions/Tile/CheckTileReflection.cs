using UnityEngine;
using US13.Managers.MatrixManager;
using US13.Tilemaps.Behaviours;
using US13.Tilemaps.Tiles;
using US13.Tilemaps.Utils;

namespace US13.ScriptableObjects.Gun.HitConditions.Tile
{
	[CreateAssetMenu(fileName = "CheckTileReflection", menuName = "ScriptableObjects/Gun/HitConditions/Tile/CheckTileReflection", order = 0)]
	public class CheckTileReflection : HitInteractTileCondition
	{
		/// <summary>
		/// Really simple check for determining if the wall can reflect bullet
		/// Hard coded only to check wall LayerType
		/// </summary>
		/// <param name="hit"></param>
		/// <param name="interactableTiles"></param>
		/// <param name="worldPosition"></param>
		/// <returns></returns>
		public override bool CheckCondition(MatrixManager.CustomPhysicsHit hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			var tile = interactableTiles.MetaTileMap.GetTileAtWorldPos(worldPosition, LayerType.Walls) as BasicTile;
			return tile != null && tile.DoesReflectBullet;
		}
	}
}