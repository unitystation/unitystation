using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RadiationCollector : PowerSupplyControlInheritance
{
	//public void PotentialDestroyed()
	//{
	//	if (SelfDestruct)
	//	{
	//		//Then you can destroy
	//	}
	//}

	public override void OnStartServerInitialise()
	{
		ApplianceType = PowerTypeCategory.RadiationCollector;
		CanConnectTo = new HashSet<PowerTypeCategory>
		{
				PowerTypeCategory.HighVoltageCable,
		};
		
		powerSupply.InData.CanConnectTo = CanConnectTo;
		powerSupply.InData.Categorytype = ApplianceType;
		powerSupply.WireEndB = WireEndB;
		powerSupply.WireEndA = WireEndA;
		SupplyingVoltage = 760000;
		InternalResistance = 76000;
		//current = 20; 
		PowerInputReactions PIRHigh = new PowerInputReactions(); //You need a resistance on the output just so supplies can communicate properly
		PIRHigh.DirectionReaction = true;
		PIRHigh.ConnectingDevice = PowerTypeCategory.HighVoltageCable;
		PIRHigh.DirectionReactionA.AddResistanceCall.ResistanceAvailable = true;
		PIRHigh.DirectionReactionA.YouShallNotPass = true;
		PIRHigh.ResistanceReaction = true;
		PIRHigh.ResistanceReactionA.Resistance.Ohms = MonitoringResistance;
		powerSupply.InData.ConnectionReaction[PowerTypeCategory.HighVoltageCable] = PIRHigh;
		isOn = true;
		UpdateServerState(isOn);
	}

	public override void StateChange(bool isOn)
	{
		if (isOn)
		{
			Debug.Log("TODO: Sprite changes for radiation collector (close door)");
		}
		else
		{
			Debug.Log("TODO: Sprite changes off for radiation collector (open door)");
		}
	}
}