using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class APCPoweredDevice : MonoBehaviour
{
	public bool IsEnvironmentalDevice = false;
	public float Wattusage = 0.01f;
	public float Resistance = 99999999;
	public APC RelatedAPC;
	void Start() {
		if (Wattusage > 0) {
			Resistance = 240/(Wattusage / 240) ;
		}
	}
	public void APCBroadcastToDevice(APC APC)
	{
		if (RelatedAPC == null)
		{
			RelatedAPC = APC;
			if (IsEnvironmentalDevice)
			{
				RelatedAPC.EnvironmentalDevices.Add(this);
			}
			else { 
				RelatedAPC.ConnectedDevices.Add(this);
			}
		}
	}
	public void PowerNetworkUpdate(float Voltage)
	{
	}
}
