using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class PoweredDevice : ElectricalOIinheritance, IElectricityIO
{

	//Renderers:
	public SpriteRenderer NorthConnection;
	public SpriteRenderer SouthConnection;
	public SpriteRenderer WestConnection;
	public SpriteRenderer EastConnection;

	public RegisterObject registerTile3;
	private Matrix matrix => registerTile3.Matrix;
	private Vector3 posCache;


	public bool IsConnector = false;


	private bool isSupplying = false;

	public void FindPossibleConnections()
	{
		Data.connections.Clear();
		Data.connections = ElectricityFunctions.FindPossibleConnections(
			transform.localPosition,
			matrix,
			InData.CanConnectTo,
			GetConnPoints()
		);
		if (Data.connections.Count > 0)
		{
			connected = true;
			//			if (IsConnector) { //For connectors sprites
			//				for (int i = 0; i < Data.connections.Count; i++) {
			//					if (Data.connections [i].InData.Categorytype == 0) {
			//					}
			//				}
			//			}
		}
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		//Not working for some reason:
		registerTile3 = gameObject.GetComponent<RegisterObject>();
		StartCoroutine(WaitForLoad());
		posCache = transform.localPosition;
	}

	IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(2f);
		FindPossibleConnections();
	}
		

	public override void DirectionOutput (GameObject SourceInstance)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		Data.DownstreamCount = Data.Downstream[SourceInstanceID].Count;
		Data.UpstreamCount = Data.Upstream[SourceInstanceID].Count;
	}


	public override void ElectricityOutput(float Current, GameObject SourceInstance)
	{
		Data.ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers(this);
		Data.CurrentInWire = Data.ActualCurrentChargeInWire.Current;
		Data.ActualVoltage = Data.ActualCurrentChargeInWire.Voltage;
		Data.EstimatedResistance = Data.ActualCurrentChargeInWire.EstimatedResistant;
	}
	public void SetConnPoints(int DirectionEndin, int DirectionStartin)
	{ }

}