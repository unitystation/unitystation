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
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Add(ServerUpdateCycle, 1f);
		}
	}

	private void OnDisable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ServerUpdateCycle);
		}
	}

	private void ServerUpdateCycle()
	{
		var pos = registerTile.WorldPosition;

		if (pos != posCache)
		{
			metaDataNode = MatrixManager.GetMetaDataAt(pos);
			posCache = pos;
		}

		if (metaDataNode.GasMix.Temperature > AtmosDefines.WATER_VAPOR_FREEZE)
		{
			metaDataNode.GasMix.AddGas(Gas.WaterVapor, 2f);
			Despawn.ServerSingle(gameObject);
		}
	}
}
