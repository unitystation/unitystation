using Tiles;
using UnityEngine;

namespace ScriptableObjects.Gun.HitConditions.Tile
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