using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BulletMineOnHit : MonoBehaviour
{
	//Script does not work on asteroids but mines AsteroidStation ore
	public void BulletHitInteract(Collision2D coll, Vector2 Direction)
	{
		Transform cellTransform = coll.transform;
		MetaTileMap layerMetaTile = cellTransform.GetComponentInParent<MetaTileMap>();
		TileChangeManager tileChangeManager = cellTransform.GetComponentInParent<TileChangeManager>();

		ContactPoint2D firstContact = coll.GetContact(0);
		Vector3 hitPos = firstContact.point;
		Vector3 forceDir = Direction;
		forceDir.z = 0;
		Vector3 bulletHitTarget = hitPos + (forceDir *0.2f);
		Vector3Int cellPos = layerMetaTile.WorldToCell(Vector3Int.RoundToInt(bulletHitTarget));


		LayerTile getTile = layerMetaTile.GetTile(cellPos, LayerType.Walls);

		if (getTile != null)
		{
			if (Validations.IsMineableAt(bulletHitTarget, layerMetaTile))
			{

				SoundManager.PlayNetworkedAtPos("BreakStone", bulletHitTarget);
				var tile = getTile as BasicTile;
				Spawn.ServerPrefab(tile.SpawnOnDeconstruct, bulletHitTarget, count: tile.SpawnAmountOnDeconstruct);
				tileChangeManager.RemoveTile(cellPos, LayerType.Walls);
				tileChangeManager.RemoveTile(cellPos, LayerType.Effects);
			}
			return;
		}

	}
}
