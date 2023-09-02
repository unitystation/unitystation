using System;
using Light2D;
using Logs;
using Mirror;
using TileManagement;
using Tiles;
using UnityEngine;

public class TilemapDamage : MonoBehaviour, IFireExposable
{
	private TileChangeManager tileChangeManager;
	private MetaDataLayer metaDataLayer;
	private MetaTileMap metaTileMap;
	public Layer Layer { get; private set; }

	private Matrix matrix;

	//Is set to 10 as there isn't any tiles which go through 10 stages of damage, change if there is at some point.
	private const int maxOverflowProtection = 10;

	private void Awake()
	{
		tileChangeManager = transform.GetComponentInParent<TileChangeManager>();
		metaDataLayer = transform.GetComponentInParent<MetaDataLayer>();
		metaTileMap = transform.GetComponentInParent<MetaTileMap>();

		Layer = GetComponent<Layer>();
		matrix = GetComponentInParent<Matrix>();
	}

	//Poke items when both floor and plating are gone
	//As they might want to change matrix
	public void SwitchObjectsMatrixAt(Vector3Int cellPos)
	{
		if (!metaTileMap.HasTile(cellPos, LayerType.Floors)
		    && !metaTileMap.HasTile(cellPos, LayerType.Base))

		{
			foreach (var objectPhysics in matrix.Get<UniversalObjectPhysics>(cellPos, true))
			{
				objectPhysics.CheckMatrixSwitch();
			}
		}
	}

	public float ApplyDamage(float dmgAmt, AttackType attackType, Vector3 worldPos)
	{
		Vector3Int cellPosition = metaTileMap.WorldToCell(worldPos);
		return DealDamageAt(dmgAmt, attackType, cellPosition, worldPos);
	}

	public void OnExposed(FireExposure exposure)
	{
		if (Layer.LayerType == LayerType.Floors ||
		    Layer.LayerType == LayerType.Base ||
		    Layer.LayerType == LayerType.Walls ) return;

		var basicTile = metaTileMap.GetTile(exposure.ExposedLocalPosition, Layer.LayerType) as BasicTile;

		if (basicTile == null) return;

		MetaDataNode data = metaDataLayer.Get(exposure.ExposedLocalPosition);
		AddDamage(exposure.StandardDamage(), AttackType.Fire, data, basicTile, exposure.ExposedWorldPosition);
	}

	private float DealDamageAt(float damage, AttackType attackType, Vector3Int cellPos, Vector3 worldPosition)
	{
		var basicTile = metaTileMap.GetTile(cellPos, Layer.LayerType) as BasicTile;

		if (basicTile == null) return 0;

		if (basicTile.indestructible) return 0;

		MetaDataNode data = metaDataLayer.Get(cellPos);
		return AddDamage(damage, attackType, data, basicTile, worldPosition);
	}

	private float AddDamage(float Energy, AttackType attackType, MetaDataNode data,
		BasicTile basicTile, Vector3 worldPosition)
	{
		float energyAbsorbed = 0;
		if (basicTile.indestructible || Energy < basicTile.damageDeflection)
		{
			if (attackType == AttackType.Bomb && basicTile.ExplosionImpassable == false)
			{
				return Energy * 0.375f;
			}
			else
			{
				if (attackType == AttackType.Bomb)
				{
					return energyAbsorbed * 0.85f;
				}
				else
				{
					return energyAbsorbed;
				}
			}
		}

		var damageTaken = basicTile.Armor.GetDamage(Energy, attackType);

		data.AddTileDamage(Layer.LayerType, damageTaken);

		if(basicTile.SoundOnHit != null && !string.IsNullOrEmpty(basicTile.SoundOnHit.AssetAddress) && basicTile.SoundOnHit.AssetAddress != "null")
		{
			if(damageTaken >= 1)
				SoundManager.PlayNetworkedAtPos(basicTile.SoundOnHit, worldPosition);
		}

		var totalDamageTaken = data.GetTileDamage(Layer.LayerType);

		if (totalDamageTaken >= basicTile.MaxHealth)
		{
			float excessEnergy = basicTile.Armor.GetForce( totalDamageTaken - basicTile.MaxHealth, attackType);
			if (basicTile.SoundOnDestroy.Count > 0)
			{
				SoundManager.PlayNetworkedAtPos(basicTile.SoundOnDestroy.RandomElement(), worldPosition);
			}
			data.RemoveTileDamage(Layer.LayerType);
			tileChangeManager.MetaTileMap.RemoveTileWithlayer(data.LocalPosition, Layer.LayerType);
			tileChangeManager.MetaTileMap.RemoveOverlaysOfType(data.LocalPosition, LayerType.Effects, OverlayType.Damage);

			if (Layer.LayerType == LayerType.Floors || Layer.LayerType == LayerType.Base)
			{
				tileChangeManager.MetaTileMap.RemoveOverlaysOfType(data.LocalPosition, LayerType.Floors, OverlayType.Cleanable);
			}

			if (Layer.LayerType == LayerType.Walls)
			{
				tileChangeManager.MetaTileMap.RemoveOverlaysOfType(data.LocalPosition, LayerType.Walls, OverlayType.Cleanable);
				tileChangeManager.MetaTileMap.RemoveOverlaysOfType(data.LocalPosition, LayerType.Effects, OverlayType.Mining);
			}

			//Add new tile if needed
			//TODO change floors to using overlays, but generic overlay will need to be sprited
			//TODO Use Armour values
			//TODO have tiles present but one z down
			if (basicTile.ToTileWhenDestroyed != null)
			{
				var tile = basicTile.ToTileWhenDestroyed as BasicTile;

				var overFlowProtection = 0;

				while (excessEnergy > 0 && tile != null)
				{
					overFlowProtection++;

					if (tile.MaxHealth <= excessEnergy)
					{
						excessEnergy -= tile.MaxHealth;
						tile = tile.ToTileWhenDestroyed as BasicTile;
					}
					else
					{
						//Atm we just set remaining damage to 0, instead of absorbing it for the new tile
						excessEnergy = 0;
						tileChangeManager.MetaTileMap.SetTile(data.LocalPosition, tile);
						break;
					}

					if (overFlowProtection > maxOverflowProtection)
					{
						Loggy.LogError($"Overflow protection triggered on {basicTile.name}, ToTileWhenDestroyed is spawning tiles in a loop", Category.TileMaps);
						break;
					}
				}

				energyAbsorbed = Energy - excessEnergy;
			}

			if (basicTile.SpawnOnDestroy != null)
			{
				basicTile.SpawnOnDestroy.SpawnAt(SpawnDestination.At(worldPosition, metaTileMap.ObjectLayer.gameObject.transform));
			}

			basicTile.LootOnDespawn?.SpawnLoot(worldPosition);
		}
		else
		{
			if (basicTile.DamageOverlayList != null)
			{
				foreach (var overlayData in basicTile.DamageOverlayList.DamageOverlays)
				{
					if (overlayData.damagePercentage <= totalDamageTaken / basicTile.MaxHealth)
					{
						tileChangeManager.MetaTileMap.AddOverlay(data.LocalPosition, overlayData.overlayTile);
						break;
					}
				}
			}

			//All the damage was absorbed, none left to return for next layer
			energyAbsorbed = Energy;
		}

		if (basicTile.MaxHealth < basicTile.MaxHealth - totalDamageTaken)
		{
			data.ResetDamage(Layer.LayerType);
		}

		if (attackType == AttackType.Bomb && basicTile.ExplosionImpassable == false)
		{
			return energyAbsorbed  * 0.375f;
		}
		else
		{
			if (attackType == AttackType.Bomb)
			{
				return energyAbsorbed*  0.85f;
			}
			else
			{
				return energyAbsorbed;
			}
		}
	}

	public float Integrity(Vector3Int pos)
	{
		var layerTile = metaTileMap.GetTile(pos, Layer.LayerType) as BasicTile;
		if (layerTile == null)
		{
			return 0;
		}

		return Mathf.Clamp(layerTile.MaxHealth - metaDataLayer.Get(pos).GetTileDamage(layerTile.LayerType), 0, float.MaxValue);
	}

	public void RemoveTileEffects(Vector3Int cellPos)
	{
		var data = metaDataLayer.Get(cellPos);

		tileChangeManager.MetaTileMap.RemoveOverlaysOfType(cellPos, LayerType.Effects, OverlayType.Damage);
		data.ResetDamage(Layer.LayerType);
	}
}