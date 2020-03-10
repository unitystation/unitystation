using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BulletMineOnHit : MonoBehaviour
{
	public void bulletEnter2D(Collision2D coll)
	{
		GameObject objectColl = coll.gameObject;

		InteractableTiles interTile = objectColl.GetComponent<InteractableTiles>();


		Transform cellTransform = coll.transform;
		MetaTileMap layerMetaTile = cellTransform.GetComponentInParent<MetaTileMap>();


		TileChangeManager tileChangeManager = cellTransform.GetComponentInParent<TileChangeManager>();


		ContactPoint2D firstContact = coll.GetContact(0);
		Vector2 dirOfForce = (firstContact.point - (Vector2)coll.transform.position).normalized;
		BulletHitInteract(dirOfForce, firstContact.point, layerMetaTile, tileChangeManager);
	}

	public void BulletHitInteract(Vector3 forceDir, Vector3 hitPos, MetaTileMap layerMetaTile, TileChangeManager tileChangeManager)
	{

		forceDir.z = 0;
		Vector3 bulletHitTarget = hitPos + (forceDir * 0.4f);
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
