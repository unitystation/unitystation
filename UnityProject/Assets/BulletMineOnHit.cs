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
		if (objectColl == null) Chat.AddGameWideSystemMsgToChat("It's an object.");
		if (objectColl != null) Chat.AddGameWideSystemMsgToChat("objectColl != nul");
		InteractableTiles interTile = objectColl.GetComponent<InteractableTiles>();
		if (interTile == null) Chat.AddGameWideSystemMsgToChat("InterTile == null.");
		if (interTile != null) Chat.AddGameWideSystemMsgToChat("InterTile != null.");
		Chat.AddGameWideSystemMsgToChat("OnCollisionEnter2D is called");
		Transform cellTransform = coll.transform;
		MetaTileMap layerMetaTile = cellTransform.GetComponentInParent<MetaTileMap>();
		if (layerMetaTile == null) Chat.AddGameWideSystemMsgToChat("layerMetaTile == null");

		TileChangeManager tileChangeManager = cellTransform.GetComponentInParent<TileChangeManager>();
		if (tileChangeManager == null) Chat.AddGameWideSystemMsgToChat("tileChangeManager == null");

		ContactPoint2D firstContact = coll.GetContact(0);
		Vector2 dirOfForce = (firstContact.point - (Vector2)coll.transform.position).normalized;
		DetermineAction(dirOfForce, firstContact.point, layerMetaTile, tileChangeManager);
	}

	private void DetermineAction(Vector2 forceDirection, Vector3 hitPos, MetaTileMap layerMetaTile, TileChangeManager tileChangeManager)
	{
		BulletHitInteract(forceDirection, hitPos, layerMetaTile, tileChangeManager);
	}

	public void BulletHitInteract(Vector3 forceDir, Vector3 hitPos, MetaTileMap layerMetaTile, TileChangeManager tileChangeManager)
	{

		forceDir.z = 0;
		Vector3 bulletHitTarget = hitPos + (forceDir * 0.2f);
		Vector3Int cellPos = layerMetaTile.WorldToCell(Vector3Int.RoundToInt(bulletHitTarget));
		Chat.AddGameWideSystemMsgToChat("BulletHitInteract was called.");

		LayerTile getTile = layerMetaTile.GetTile(cellPos, LayerType.Walls);
		if (getTile == null) Chat.AddGameWideSystemMsgToChat("getTile == null");
		if (getTile != null)
		{
			Chat.AddGameWideSystemMsgToChat("You hit layer Wall.");
			if (Validations.IsMineableAt(bulletHitTarget, layerMetaTile))
			{

				Chat.AddGameWideSystemMsgToChat("Minable!");
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
