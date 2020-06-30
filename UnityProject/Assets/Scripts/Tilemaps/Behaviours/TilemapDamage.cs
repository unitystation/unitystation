using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

/// <summary>
/// The level of damage that a window has received
/// </summary>
public enum WindowDamageLevel
{
	Undamaged,
	Crack01,
	Crack02,
	Crack03,
	Broken
}

/// <summary>
/// The level of damage that a grill has received
/// </summary>
public enum GrillDamageLevel
{
	Undamaged,
	Damaged
}

/// <summary>
/// Allows for damaging tiles and updating tiles based on damage taken.
/// </summary>
public class TilemapDamage : MonoBehaviour, IFireExposable
{
	//TODO: this needs a refactor. BaseTile has useful fields that should be used instead of this.
	//also this implementation isn't designed for tile variations, essentially being limited to one tile kind per layer

	private static readonly float TILE_MIN_SCORCH_TEMPERATURE = 100f;

	public float Integrity(Vector3Int pos)
	{
		if (!Layer.HasTile(pos, true))
		{
			return 0;
		}
		float maxDamage = 0;

		maxDamage = GetMaxDamage(pos);

		return Mathf.Clamp(maxDamage - metaDataLayer.Get(pos).Damage, 0, float.MaxValue);
	}

	private TileChangeManager tileChangeManager;
	private MetaDataLayer metaDataLayer;
	private MetaTileMap metaTileMap;

	public Layer Layer { get; private set; }

	private Matrix matrix;

	void Awake()
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

	//Server Only:
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

	/// <summary>
	/// Only works server side, applies the indicated melee damage to the tile, respecting armor.
	/// </summary>
	/// <param name="worldPos"></param>
	/// <param name="originator"></param>
	/// <param name="dmgAmt"></param>
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

		if (Layer.LayerType == LayerType.Floors)
		{

			return AddFloorDamage(dmgAmt, data, cellPos, worldPos, attackType);
		}

		if (Layer.LayerType == LayerType.Base)
		{

			return AddPlatingDamage(dmgAmt, data, cellPos, worldPos, attackType);
		}

		return basicTile.AddDamage(dmgAmt, data, cellPos, attackType, tileChangeManager);
	}

	private float AddWindowDamage(float damage, MetaDataNode data, Vector3Int cellPos, Vector3 hitPos, AttackType attackType, bool spawnPieces = true)
	{
		BasicTile tile = null;
		data.Damage += GetReducedDamage(cellPos, damage, attackType);

		if (data.Damage >= GetMaxDamage(cellPos))
		{
			tile = tileChangeManager.RemoveTile(cellPos, LayerType.Windows) as BasicTile;
			data.WindowDamage = WindowDamageLevel.Broken;
		}

		return CalculateAbsorbDamaged(cellPos, attackType,data,tile);
	}

	private float AddGrillDamage(float damage, MetaDataNode data, Vector3Int cellPos, Vector3 bulletHitTarget, AttackType attackType, bool spawnPieces = true)
	{
		data.Damage += GetReducedDamage(cellPos, damage, attackType);
		BasicTile tile = null;
		//Make grills a little bit weaker (set to 60 hp):
		if (data.Damage >= GetMaxDamage(cellPos))
		{
			tile = tileChangeManager.RemoveTile(cellPos, LayerType.Grills) as BasicTile;
		}

		return CalculateAbsorbDamaged(cellPos, attackType,data,tile);
	}

	private float AddFloorDamage(float dmgAmt, MetaDataNode data, Vector3Int cellPos, Vector2 worldPos, AttackType attackType)
	{
		data.Damage += GetReducedDamage(cellPos, dmgAmt, attackType);
		BasicTile tile = null;

		if (data.Damage >= GetMaxDamage(cellPos))
		{
			tile  = tileChangeManager.RemoveTile(cellPos, LayerType.Floors) as BasicTile;
		}

		return CalculateAbsorbDamaged(cellPos, attackType,data,tile);
	}

	private float AddPlatingDamage(float dmgAmt, MetaDataNode data, Vector3Int cellPos, Vector2 worldPos, AttackType attackType)
	{
		data.Damage += GetReducedDamage(cellPos, dmgAmt, attackType);
		BasicTile tile = null;
		if (data.Damage >= GetMaxDamage(cellPos))
		{
			tile = tileChangeManager.RemoveTile(cellPos, LayerType.Base, false) as BasicTile;
		}
		return CalculateAbsorbDamaged(cellPos, attackType,data,tile);
	}

	private float GetReducedDamage(Vector3Int cellPos, float dmgAmt, AttackType attackType)
	{

		var layerTile = metaTileMap.GetTile(cellPos, Layer.LayerType);
		if (layerTile is BasicTile tile)
		{
			return tile.Armor.GetDamage(dmgAmt, attackType);
		}

		return dmgAmt;
	}

	private float GetMaxDamage(Vector3Int cellPos)
	{
		var layerTile = metaTileMap.GetTile(cellPos, Layer.LayerType);
		if (layerTile is BasicTile tile)
		{
			return tile.MaxHealth;
		}

		return 0;
	}

	private float CalculateAbsorbDamaged(Vector3Int cellPos, AttackType attackType, MetaDataNode data, BasicTile tile = null)
	{
		if (tile == null)
		{
			tile= metaTileMap.GetTile(cellPos, Layer.LayerType) as BasicTile;
		}

		float currentDamage = data.Damage;
		if (tile.MaxHealth < data.Damage)
		{
			currentDamage = tile.MaxHealth;
			data.ResetDamage();
		}

		if (tile.Armor.GetRatingValue(attackType) > 0 && (currentDamage - data.GetPreviousDamage()) > 0)
		{
			return ((currentDamage - data.GetPreviousDamage() ) / tile.Armor.GetRatingValue(attackType));
		}
		else
		{
			return (0);
		}

	}

	public void RepairWindow(Vector3Int cellPos)
	{
		var data = metaDataLayer.Get(cellPos);
		tileChangeManager.RemoveTile(cellPos, LayerType.Effects);
		data.WindowDamage = WindowDamageLevel.Undamaged;
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

	public void TryScorch(Vector3Int cellPos)
	{
		//is it already scorched
		var metaData = metaDataLayer.Get(cellPos);
		if (metaData.IsScorched)
			return;

		//TODO: This should be done using an overlay system which hasn't been implemented yet, this replaces the tile's original appearance
		if (metaTileMap.HasTile(cellPos, LayerType.Floors, true))
		{ //Scorch floors
			tileChangeManager.UpdateTile(cellPos, TileType.Floor, "floorscorched" + Random.Range(1, 3));
		}
		else
		{ //Scorch base
			tileChangeManager.UpdateTile(cellPos, TileType.Base, "platingdmg" + Random.Range(1, 4));
		}

		metaData.IsScorched = true;
	}
}