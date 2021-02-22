using System;
using Mirror;
using TileManagement;
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
			if (!metaTileMap.HasTile(cellPos, LayerType.Floors)
			    && !metaTileMap.HasTile(cellPos, LayerType.Base)
			    && metaTileMap.HasObject(cellPos, CustomNetworkManager.Instance._isServer)
			)
			{
				foreach (var customNetTransform in matrix.Get<CustomNetTransform>(cellPos, true))
				{
					customNetTransform.CheckMatrixSwitch();
				}
			}
		});
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

	private float AddDamage(float damage, AttackType attackType, MetaDataNode data,
		BasicTile basicTile, Vector3 worldPosition)
	{
		if (basicTile.indestructible)
		{
			return 0;
		}

		var damageTaken = basicTile.Armor.GetDamage(damage < basicTile.damageDeflection ? 0 : damage, attackType);

		data.AddTileDamage(Layer.LayerType, damageTaken);

		if(basicTile.SoundOnHit.AssetAddress != null)
			SoundManager.PlayNetworkedAtPos(basicTile.SoundOnHit, worldPosition);
		else{
			Logger.LogError($"Tried to play SoundOnHit for {basicTile.DisplayName}, but it was null!", Category.Addressables);
		}

		var totalDamageTaken = data.GetTileDamage(Layer.LayerType);


		if (totalDamageTaken >= basicTile.MaxHealth)
		{
			data.RemoveTileDamage(Layer.LayerType);
			tileChangeManager.RemoveTile(data.Position, Layer.LayerType);

			//Add new tile if needed
			//TODO change floors to using overlays, but generic overlay will need to be sprited
			if (basicTile.ToTileWhenDestroyed != null)
			{
				var damageLeft = totalDamageTaken - basicTile.MaxHealth;
				var tile = basicTile.ToTileWhenDestroyed as BasicTile;

				while (damageLeft > 0 && tile != null)
				{
					if (tile.MaxHealth <= damageLeft)
					{
						damageLeft -= tile.MaxHealth;
						tile = tile.ToTileWhenDestroyed as BasicTile;
					}
					else
					{
						//Atm we just set remaining damage to 0, instead of absorbing it for the new tile
						damageLeft = 0;
						tileChangeManager.UpdateTile(data.Position, tile);
						break;
					}
				}

				damageTaken = damageLeft;
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
						tileChangeManager.UpdateOverlay(data.Position, overlayData.overlayTile);
						break;
					}
				}
			}

			//All the damage was absorbed, none left to return for next layer
			damageTaken = 0;
		}

		if (basicTile.MaxHealth < basicTile.MaxHealth - totalDamageTaken)
		{
			data.ResetDamage(Layer.LayerType);
		}

		//Return how much damage is left
		return damageTaken;
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
		tileChangeManager.RemoveOverlay(cellPos, LayerType.Effects);
		data.ResetDamage(Layer.LayerType);
	}
}