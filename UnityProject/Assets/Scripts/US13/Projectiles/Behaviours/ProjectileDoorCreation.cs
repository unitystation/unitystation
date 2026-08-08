using UnityEngine;
using UnityEngine.Tilemaps;
using US13.Core.Lifecycle;
using US13.Managers.MatrixManager;
using US13.Objects.Doors;
using US13.Projectiles.Behaviours;
using US13.ScriptableObjects.Gun;
using US13.ScriptableObjects.Gun.HitConditions.Tile;
using US13.Tilemaps.Behaviours;
using US13.Tilemaps.Behaviours.Objects;
using US13.Tilemaps.Utils;
using Util;

public class ProjectileDoorCreation : MonoBehaviour, IOnHitInteractTile
{
		[SerializeField] private GameObject Door = null;

		public bool Interact(MatrixManager.CustomPhysicsHit hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			var LocalPosition = worldPosition.ToLocalInt(interactableTiles.Matrix);
			var wall = interactableTiles.MetaTileMap.GetTile(LocalPosition,
				LayerType.Walls);
			if (wall != null)
			{
				interactableTiles.MetaTileMap.RemoveTileWithlayer(LocalPosition, LayerType.Walls);
				var Spawnedoor =  Spawn.ServerPrefab(Door, LocalPosition.ToWorldInt(interactableTiles.Matrix));

				Spawnedoor.GameObject.GetComponent<DoorMasterController>().Open();
				Spawnedoor.GameObject.GetComponent<RegisterDoor>().isClosed = false;
				return true;
			}

			return false;
		}

}
