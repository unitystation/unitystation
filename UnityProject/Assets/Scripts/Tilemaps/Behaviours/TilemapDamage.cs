﻿using System;
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

		data.AddTileDamage(Layer.LayerType, basicTile.Armor.GetDamage(damage < basicTile.damageDeflection? 0: damage, attackType));

		SoundManager.PlayNetworkedAtPos(basicTile.SoundOnHit, worldPosition);


		if (data.GetTileDamage(Layer.LayerType) >= basicTile.MaxHealth)
		{
			data.RemoveTileDamage(Layer.LayerType);
			tileChangeManager.RemoveTile(data.Position, Layer.LayerType);
			basicTile.LootOnDespawn?.SpawnLoot(worldPosition);
		}

		return CalculateAbsorbDamaged(attackType,data,basicTile);
	}

	private float CalculateAbsorbDamaged(AttackType attackType, MetaDataNode data, BasicTile basicTile)
	{
		var damage = basicTile.MaxHealth - data.GetTileDamage(Layer.LayerType);

		if (basicTile.MaxHealth < damage)
		{
			data.ResetDamage(Layer.LayerType);
		}

		if (basicTile.Armor.GetRatingValue(attackType) > 0 && damage > 0)
		{
			return damage  / basicTile.Armor.GetRatingValue(attackType);
		}

		return 0;
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

	public void RepairWindow(Vector3Int cellPos)
	{
		var data = metaDataLayer.Get(cellPos);
		tileChangeManager.RemoveTile(cellPos, LayerType.Effects);
		data.ResetDamage(Layer.LayerType);
	}
}