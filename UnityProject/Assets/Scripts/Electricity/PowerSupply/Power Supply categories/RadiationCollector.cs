﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RadiationCollector : InputTrigger, IDeviceControl
{
	private bool SelfDestruct = false;

	public PowerSupply powerSupply;
	[SyncVar(hook = "UpdateState")]
	public bool isOn = false;
	public int DirectionStart = 0;
	public int DirectionEnd = 9;
	public float MonitoringResistance = 9999999999;
	public float current = 20;
	public float Previouscurrent = 20;

	public PowerTypeCategory ApplianceType = PowerTypeCategory.RadiationCollector;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>()
	{
		PowerTypeCategory.StandardCable,
			PowerTypeCategory.HighVoltageCable,
	};

	public void PotentialDestroyed()
	{
		if (SelfDestruct)
		{
			//Then you can destroy
		}
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		powerSupply.InData.CanConnectTo = CanConnectTo;
		powerSupply.InData.Categorytype = ApplianceType;
		powerSupply.DirectionStart = DirectionStart;
		powerSupply.DirectionEnd = DirectionEnd;
		powerSupply.Data.SupplyingCurrent = 20;
		powerSupply.InData.ControllingDevice = this;

		PowerInputReactions PIRMedium = new PowerInputReactions(); //You need a resistance on the output just so supplies can communicate properly
		PIRMedium.DirectionReaction = true;
		PIRMedium.ConnectingDevice = PowerTypeCategory.MediumMachineConnector;
		PIRMedium.DirectionReactionA.AddResistanceCall.ResistanceAvailable = true;
		PIRMedium.DirectionReactionA.YouShallNotPass = true;
		PIRMedium.ResistanceReaction = true;
		PIRMedium.ResistanceReactionA.Resistance.Ohms = MonitoringResistance;

		isOn = true;
		UpdateServerState(isOn);
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		UpdateState(isOn);
	}

	void UpdateState(bool _isOn)
	{
		isOn = _isOn;
		if (isOn)
		{
			Debug.Log("TODO: Sprite changes for radiation collector (close door)");
		}
		else
		{
			Debug.Log("TODO: Sprite changes off for radiation collector (open door)");
		}
	}

	void UpdateServerState(bool _isOn)
	{
		if (isOn)
		{
			powerSupply.TurnOnSupply();
		}
		else
		{
			powerSupply.TurnOffSupply();
		}
	}

	public override void Interact(GameObject originator, Vector3 position, string hand)
	{
		//Interact stuff with the Radiation collector here
		if (!isServer)
		{
			InteractMessage.Send(gameObject, hand);
		}
		else
		{
			isOn = !isOn;
			UpdateServerState(isOn);
		}
	}
}