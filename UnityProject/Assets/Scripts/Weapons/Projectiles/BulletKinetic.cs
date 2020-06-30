using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class BulletKinetic : BulletBehaviour
{


	//to control Kinetic animation with it
	private bool isOnDespawn = false;

	public override void Shoot(Vector2 dir, GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest)
	{
		base.Shoot(dir, controlledByPlayer, fromWeapon, targetZone);
		StartCoroutine(countTiles());
	}

	public override void HandleCollisionEnter2D(Collision2D coll)
	{
		//This one is not working right now as intended
		//Bullet get the wall tiles of the station
		//but doesn't mine asteroid walls
		//it just collides with them
		//GetComponent<BulletMineOnHit>()?.BulletHitInteract(coll,Direction);
		ReturnToPool(coll);
	}

	protected override void DespawnThis()
	{
		if (!isOnDespawn)
		{

			isOnDespawn = true;
			if (trailRenderer != null)
			{
				trailRenderer.ShotDone();
			}

			rigidBody.velocity = Vector2.zero;
			StartCoroutine(KineticAnim());
		}

	}

	protected void ReturnToPool(Collision2D coll)
	{
		if (!isOnDespawn)
		{
			isOnDespawn = true;
			if (trailRenderer != null)
			{
				trailRenderer.ShotDone();
			}

			rigidBody.velocity = Vector2.zero;
			StartCoroutine(KineticAnim(coll));
		}
	}

	public IEnumerator KineticAnim()
	{

		Transform cellTransform = rigidBody.gameObject.transform;
		MetaTileMap layerMetaTile = cellTransform.GetComponentInParent<MetaTileMap>();
		var position = layerMetaTile.WorldToCell(Vector3Int.RoundToInt(rigidBody.gameObject.AssumedWorldPosServer()));

		TileChangeManager tileChangeManager = transform.GetComponentInParent<TileChangeManager>();

		// Store the old effect
		LayerTile oldEffectLayerTile = tileChangeManager.GetLayerTile(position, LayerType.Effects);

		tileChangeManager.UpdateTile(position, TileType.Effects, "KineticAnimation");

		yield return WaitFor.Seconds(.4f);

		tileChangeManager.RemoveTile(position, LayerType.Effects);

		// Restore the old effect if any (ex: cracked glass, does not work)
		if (oldEffectLayerTile)
			tileChangeManager.UpdateTile(position, oldEffectLayerTile);
		isOnDespawn = false;
		global::Despawn.ClientSingle(gameObject);
	}

	public IEnumerator KineticAnim(Collision2D coll)
	{

		Transform cellTransform = rigidBody.gameObject.transform;
		MetaTileMap layerMetaTile = cellTransform.GetComponentInParent<MetaTileMap>();

		ContactPoint2D firstContact = coll.GetContact(0);
		Vector3 hitPos = firstContact.point;
		Vector3 forceDir = Direction;
		forceDir.z = 0;
		Vector3 bulletHitTarget = hitPos + (forceDir * 0.2f);
		Vector3Int cellPos = layerMetaTile.WorldToCell(Vector3Int.RoundToInt(bulletHitTarget));

		TileChangeManager tileChangeManager = transform.GetComponentInParent<TileChangeManager>();

		// Store the old effect
		LayerTile oldEffectLayerTile = tileChangeManager.GetLayerTile(cellPos, LayerType.Effects);

		tileChangeManager.UpdateTile(cellPos, TileType.Effects, "KineticAnimation");

		yield return WaitFor.Seconds(.4f);

		tileChangeManager.RemoveTile(cellPos, LayerType.Effects);

		// Restore the old effect if any (ex: cracked glass, does not work)
		if (oldEffectLayerTile)
		{
			tileChangeManager.UpdateTile(cellPos, oldEffectLayerTile);
		}
		isOnDespawn = false;
		global::Despawn.ClientSingle(gameObject);
	}

}
