using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
		GetComponent<BulletMineOnHit>()?.BulletHitInteract(coll,Direction);
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

		Transform cellTransform = rigidBody.gameObject.transform;
		MetaTileMap layerMetaTile = cellTransform.GetComponentInParent<MetaTileMap>();
		var position = layerMetaTile.WorldToCell(Vector3Int.RoundToInt(rigidBody.gameObject.AssumedWorldPosServer()));

		TileChangeManager tileChangeManager = transform.GetComponentInParent<TileChangeManager>();

		// Store the old effect
		LayerTile oldEffectLayerTile = tileChangeManager.GetLayerTile(position, LayerType.Effects);

		tileChangeManager.UpdateTile(position, TileType.Effects, "KineticAnimation");

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
