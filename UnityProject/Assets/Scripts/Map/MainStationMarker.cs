using System.Collections;
using System.Collections.Generic;
using TileMap.Behaviours;
using UnityEngine;

public class MainStationMarker : ItemMatrixSystemInit
{
	public override void Start()
	{
		base.Start();
		if (CustomNetworkManager.IsServer)
		{
			MatrixManager.Instance.InternalMainStationMatrix = MetaTileMap.matrix;
			MetaTileMap.matrix.NetworkedMatrix.MatrixSync.IsMainStationMatrix = true;
		}
	}
}
