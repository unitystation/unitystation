using UnityEngine;

public class TilemapDamage : MonoBehaviour, IFireExposable
{
	private TileChangeManager tileChangeManager;
	private MetaDataLayer metaDataLayer;
	private MetaTileMap metaTileMap;
	public Layer Layer { get; private set; }

	private Matrix matrix;

	private void Awake()
	{
		tileChangeManager = transform.GetComponentInParent<TileChangeManager>();
		metaDataLayer = transform.GetComponentInParent<MetaDataLayer>();
		metaTileMap = transform.GetComponentInParent<MetaTileMap>();

		Layer = GetComponent<Layer>();
		matrix = GetComponentInParent<Matrix>();

		tileChangeManager.OnFloorOrPlatingRemoved.RemoveAllListeners();
		tileChangeManager.OnFloorOrPlatingRemoved.AddListener(cellPos =>
		{ //Poke items when both floor and plating are gone
			//As they might want to change matrix
			if (!metaTileMap.HasTile(cellPos, LayerType.Floors, true)
			    && !metaTileMap.HasTile(cellPos, LayerType.Base, true)
			    && metaTileMap.HasTile(cellPos, LayerType.Objects, true)
			)
			{
				foreach (var customNetTransform in matrix.Get<CustomNetTransform>(cellPos, true))
				{
					customNetTransform.CheckMatrixSwitch();
				}
			}
		});
	}

	public void OnCollisionEnter2D(Collision2D coll)
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			return;
		}
		ContactPoint2D firstContact = coll.GetContact(0);
		DetermineAction(coll.gameObject, coll.relativeVelocity.normalized, firstContact.point);
	}

	private void DetermineAction(GameObject objectColliding, Vector2 forceDirection, Vector3 hitPos)
	{
		BulletBehaviour bulletBehaviour = objectColliding.transform.parent.GetComponent<BulletBehaviour>();
		if (bulletBehaviour != null)
		{
			DoBulletDamage(bulletBehaviour, forceDirection, hitPos);
		}
	}

	private void DoBulletDamage(BulletBehaviour bullet, Vector3 forceDir, Vector3 hitPos)
	{
		forceDir.z = 0;
		Vector3 bulletHitTarget = hitPos + (forceDir * 0.2f);
		Vector3Int cellPos = metaTileMap.WorldToCell(Vector3Int.RoundToInt(bulletHitTarget));
		MetaDataNode data = metaDataLayer.Get(cellPos);

		var basicTile = metaTileMap.GetTile(cellPos, Layer.LayerType) as BasicTile;

		if (basicTile == null) return;

		if (bullet.isMiningBullet)
		{
			if (Layer.LayerType == LayerType.Walls)
			{

				if (basicTile != null)
				{
					if (Validations.IsMineableAt(bulletHitTarget, metaTileMap))
					{
						SoundManager.PlayNetworkedAtPos("BreakStone", bulletHitTarget);
						Spawn.ServerPrefab(basicTile.SpawnOnDeconstruct, bulletHitTarget,
							count: basicTile.SpawnAmountOnDeconstruct);
						tileChangeManager.RemoveTile(cellPos, LayerType.Walls);
						return;
					}
				}
			}
		}

		basicTile.AddDamage(bullet.damage, data, cellPos, AttackType.Bullet, tileChangeManager);
	}

	public void DoThrowDamage(Vector3Int worldTargetPos, ThrowInfo throwInfo, int dmgAmt)
	{
		DoMeleeDamage(new Vector2(worldTargetPos.x, worldTargetPos.y), throwInfo.ThrownBy, dmgAmt);
	}

	public void DoMeleeDamage(Vector2 worldPos, GameObject originator, int dmgAmt)
	{
		Vector3Int cellPos = metaTileMap.WorldToCell(worldPos);
		DoDamageInternal(cellPos, dmgAmt, worldPos, AttackType.Melee);
	}

	public float ApplyDamage(Vector3Int cellPos, float dmgAmt, Vector3Int worldPos, AttackType attackType = AttackType.Melee)
	{
		return DoDamageInternal(cellPos, dmgAmt, worldPos, attackType); //idk if collision can be classified as "melee"
	}

	/// <summary>
	/// Damage in excess of the tile's current health, 0 if tile was not destroyed or health equaled damage done
	/// </summary>
	/// <param name="cellPos"></param>
	/// <param name="dmgAmt"></param>
	/// <param name="worldPos"></param>
	/// <param name="attackType"></param>
	/// <returns></returns>
	private float DoDamageInternal(Vector3Int cellPos, float dmgAmt, Vector3 worldPos, AttackType attackType)
	{
		MetaDataNode data = metaDataLayer.Get(cellPos);

		//look up layer tile so we can calculate damage
		var basicTile = metaTileMap.GetTile(cellPos, Layer.LayerType) as BasicTile;

		if (basicTile == null) return 0;

		if (basicTile.Resistances.Indestructable ||
		    basicTile.Resistances.FireProof && attackType == AttackType.Fire ||
		    basicTile.Resistances.AcidProof && attackType == AttackType.Acid)
		{
			return 0;
		}

		dmgAmt = basicTile.Armor.GetDamage(dmgAmt, attackType);

		return basicTile.AddDamage(dmgAmt, data, cellPos, attackType, tileChangeManager);
	}

	public float Integrity(Vector3Int pos)
	{
		var layerTile = metaTileMap.GetTile(pos, Layer.LayerType) as BasicTile;
		if (layerTile == null)
		{
			return 0;
		}

		return Mathf.Clamp(layerTile.MaxHealth - metaDataLayer.Get(pos).Damage, 0, float.MaxValue);
	}

	public void RepairWindow(Vector3Int cellPos)
	{
		var data = metaDataLayer.Get(cellPos);
		tileChangeManager.RemoveTile(cellPos, LayerType.Effects);
		data.Damage = 0;
	}

	public void OnExposed(FireExposure exposure)
	{
		var cellPos = exposure.ExposedLocalPosition;
		MetaDataNode data = metaDataLayer.Get(cellPos);

		//look up layer tile so we can calculate damage
		var basicTile = metaTileMap.GetTile(cellPos, Layer.LayerType) as BasicTile;

		if (basicTile == null) return;

		basicTile.AddDamage(exposure.StandardDamage(), data, cellPos, AttackType.Fire, tileChangeManager);
	}
}