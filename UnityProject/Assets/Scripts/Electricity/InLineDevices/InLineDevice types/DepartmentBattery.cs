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

public class DepartmentBattery : InputTrigger, IElectricalNeedUpdate, IInLineDevices, Itransformer, IBattery, IDeviceControl
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

	private bool SelfDestruct = false;


	public InLineDevice RelatedDevice;
	//Is the SMES turned on
	[SyncVar(hook = "UpdateState")]
	public bool isOn = false;
	public bool isOnForInterface { get; set; } = false;
	public bool ChangeToOff = false;
	public bool PassChangeToOff { get; set; } = false;
	[SyncVar]
	public int currentCharge; // 0 - 100
	public float current { get; set; } = 0;
	public float Previouscurrent = 0;
	public float PreviousResistance = 0;
	private Resistance resistance = new Resistance();
	//Sprites:
	public int DirectionStart = 0;
	public int DirectionEnd = 9;

	public float TurnRatio { get; set; } = 12.5f;
	public float VoltageLimiting { get; set; } = 0;
	public float VoltageLimitedTo { get; set; } = 0;

	public float MaximumCurrentSupport { get; set; } = 8;
	public float MinimumSupportVoltage { get; set; } = 216;
	public float StandardSupplyingVoltage { get; set; } = 240;
	public float PullingWatts { get; set; } = 0;
	public float CapacityMax { get; set; } = 432000;
	public float CurrentCapacity { get; set; } = 432000;
	public float PullLastDeductedTime { get; set; } = 0;
	public float ChargLastDeductedTime { get; set; } = 0;

	public float ExtraChargeCutOff { get; set; } = 240;
	public float IncreasedChargeVoltage { get; set; } = 250;
	public float StandardChargeNumber { get; set; } = 6;
	public float ChargeSteps { get; set; } = 0.1f;
	//public float MaxChargingMultiplier { get; set; } = 1.2f;
	public float MaxChargingMultiplier { get; set; } = 999999f;
	public float ChargingMultiplier { get; set; } = 0.1f;

	public float ChargingWatts { get; set; } = 0;
	public float Resistance { get; set; } = 0;
	public float CircuitResistance { get; set; } = 0;

	public bool CanCharge { get; set; } = true;
	public bool Cansupport { get; set; } = true;
	public bool ToggleCanCharge { get; set; } = true;
	public bool ToggleCansupport { get; set; } = true;

	public float ActualVoltage { get; set; } = 0;
	public float MonitoringResistance = 999999;

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
	public override void OnStartServer()
	{
		base.OnStartServer();
		RelatedDevice.InData.CanConnectTo = CanConnectTo;
		RelatedDevice.InData.Categorytype = ApplianceType;
		RelatedDevice.InData.ControllingUpdate = this;
		RelatedDevice.DirectionStart = DirectionStart;
		RelatedDevice.DirectionEnd = DirectionEnd;

		RelatedDevice.RelatedDevice = this;
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
		RelatedDevice.InData.ConnectionReaction[PowerTypeCategory.LowMachineConnector] = PIRLow;
		RelatedDevice.InData.ConnectionReaction[PowerTypeCategory.StandardCable] = PRSDCable;
		RelatedDevice.InData.ControllingDevice = this;
		RelatedDevice.InData.ControllingUpdate = this;
		currentCharge = 0;
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		UpdateState(isOn);
	}

	public void PotentialDestroyed()
	{
		if (SelfDestruct)
		{
			//Then you can destroy
		}
	}

	public void PowerUpdateStructureChange()
	{
		RelatedDevice.PowerUpdateStructureChange();
	}
	public void PowerUpdateStructureChangeReact()
	{
		RelatedDevice.PowerUpdateStructureChangeReact();
	}
	public void InitialPowerUpdateResistance(){
		RelatedDevice.InitialPowerUpdateResistance();
		foreach (KeyValuePair<IElectricityIO,HashSet<PowerTypeCategory>> Supplie in RelatedDevice.Data.ResistanceToConnectedDevices) {
			RelatedDevice.ResistanceInput( 1.11111111f, Supplie.Key.GameObject(), null);
			ElectricalSynchronisation.NUCurrentChange.Add (Supplie.Key.InData.ControllingUpdate);
		}
	}
	public void PowerUpdateResistanceChange()
	{
		RelatedDevice.PowerUpdateResistanceChange();
		RelatedDevice.PowerUpdateResistanceChange();
		foreach (KeyValuePair<IElectricityIO,HashSet<PowerTypeCategory>> Supplie in RelatedDevice.Data.ResistanceToConnectedDevices) {
			if (Supplie.Value.Contains(PowerTypeCategory.StandardCable)){
				RelatedDevice.ResistanceInput( 1.11111111f, Supplie.Key.GameObject(), null);
				ElectricalSynchronisation.NUCurrentChange.Add (Supplie.Key.InData.ControllingUpdate);
			}
		}
	}

	public void PowerUpdateCurrentChange()
	{
		RelatedDevice.FlushSupplyAndUp(RelatedDevice.gameObject); //Room for optimisation
		CircuitResistance = ElectricityFunctions.WorkOutResistance(RelatedDevice.Data.ResistanceComingFrom[RelatedDevice.gameObject.GetInstanceID()]); // //!!
		ActualVoltage = RelatedDevice.Data.ActualVoltage;

		BatteryCalculation.PowerUpdateCurrentChange(this);

		if (current != Previouscurrent)
		{
			if (Previouscurrent == 0 && !(current <= 0))
			{

			}
			else if (current == 0 && !(Previouscurrent <= 0))
			{
				RelatedDevice.FlushSupplyAndUp(RelatedDevice.gameObject);
			}
			RelatedDevice.Data.SupplyingCurrent = current;
			Previouscurrent = current;
		}
		RelatedDevice.PowerUpdateCurrentChange();
	}

	public void PowerNetworkUpdate()
	{
		RelatedDevice.PowerNetworkUpdate();
		ActualVoltage = RelatedDevice.Data.ActualVoltage;
		BatteryCalculation.PowerNetworkUpdate(this);
//		if (ChangeToOff)
//		{
//			ChangeToOff = false;
//			RelatedDevice.TurnOffSupply();
//			BatteryCalculation.TurnOffEverything(this);
//			ElectricalSynchronisation.RemoveSupply(this, ApplianceType);
//		}
//
		if (current != Previouscurrent)
		{
			RelatedDevice.Data.SupplyingCurrent = current;
			Previouscurrent = current;
			ElectricalSynchronisation.NUCurrentChange.Add(this);
		}

		if (Resistance != PreviousResistance)
		{
			if (PreviousResistance == 0 && !(Resistance == 0))
			{
				resistance.ResistanceAvailable = true;

			}
			else if (Resistance == 0 && !(PreviousResistance <= 0))
			{
				resistance.ResistanceAvailable = false;
				ElectricalDataCleanup.CleanConnectedDevices(RelatedDevice);
			}

			PreviousResistance = Resistance;
			foreach (KeyValuePair<IElectricityIO, HashSet<PowerTypeCategory>> Supplie in RelatedDevice.Data
				.ResistanceToConnectedDevices)
			{
				if (Supplie.Value.Contains(PowerTypeCategory.StandardCable))
				{
					ElectricalSynchronisation.ResistanceChange.Add(this);
					ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingUpdate);
				}
			}
		}

		//Logger.Log (CurrentCapacity.ToString() + " < CurrentCapacity", Category.Electrical);
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

	void UpdateState(bool _isOn)
	{
		isOn = _isOn;
		if (isOn)
		{
			PowerIndicator.sprite = LightOn;

		}
		else
		{
			PowerIndicator.sprite = LightOff;
		}

	}

	void UpdateServerState(bool _isOn)
	{
		isOnForInterface = _isOn;
		if (isOn)
		{
			RelatedDevice.TurnOnSupply();
			PreviousResistance = 0;
			Previouscurrent = 0;
		}
		else
		{
			RelatedDevice.Data.ChangeToOff = true;
			ElectricalSynchronisation.NUStructureChangeReact.Add (this);
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
			if (!RelatedDevice.Data.ChangeToOff)
			{
				isOn = !isOn;
				UpdateServerState(isOn);
			}
		}

		return true;
	}

	public float ModifyElectricityInput( float Current, GameObject SourceInstance, IElectricityIO ComingFrom)
	{
		int InstanceID = SourceInstance.GetInstanceID();

		float ActualCurrent = RelatedDevice.Data.CurrentInWire;

		float Resistance = ElectricityFunctions.WorkOutResistance(RelatedDevice.Data.ResistanceComingFrom[InstanceID]);
		float Voltage = (Current * Resistance);
		//Logger.Log (Voltage.ToString() + " < Voltage " + Resistance.ToString() + " < Resistance" + ActualCurrent.ToString() + " < ActualCurrent" + Current.ToString() + " < Current");
		Tuple<float, float> Currentandoffcut = TransformerCalculations.TransformerCalculate(this, Voltage : Voltage, ResistanceModified : Resistance, ActualCurrent : ActualCurrent);
		if (Currentandoffcut.Item2 > 0)
		{
			if (!(RelatedDevice.Data.CurrentGoingTo.ContainsKey(InstanceID)))
			{
				RelatedDevice.Data.CurrentGoingTo[InstanceID] = new Dictionary<IElectricityIO, float>();
			}
			RelatedDevice.Data.CurrentGoingTo[InstanceID][RelatedDevice.GameObject().GetComponent<IElectricityIO>()] = Currentandoffcut.Item2;
		}
		//return (Current);
		return (Currentandoffcut.Item1);

	}
	public float ModifyElectricityOutput( float Current, GameObject SourceInstance)
	{
		return (Current);
	}
	public float ModifyResistanceInput( float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom)
	{
		return (Resistance);
	}
	public float ModifyResistancyOutput( float Resistance, GameObject SourceInstance)
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
	public void TurnOffCleanup (){
	}
}