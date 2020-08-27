using System;
using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;

public class IceShard : MonoBehaviour
{
	private RegisterTile registerTile;

	private MetaDataNode metaDataNode;

	private Vector3 posCache;

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
	}

	private void OnEnable()
	{
		UpdateManager.Add(UpdateCycle, 1f);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateCycle);
	}

	private void UpdateCycle()
	{
		if(!CustomNetworkManager.IsServer) return;

		var pos = registerTile.WorldPosition;

		if (pos == posCache && metaDataNode.GasMix.Temperature > AtmosDefines.WATER_VAPOR_FREEZE)
		{
			metaDataNode.GasMix.AddGas(Gas.WaterVapor, 2f);
			Despawn.ServerSingle(gameObject);
			return;
		}
		else
		{
			posCache = pos;
			metaDataNode = MatrixManager.GetMetaDataAt(pos);

			if (metaDataNode.GasMix.Temperature > AtmosDefines.WATER_VAPOR_FREEZE)
			{
				metaDataNode.GasMix.AddGas(Gas.WaterVapor, 2f);
				Despawn.ServerSingle(gameObject);
				return;
			}
		}
	}
}
