using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Atmospherics;
using UnityEngine;

public class PeriodicGasSetter : MonoBehaviour
{
	public float UpdateNSeconds = 10;

	private RegisterTile RegisterTile;

	public GasMix GasMix;

	public bool RunOnStart = true;

	private bool Added = false;

	public void Awake()
	{
		RegisterTile = this.GetComponent<RegisterTile>();
	}

	private void OnEnable()
	{
		if (CustomNetworkManager.IsServer) return;
		if (RunOnStart == false) return;
		UpdateManager.Add(UpdateLoop,UpdateNSeconds );
		Added = true;
	}

	private void OnDisable()
	{
		if (CustomNetworkManager.IsServer) return;
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateLoop);
		Added = false;
	}

	public void UpdateLoop()
	{
		var node = RegisterTile.Matrix.MetaDataLayer.Get(transform.localPosition.RoundToInt());
		var nodeGasMix =  node.GasMixLocal;

		var gas = GasMix.GasData.CopyTo(nodeGasMix.GasData);
		nodeGasMix.GasData = gas;
		nodeGasMix.Temperature = GasMix.Temperature;
		nodeGasMix.Temperature = GasMix.Pressure;
		nodeGasMix.Volume = GasMix.Volume;
		node.GasMixLocal = nodeGasMix;
	}


	public void FireOnce()
	{
		UpdateLoop();
	}

	[NaughtyAttributes.Button]
	public void StartLoop()
	{
		UpdateManager.Add(UpdateLoop,UpdateNSeconds);
		Added = true;
	}

	public void StopLoop()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateLoop);
		Added = false;
	}

	public void SetLoopSpeed(float NewSpeed)
	{
		if (Added)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateLoop);
		}

		UpdateNSeconds = NewSpeed;

		if (Added)
		{
			UpdateManager.Add(UpdateLoop,UpdateNSeconds);
		}
	}
}
