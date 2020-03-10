using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletKinetic : BulletBehaviour
{
	public float maxBulletDistance;

	public override void Shoot(Vector2 dir, GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest)
	{
		StartShoot(dir, controlledByPlayer, fromWeapon, targetZone);
		StartCoroutine(countTiles());
	}

	private IEnumerator countTiles()
	{
		Vector2 startPos = gameObject.AssumedWorldPosServer();
		//List<Vector3Int> positionList = MatrixManager.GetTiles(startPos, dir, 3);
		float time = maxBulletDistance / weapon.ProjectileVelocity;
		yield return WaitFor.Seconds(time);
		ReturnToPool();
	}

	public override void HandleCollisionEnter2D(Collision2D coll)
	{
		GetComponent<BulletMineOnHit>()?.bulletEnter2D(coll);
		ReturnToPool();
	}

	protected override void ReturnToPool()
	{
		if (trailRenderer != null)
		{
			trailRenderer.ShotDone();
		}

		rigidBody.velocity = Vector2.zero;
		StartCoroutine(KineticAnim());

	}

	public IEnumerator KineticAnim()
	{
		/*Vector3 posBullet = rigidBody.gameObject.AssumedWorldPosServer();
		Vector3 vec = posBullet;
		vec.z = 0;
		posBullet +=(vec * 0.2f);
		*/
		Transform cellTransform = rigidBody.gameObject.transform;
		MetaTileMap layerMetaTile = cellTransform.GetComponentInParent<MetaTileMap>();
		var position = layerMetaTile.WorldToCell(Vector3Int.RoundToInt(rigidBody.gameObject.AssumedWorldPosServer()));
		if (position == null) Chat.AddGameWideSystemMsgToChat("position is null");
		TileChangeManager tileChangeManager = transform.GetComponentInParent<TileChangeManager>();
		if (tileChangeManager == null) Chat.AddGameWideSystemMsgToChat("position is null");
		// Store the old effect for restoring after fire is gone
		LayerTile oldEffectLayerTile = tileChangeManager.GetLayerTile(position, LayerType.Effects);

		tileChangeManager.UpdateTile(position, TileType.Effects, "Kinetic");

		yield return WaitFor.Seconds(.4f);

		tileChangeManager.RemoveTile(position, LayerType.Effects);
		//tileChangeManager.RemoveEffect(position, LayerType.Effects);
		Chat.AddGameWideSystemMsgToChat("Remove was called");
		// Restore the old effect if any (ex: cracked glass)
		if (oldEffectLayerTile)
			tileChangeManager.UpdateTile(position, oldEffectLayerTile);
		Despawn.ClientSingle(gameObject);
	}

}
