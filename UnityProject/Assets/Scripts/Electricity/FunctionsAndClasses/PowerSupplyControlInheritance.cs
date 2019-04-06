using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PowerSupplyControlInheritance : InputTrigger, IDeviceControl
{
	public bool SelfDestruct = false;
	[SerializeField]
	public InLineDevice powerSupply;
	[SyncVar(hook = "UpdateState")]
	public bool isOn = false;
	public int DirectionStart = 0;
	public int DirectionEnd = 9;
	public float MonitoringResistance = 0;
	//[SerializeField]
	public float current { get; set; } = 0;
	public float Previouscurrent = 0;
	public float SupplyingVoltage = 0;
	public float PreviousSupplyingVoltage = 0;
	public float InternalResistance = 0;
	public float PreviousInternalResistance = 0;
	[Header("Transformer Settings")]
	public float TurnRatio; //the Turn ratio of the transformer so if it 2, 1v in 2v out 
	public float VoltageLimiting; //If it requires VoltageLimiting and  At what point the VoltageLimiting will kick in
	public float VoltageLimitedTo;  //what it will be limited to

	public ElectricalOIinheritance _IElectricityIO { get; set; }
	public PowerTypeCategory ApplianceType { get; set; }
	public HashSet<PowerTypeCategory> CanConnectTo { get; set; }


	public  Resistance resistance { get; set; } = new Resistance();
	[Header("Battery Settings")]
	public  float MaximumCurrentSupport = 0; //The maximum number of amps can be pulled From the battery 
	public  float MinimumSupportVoltage = 0; //At which point the battery kicks in
	public  float StandardSupplyingVoltage = 0;
	public  float CapacityMax = 0;
	public  float CurrentCapacity = 0;
	public  float ExtraChargeCutOff  = 0; //if  The voltages less than this it will decrease the charge steps until A it is not or B it reaches zero then stops charging
	public  float IncreasedChargeVoltage = 0; // At what voltage the charge multiplier will increase
	public  float StandardChargeNumber  = 0; //Basically part of the multiplier of how much it should charge
	public  float ChargeSteps = 0; //The steps it will go up by when adjusting the charge current
	public  float MaxChargingMultiplier = 0; 
	public  float ChargingMultiplier = 0; 
	public  bool CanCharge = false;
	public  bool Cansupport = false;
	public  bool ToggleCanCharge = false;
	public  bool ToggleCansupport = false;

	[Header("Don't touch")]
	public float PullingWatts = 0;
	public float ChargingWatts = 0;
	public float Resistance = 0;
	public float PreviousResistance = 0;
	public float CircuitResistance = 0;
	public float ActualVoltage = 0;
	public float PullLastDeductedTime = 0;
	public float ChargLastDeductedTime = 0;
	public bool PassChangeToOff = false;
	public bool isOnForInterface = false;

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
		_IElectricityIO = powerSupply;
		powerSupply.RelatedDevice = this;
		powerSupply.InData.ControllingDevice = this;
		powerSupply.InData.ControllingUpdate = this;
		OnStartServerInitialise();
	}
	public virtual void OnStartServerInitialise()
	{
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		UpdateState(isOn);
	}

	public void UpdateState(bool _isOn)
	{
		isOn = _isOn;
		StateChange(isOn);
	}
	public virtual void StateChange(bool isOn)
	{
	}

	public virtual void UpdateServerState(bool _isOn)
	{
		if (isOn)
		{
			powerSupply.TurnOnSupply();
			PreviousResistance = 0;
			Previouscurrent = 0;
		}
		else
		{
			powerSupply.TurnOffSupply();
		}
	}

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		//Logger.Log("this");
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
		_Interact(originator, position, hand);
		return true;
	}
	public virtual void _Interact(GameObject originator, Vector3 position, string hand)
	{
	}

	public virtual void TurnOffCleanup()
	{
		_TurnOffCleanup();	}
	public virtual void _TurnOffCleanup()
	{
	}

	public virtual void PowerUpdateStructureChange() {
		powerSupply.PowerUpdateStructureChange();
		_PowerUpdateStructureChange();
	}
	public virtual void _PowerUpdateStructureChange()
	{
	}

	public virtual void PowerUpdateStructureChangeReact() { 
		powerSupply.PowerUpdateStructureChangeReact();
		_PowerUpdateStructureChangeReact();
	}
	public virtual void _PowerUpdateStructureChangeReact()
	{
	}
	public virtual void InitialPowerUpdateResistance() {
		powerSupply.InitialPowerUpdateResistance();
		//this ok 
		foreach (KeyValuePair<ElectricalOIinheritance, HashSet<PowerTypeCategory>> Supplie in powerSupply.Data.ResistanceToConnectedDevices)
		{
			powerSupply.ResistanceInput(1.11111111f, Supplie.Key.GameObject(), null);
			ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingUpdate);
		}
		_InitialPowerUpdateResistance();
	}
	public virtual void _InitialPowerUpdateResistance()
	{
	}
	public virtual void PowerUpdateResistanceChange() { 
		powerSupply.PowerUpdateResistanceChange();
		foreach (KeyValuePair<ElectricalOIinheritance, HashSet<PowerTypeCategory>> Supplie in powerSupply.Data.ResistanceToConnectedDevices)
		{
			//Logger.Log("4");
			powerSupply.ResistanceInput(1.11111111f, Supplie.Key.GameObject(), null);
			ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingUpdate);
		}
		_PowerUpdateResistanceChange();
	}
	public virtual void _PowerUpdateResistanceChange()
	{
	}
	public virtual void PowerUpdateCurrentChange()
	{
		if (powerSupply.Data.ResistanceComingFrom.Count > 0)
		{
			powerSupply.FlushSupplyAndUp(powerSupply.gameObject); //Room for optimisation
			CircuitResistance = ElectricityFunctions.WorkOutResistance(powerSupply.Data.ResistanceComingFrom[powerSupply.gameObject.GetInstanceID()]); // //!!
			ActualVoltage = powerSupply.Data.ActualVoltage;

			BatteryCalculation.PowerUpdateCurrentChange(this);

			if (current != Previouscurrent)
			{
				if (Previouscurrent == 0 && !(current <= 0))
				{

				}
				else if (current == 0 && !(Previouscurrent <= 0))
				{
					powerSupply.FlushSupplyAndUp(powerSupply.gameObject);
				}
				powerSupply.Data.SupplyingCurrent = current;
				Previouscurrent = current;
			}
		}
	

		powerSupply.PowerUpdateCurrentChange();
		_PowerUpdateCurrentChange();
	}
	public virtual void _PowerUpdateCurrentChange()
	{
	}
	public virtual void PowerNetworkUpdate() {
		powerSupply.PowerNetworkUpdate();
		ActualVoltage = powerSupply.Data.ActualVoltage;
		BatteryCalculation.PowerNetworkUpdate(this);

		if (current != Previouscurrent | SupplyingVoltage != PreviousSupplyingVoltage | InternalResistance != PreviousInternalResistance )
		{
			powerSupply.Data.SupplyingCurrent = current;
			Previouscurrent = current;

			powerSupply.Data.SupplyingVoltage = SupplyingVoltage;
			PreviousSupplyingVoltage = SupplyingVoltage;

			powerSupply.Data.InternalResistance = InternalResistance;
			PreviousInternalResistance = InternalResistance;

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
				ElectricalDataCleanup.CleanConnectedDevices(powerSupply);
			}

			PreviousResistance = Resistance;
			foreach (KeyValuePair<ElectricalOIinheritance, HashSet<PowerTypeCategory>> Supplie in powerSupply.Data
				.ResistanceToConnectedDevices)
			{
				if (Supplie.Value.Contains(PowerTypeCategory.StandardCable))
				{
					ElectricalSynchronisation.ResistanceChange.Add(this);
					ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingUpdate);
				}
			}
		}
		//Logger.Log(CurrentCapacity + " < CurrentCapacity", Category.Electrical);
		_PowerNetworkUpdate();

	}
	public virtual void _PowerNetworkUpdate()
	{
	}

	public GameObject GameObject() {
		return (gameObject);
	}

	public virtual float ModifyElectricityInput(float Current, GameObject SourceInstance, ElectricalOIinheritance ComingFrom) { 
		return (Current);
	}
	public virtual float ModifyElectricityOutput(float Current, GameObject SourceInstance) { 
		return (Current);
	}

	public virtual float ModifyResistanceInput(float Resistance, GameObject SourceInstance, ElectricalOIinheritance ComingFrom) { 
		return (Resistance);
	}
	public virtual float ModifyResistancyOutput(float Resistance, GameObject SourceInstance) {
		return (Resistance);
	}
}
