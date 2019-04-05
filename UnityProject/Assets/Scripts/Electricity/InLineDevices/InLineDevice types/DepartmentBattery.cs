using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public enum BatteryStateSprite
{
	Full,
	Half,
	Empty,
}

public class DepartmentBattery : PowerSupplyControlInheritance
{
	public DepartmentBatterySprite CurrentSprite  = DepartmentBatterySprite.Default;
	public SpriteRenderer Renderer;

	public Sprite BatteryOpenPresent;
	public Sprite BatteryOpenMissing;
	public Sprite BatteryClosedMissing;

	public Sprite BatteryCharged;
	public Sprite PartialCharge;
	[SyncVar(hook = "UpdateBattery")]
	public BatteryStateSprite CurrentState;

	public Sprite LightOn;
	public Sprite LightOff;
	public Sprite LightRed;

	public SpriteRenderer BatteryCompartmentSprite;
	public SpriteRenderer BatteryIndicatorSprite;
	public SpriteRenderer PowerIndicator;

	public List<DepartmentBatterySprite> enums;
	public List<Sprite> Sprite;
	public Dictionary<DepartmentBatterySprite,Sprite> Sprites = new Dictionary<DepartmentBatterySprite, Sprite>();

	//public override bool isOnForInterface { get; set; } = false;
	//public override bool PassChangeToOff { get; set; } = false;
	[SyncVar]
	public int currentCharge; // 0 - 100

	public PowerTypeCategory ApplianceType = PowerTypeCategory.DepartmentBattery;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>()
	{
		PowerTypeCategory.LowMachineConnector
	};

	void Start() {//Initialise Sprites
		for (int i = 0; i< enums.Count; i++)
		{
			Sprites[enums[i]] = Sprite[i];
		}

		if (enums.Count > 0)
		{
			Renderer.sprite = Sprites[CurrentSprite];
		}
	}
	public override void OnStartServerInitialise()
	{
		powerSupply.InData.CanConnectTo = CanConnectTo;
		powerSupply.InData.Categorytype = ApplianceType;
		powerSupply.DirectionStart = DirectionStart;
		powerSupply.DirectionEnd = DirectionEnd;

		resistance.ResistanceAvailable = false;

		PowerInputReactions PIRLow = new PowerInputReactions(); //You need a resistance on the output just so supplies can communicate properly
		PIRLow.DirectionReaction = true;
		PIRLow.ConnectingDevice = PowerTypeCategory.LowMachineConnector;
		PIRLow.DirectionReactionA.AddResistanceCall.ResistanceAvailable = true;
		PIRLow.DirectionReactionA.YouShallNotPass = true;
		PIRLow.ResistanceReaction = true;
		PIRLow.ResistanceReactionA.Resistance.Ohms = MonitoringResistance;

		PowerInputReactions PRSDCable = new PowerInputReactions();
		PRSDCable.DirectionReaction = true;
		PRSDCable.ConnectingDevice = PowerTypeCategory.StandardCable;
		PRSDCable.DirectionReactionA.AddResistanceCall = resistance;
		PRSDCable.ResistanceReaction = true;
		PRSDCable.ResistanceReactionA.Resistance = resistance;

		resistance.Ohms = 10000;
		powerSupply.InData.ConnectionReaction[PowerTypeCategory.LowMachineConnector] = PIRLow;
		powerSupply.InData.ConnectionReaction[PowerTypeCategory.StandardCable] = PRSDCable;
		currentCharge = 0;
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		UpdateState(isOn);
	}

	public override void _PowerNetworkUpdate() { 
		if (CurrentCapacity > 0)
		{
			if (CurrentCapacity > (CapacityMax / 2))
			{
				if (CurrentState != BatteryStateSprite.Full)
				{
					UpdateBattery(BatteryStateSprite.Full);
				}

			}
			else
			{
				if (CurrentState != BatteryStateSprite.Half)
				{
					UpdateBattery(BatteryStateSprite.Half);
				}
			}
		}
		else
		{
			if (CurrentState != BatteryStateSprite.Empty)
			{
				UpdateBattery(BatteryStateSprite.Empty);
			}
		}
	}

	void UpdateBattery(BatteryStateSprite State)
	{
		CurrentState = State;

		switch (CurrentState)
		{
			case BatteryStateSprite.Full:
				if (BatteryIndicatorSprite.enabled == false)
				{
					BatteryIndicatorSprite.enabled = true;
				}
				BatteryIndicatorSprite.sprite = BatteryCharged;
				break;
			case BatteryStateSprite.Half:
				if (BatteryIndicatorSprite.enabled == false)
				{
					BatteryIndicatorSprite.enabled = true;
				}
				BatteryIndicatorSprite.sprite = PartialCharge;
				break;
			case BatteryStateSprite.Empty:
				BatteryIndicatorSprite.enabled = false;
				break;
		}

	}

	public override void StateChange(bool isOn)
	{
		if (isOn)
		{
			PowerIndicator.sprite = LightOn;

		}
		else
		{
			PowerIndicator.sprite = LightOff;
		}

	}

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		//Interact stuff with the SMES here
		if (!isServer)
		{
			InteractMessage.Send(gameObject, hand);
		}
		else
		{
			if (!powerSupply.Data.ChangeToOff)
			{
				isOn = !isOn;
				UpdateServerState(isOn);
			}
		}

		return true;
	}

	public override float ModifyElectricityInput( float Current, GameObject SourceInstance, ElectricalOIinheritance ComingFrom)
	{
		int InstanceID = SourceInstance.GetInstanceID();

		float ActualCurrent = powerSupply.Data.CurrentInWire;

		float Resistance = ElectricityFunctions.WorkOutResistance(powerSupply.Data.ResistanceComingFrom[InstanceID]);
		float Voltage = (Current * Resistance);
		//Logger.Log (Voltage.ToString() + " < Voltage " + Resistance.ToString() + " < Resistance" + ActualCurrent.ToString() + " < ActualCurrent" + Current.ToString() + " < Current");
		Tuple<float, float> Currentandoffcut = TransformerCalculations.TransformerCalculate(this, Voltage : Voltage, ResistanceModified : Resistance, ActualCurrent : ActualCurrent);
		if (Currentandoffcut.Item2 > 0)
		{
			if (!(powerSupply.Data.CurrentGoingTo.ContainsKey(InstanceID)))
			{
				powerSupply.Data.CurrentGoingTo[InstanceID] = new Dictionary<ElectricalOIinheritance, float>();
			}
			powerSupply.Data.CurrentGoingTo[InstanceID][powerSupply.GameObject().GetComponent<ElectricalOIinheritance>()] = Currentandoffcut.Item2;
		}
		//return (Current);
		return (Currentandoffcut.Item1);

	}
	public override float ModifyResistancyOutput( float Resistance, GameObject SourceInstance)
	{
		Tuple<float, float> ResistanceM = TransformerCalculations.TransformerCalculate(this, ResistanceToModify : Resistance);
		//return (Resistance);
		return (ResistanceM.Item1);
	}

	[ContextMethod("Toggle Charge", "Power_Button")]
	public void ToggleCharge()
	{
		ToggleCanCharge = !ToggleCanCharge;
	}

	[ContextMethod("Toggle Support", "Power_Button")]
	public void ToggleSupport()
	{
		ToggleCansupport = !ToggleCansupport;
	}

	//FIXME: Objects at runtime do not get destroyed. Instead they are returned back to pool
	//FIXME: that also renderers IDevice useless. Please reassess
	public void OnDestroy()
	{
//		ElectricalSynchronisation.StructureChange = true;
//		ElectricalSynchronisation.ResistanceChange = true;
//		ElectricalSynchronisation.CurrentChange = true;
		ElectricalSynchronisation.RemoveSupply(this, ApplianceType);
		SelfDestruct = true;
		//Make Invisible
	}
}