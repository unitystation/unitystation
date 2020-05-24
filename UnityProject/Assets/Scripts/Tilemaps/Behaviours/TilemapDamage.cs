using System;
using System.Collections;
using System.Collections.Generic;
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

		if (Layer.LayerType == LayerType.Windows)
		{
			LayerTile getTile = metaTileMap.GetTile(cellPos, LayerType.Windows);
			if (getTile != null)
			{
				//TODO damage amt based off type of bullet
				AddWindowDamage(bullet.damage, data, cellPos, bulletHitTarget, AttackType.Bullet);
				return;
			}
		}

		if (Layer.LayerType == LayerType.Grills)
		{
			//Make sure a window is not protecting it first:
			if (!metaTileMap.HasTile(cellPos, LayerType.Windows, true))
			{
				if (metaTileMap.HasTile(cellPos, LayerType.Grills, true))
				{
					//TODO damage amt based off type of bullet
					AddGrillDamage(bullet.damage, data, cellPos, bulletHitTarget, AttackType.Bullet);
				}
			}
		}
		if (bullet.isMiningBullet)
		{
			if (Layer.LayerType == LayerType.Walls)
			{
				LayerTile getTile = metaTileMap.GetTile(cellPos, LayerType.Walls);
				if (getTile != null)
				{
					if (Validations.IsMineableAt(bulletHitTarget, metaTileMap))
					{
						SoundManager.PlayNetworkedAtPos("BreakStone", bulletHitTarget);
						var tile = getTile as BasicTile;
						Spawn.ServerPrefab(tile.SpawnOnDeconstruct, bulletHitTarget, count: tile.SpawnAmountOnDeconstruct);
						tileChangeManager.RemoveTile(cellPos, LayerType.Walls);
					}
				}

			}
		}

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

	/// <returns>Damage in excess of the tile's current health, 0 if tile was not destroyed or health equaled
	/// <paramref name="attackType"/>
	/// damage done.</returns>
	private float DoDamageInternal(Vector3Int cellPos, float dmgAmt, Vector3 worldPos, AttackType attackType)
	{
		MetaDataNode data = metaDataLayer.Get(cellPos);

		//look up layer tile so we can calculate damage
		var layerTile = metaTileMap.GetTile(cellPos, true);


		if (layerTile is BasicTile basicTile)
		{
			if (basicTile.Resistances.Indestructable ||
			    basicTile.Resistances.FireProof && attackType == AttackType.Fire ||
			    basicTile.Resistances.AcidProof && attackType == AttackType.Acid)
			{
				return 0;
			}

			dmgAmt = basicTile.Armor.GetDamage(dmgAmt, attackType);
		}


		if (Layer.LayerType == LayerType.Walls)
		{
			if (metaTileMap.HasTile(cellPos, LayerType.Walls, true))
			{
				//				SoundManager.PlayNetworkedAtPos( "WallHit", worldPos, Random.Range( 0.9f, 1.1f ) );
				return AddWallDamage(dmgAmt, data, cellPos, worldPos, attackType);
			}
		}

		if (Layer.LayerType == LayerType.Windows)
		{
			if (metaTileMap.HasTile(cellPos, LayerType.Windows, true))
			{
				SoundManager.PlayNetworkedAtPos("GlassHit", worldPos, Random.Range(0.9f, 1.1f));
				return AddWindowDamage(dmgAmt, data, cellPos, worldPos, attackType);
			}
		}

		if (Layer.LayerType == LayerType.Grills)
		{
			//Make sure a window is not protecting it first:
			if (!metaTileMap.HasTile(cellPos, LayerType.Windows, true))
			{
				if (metaTileMap.HasTile(cellPos, LayerType.Grills, true))
				{
					SoundManager.PlayNetworkedAtPos("GrillHit", worldPos, Random.Range(0.9f, 1.1f));
					return AddGrillDamage(dmgAmt, data, cellPos, worldPos, attackType);
				}
			}
		}

		if (Layer.LayerType == LayerType.Objects)
		{
			if (metaTileMap.GetTile(cellPos, LayerType.Objects)?.TileType == TileType.Table)
			{
				//				SoundManager.PlayNetworkedAtPos( "TableHit", worldPos, Random.Range( 0.9f, 1.1f ) );
				return AddTableDamage(dmgAmt, data, cellPos, worldPos, attackType);
			}
		}

		if (Layer.LayerType == LayerType.Floors)
		{
			if (metaTileMap.HasTile(cellPos, LayerType.Floors, true))
			{
				//				SoundManager.PlayNetworkedAtPos( "FloorHit", worldPos, Random.Range( 0.9f, 1.1f ) );
				return AddFloorDamage(dmgAmt, data, cellPos, worldPos, attackType);
			}
		}

		if (Layer.LayerType == LayerType.Base)
		{
			if (metaTileMap.HasTile(cellPos, LayerType.Base, true))
			{
				//				SoundManager.PlayNetworkedAtPos( "FloorHit", worldPos, Random.Range( 0.9f, 1.1f ) );
				return AddPlatingDamage(dmgAmt, data, cellPos, worldPos, attackType);
			}
		}

		return dmgAmt;
	}

	private float AddTableDamage(float dmgAmt, MetaDataNode data, Vector3Int cellPos, Vector2 worldPos, AttackType attackType)
	{
		data.Damage += GetReducedDamage(cellPos, dmgAmt, attackType);

		if (data.Damage >= GetMaxDamage(cellPos))
		{
			//watch out! must not accidentally destroy other objects like player!
			tileChangeManager.RemoveTile(cellPos, LayerType.Objects);

			//			SoundManager.PlayNetworkedAtPos("TableHit", worldPos, 1f);

			//Spawn remains:
			if (Random.value < 0.25f)
			{
				SpawnRods(worldPos);
			}
			else if (Random.value > 0.75f)
			{
				SpawnMetal(worldPos);
			}

			return data.ResetDamage() - GetMaxDamage(cellPos);
		}

		return 0;
	}

	private float AddWallDamage(float dmgAmt, MetaDataNode data, Vector3Int cellPos, Vector2 worldPos, AttackType attackType)
	{
		data.Damage += GetReducedDamage(cellPos, dmgAmt, attackType);

		if (data.Damage >= GetMaxDamage(cellPos))
		{
			tileChangeManager.RemoveTile(cellPos, LayerType.Walls);

			//			SoundManager.PlayNetworkedAtPos("WallHit", worldPos, 1f);

			//Spawn remains:
			if (Random.value < 0.05f)
			{
				SpawnRods(worldPos);
			}
			else if (Random.value > 0.95f)
			{
				SpawnMetal(worldPos);
			}

			return data.ResetDamage() - GetMaxDamage(cellPos);
		}

		return 0;
	}

	private float AddFloorDamage(float dmgAmt, MetaDataNode data, Vector3Int cellPos, Vector2 worldPos, AttackType attackType)
	{
		data.Damage += GetReducedDamage(cellPos, dmgAmt, attackType);

		if (data.Damage >= 30 && data.Damage < 70)
		{
			TryScorch(cellPos);
		}
		else if (data.Damage >= GetMaxDamage(cellPos))
		{
			var removed = tileChangeManager.RemoveTile(cellPos, LayerType.Floors);
			if (Random.value < 0.25f)
			{
				if (removed is BasicTile basicTile)
				{
					var toSpawn = basicTile.SpawnOnDeconstruct;
					Spawn.ServerPrefab(toSpawn, worldPos);
				}
			}

			//			SoundManager.PlayNetworkedAtPos("FloorHit", worldPos, 1f);

			return data.ResetDamage() - GetMaxDamage(cellPos);
		}

		return 0;
	}

	/// <summary>
	/// Damage Plating/Catwalk/Lattice
	/// </summary>
	/// <param name="dmgAmt"></param>
	/// <param name="data"></param>
	/// <param name="cellPos"></param>
	/// <param name="worldPos"></param>
	/// <param name="attackType"></param>
	private float AddPlatingDamage(float dmgAmt, MetaDataNode data, Vector3Int cellPos, Vector2 worldPos, AttackType attackType)
	{
		data.Damage += GetReducedDamage(cellPos, dmgAmt, attackType);

		if (data.Damage >= 30 && data.Damage < GetMaxDamage(cellPos))
		{
			TryScorch(cellPos);
		}
		else if (data.Damage >= GetMaxDamage(cellPos))
		{
			tileChangeManager.RemoveTile(cellPos, LayerType.Base);
			//Spawn remains:
			if (Random.value < 0.25f)
			{
				SpawnRods(worldPos);
			}
			else if (Random.value > 0.75f)
			{
				SpawnMetal(worldPos);
			}

			//			SoundManager.PlayNetworkedAtPos("PlatingHit", worldPos, 1f);

			return data.ResetDamage() - GetMaxDamage(cellPos);
		}

		return 0;
	}

	public void RepairWindow(Vector3Int cellPos)
	{
		var data = metaDataLayer.Get(cellPos);
		tileChangeManager.RemoveTile(cellPos, LayerType.Effects);
		data.WindowDamage = WindowDamageLevel.Undamaged;
		data.Damage = 0;
	}

	/// <summary>
	/// Damage a window tile, incrementaly
	/// </summary>
	/// <param name="damage">The amount of damage the window received</param>
	/// <param name="data">The data about the current state of this window</param>
	/// <param name="cellPos">The position of the window tile</param>
	/// <param name="hitPos">Where exactly the bullet hit</param>
	/// <param name="attackType">The type of attack that did the damage</param>
	/// <returns>The remaining damage to apply to the tile if the window is broken, 0 otherwise.</returns>
	private float AddWindowDamage(float damage, MetaDataNode data, Vector3Int cellPos, Vector3 hitPos, AttackType attackType, bool spawnPieces = true)
	{
		data.Damage += GetReducedDamage(cellPos, damage, attackType);

		if (data.Damage >= 20 && data.Damage < 50)
		{
			tileChangeManager.UpdateTile(cellPos, TileType.WindowDamaged, "crack01");
			data.WindowDamage = WindowDamageLevel.Crack01;
		}

		if (data.Damage >= 50 && data.Damage < 75)
		{
			tileChangeManager.UpdateTile(cellPos, TileType.WindowDamaged, "crack02");
			data.WindowDamage = WindowDamageLevel.Crack02;
		}

		if (data.Damage >= 75 && data.Damage < GetMaxDamage(cellPos))
		{
			tileChangeManager.UpdateTile(cellPos, TileType.WindowDamaged, "crack03");
			data.WindowDamage = WindowDamageLevel.Crack03;
		}

		if (data.Damage >= GetMaxDamage(cellPos))
		{
			tileChangeManager.RemoveTile(cellPos, LayerType.Windows);
			data.WindowDamage = WindowDamageLevel.Broken;

			//Spawn 3 glass shards with different sprites:
			if (spawnPieces)
			{
				SpawnGlassShards(hitPos);
			}

			//Play the breaking window sfx:
			SoundManager.PlayNetworkedAtPos("GlassBreak0#", hitPos, 1f);

			return data.ResetDamage() - GetMaxDamage(cellPos);
		}

		return 0; // The remaining damage after cracking the window.
	}

	private float AddGrillDamage(float damage, MetaDataNode data, Vector3Int cellPos, Vector3 bulletHitTarget, AttackType attackType, bool spawnPieces = true)
	{
		data.Damage += GetReducedDamage(cellPos, damage, attackType);

		//At half health change image of grill to damaged
		if (data.Damage >= GetMaxDamage(cellPos) / 2 && data.Damage < GetMaxDamage(cellPos))
		{
			if (data.GrillDamage != GrillDamageLevel.Damaged)
			{
				tileChangeManager.UpdateTile(cellPos, TileType.Grill, "GrilleDestroyed");
				data.GrillDamage = GrillDamageLevel.Damaged;

				SoundManager.PlayNetworkedAtPos("GrillHit", bulletHitTarget, 1f);

				//Spawn rods
				if (Random.value < 0.7f && spawnPieces)
				{
					SpawnRods(bulletHitTarget);
				}
			}
		}

		//Make grills a little bit weaker (set to 60 hp):
		if (data.Damage >= GetMaxDamage(cellPos))
		{
			tileChangeManager.RemoveTile(cellPos, LayerType.Grills);

			SoundManager.PlayNetworkedAtPos("GrillHit", bulletHitTarget, 1f);

			//Spawn rods
			if (Random.value < 0.7f && spawnPieces)
			{
				SpawnRods(bulletHitTarget);
			}

			return data.ResetDamage() - GetMaxDamage(cellPos);
		}

		return 0;
	}

	//Only works server side:
	public void WireCutGrill(Vector3 snipPosition)
	{
		Vector3Int cellPos = metaTileMap.WorldToCell(snipPosition);
		MetaDataNode data = metaDataLayer.Get(cellPos);

		if (Layer.LayerType == LayerType.Grills)
		{
			//Make sure a window is not protecting it first:
			if (!metaTileMap.HasTile(cellPos, LayerType.Windows, true))
			{
				if (metaTileMap.HasTile(cellPos, LayerType.Grills, true))
				{
					tileChangeManager.RemoveTile(cellPos, LayerType.Grills);

					SoundManager.PlayNetworkedAtPos("WireCutter", snipPosition, 1f);
					SpawnRods(snipPosition);
				}
			}
		}

		data.ResetDamage();
	}

	private void SpawnMetal(Vector3 pos)
	{
		Spawn.ServerPrefab("Metal", pos.CutToInt(), count: 1,
			scatterRadius: Spawn.DefaultScatterRadius);
	}
	private void SpawnRods(Vector3 pos)
	{
		Spawn.ServerPrefab("Rods", pos.CutToInt(), count: 1,
			scatterRadius: Spawn.DefaultScatterRadius);
	}

	private void SpawnGlassShards(Vector3 pos)
	{
		//Spawn 2-4 glass shards
		Spawn.ServerPrefab("GlassShard", pos, count: Random.Range(1, 4),
			scatterRadius: Random.Range(0, 3));

		//Play the breaking window sfx:
		SoundManager.PlayNetworkedAtPos("GlassBreak0#", pos, 1f);
	}

	public void OnExposed(FireExposure exposure)
	{
		Profiler.BeginSample("TileExpose");
		var cellPos = exposure.ExposedLocalPosition;
		if (Layer.LayerType == LayerType.Floors)
		{
			//floor scorching
			if (exposure.IsSideExposure)
			{
				Profiler.EndSample();
				return;
			}

			if (!(exposure.Temperature > TILE_MIN_SCORCH_TEMPERATURE))
			{
				Profiler.EndSample();
				return;
			}

			if (!metaTileMap.HasTile(cellPos, true))
			{
				Profiler.EndSample();
				return;
			}
			TryScorch(cellPos);
		}
		else if (Layer.LayerType == LayerType.Windows)
		{
			if (metaTileMap.HasTile(cellPos, LayerType.Windows, true))
			{
				//window damage
				SoundManager.PlayNetworkedAtPos("GlassHit", exposure.ExposedWorldPosition, Random.Range(0.9f, 1.1f));
				AddWindowDamage(exposure.StandardDamage(), metaDataLayer.Get(cellPos), cellPos, exposure.ExposedWorldPosition, AttackType.Fire, false);
			}

		}
		else if (Layer.LayerType == LayerType.Grills)
		{
			//grill damage
			//Make sure a window is not protecting it first:
			if (!metaTileMap.HasTile(cellPos, LayerType.Windows, true))
			{
				if (metaTileMap.HasTile(cellPos, LayerType.Grills, true))
				{
					SoundManager.PlayNetworkedAtPos("GrillHit", exposure.ExposedWorldPosition, Random.Range(0.9f, 1.1f));
					AddGrillDamage(exposure.StandardDamage(), metaDataLayer.Get(cellPos), cellPos, exposure.ExposedWorldPosition, AttackType.Fire, false);
				}
			}
		}
		Profiler.EndSample();
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