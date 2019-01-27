using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(APCInteract))]
public class APC : NetworkBehaviour, IElectricalNeedUpdate, IDeviceControl
{
	// -----------------------------------------------------
	//					ELECTRICAL THINGS
	// -----------------------------------------------------
	/// <summary>
	/// Holds information about wire connections to this APC
	/// </summary>
	public PoweredDevice poweredDevice;

	[SyncVar (hook="SetVoltage")]
	private float _voltage = 0;
	/// <summary>
	/// The current voltage of this APC. Calls OnVoltageChange when changed.
	/// </summary>
	public float Voltage
	{
		get
		{
			return _voltage;
		}
		private set
		{
			if(value != _voltage)
			{
				_voltage = value;
				OnVoltageChange();
			}
		}
	}
	public float Current;
	private void OnVoltageChange()
	{
		// Determine the state of the APC using the voltage
		// Changing State will trigger OnStateChange to handle it
		if (Voltage > 219f)
		{
			State = APCState.Full;
		}
		else if (Voltage > 40f)
		{
			State = APCState.Charging;
		}
		else if (Voltage > 0f)
		{
			State = APCState.Critical;
		}
		else
		{
			State = APCState.Dead;
		}
	}

	/// <summary>
	/// Function for setting the voltage via the property. Used for the voltage SyncVar hook.
	/// </summary>
	private void SetVoltage(float newVoltage)
	{
		Voltage = newVoltage;
	}

	private float _resistance = 0;
	/// <summary>
	/// The current resistance of devices connected to this APC.
	/// </summary>
	public float Resistance
	{
		get
		{
			return _resistance;
		}
		set
		{
			if(value != _resistance)
			{
				if (value == 0 || double.IsInfinity(value))
				{
					_resistance = 9999999999;
				}
				else
				{
					_resistance = value;
				}
				dirtyResistance = true;
			}
		}
	}
	/// <summary>
	/// Flag to determine if ElectricalSynchronisation has processed the resistance change yet
	/// </summary>
	private bool dirtyResistance = false;
	/// <summary>
	/// Class to hold resistance so ElectricalSync can have a reference to it
	/// </summary>
	private Resistance ResistanceClass = new Resistance();
	private PowerTypeCategory ApplianceType = PowerTypeCategory.APC;
	private HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>()
	{
		PowerTypeCategory.LowMachineConnector
	};
	private bool SelfDestruct = false;

	public override void OnStartClient()
	{
		base.OnStartClient();
		poweredDevice.InData.CanConnectTo = CanConnectTo;
		poweredDevice.InData.Categorytype = ApplianceType;
		poweredDevice.DirectionStart = 0;
		poweredDevice.DirectionEnd = 9;
		ResistanceClass.Ohms = Resistance;
		ElectricalSynchronisation.PoweredDevices.Add(this);
		PowerInputReactions PRLCable = new PowerInputReactions();
		PRLCable.DirectionReaction = true;
		PRLCable.ConnectingDevice = PowerTypeCategory.LowMachineConnector;
		PRLCable.DirectionReactionA.AddResistanceCall.ResistanceAvailable = true;
		PRLCable.DirectionReactionA.YouShallNotPass = true;
		PRLCable.ResistanceReaction = true;
		PRLCable.ResistanceReactionA.Resistance = ResistanceClass;
		poweredDevice.InData.ConnectionReaction[PowerTypeCategory.LowMachineConnector] = PRLCable;
		poweredDevice.InData.ControllingDevice = this;
		poweredDevice.InData.ControllingUpdate = this;
	}

	private void OnDisable()
	{
		ElectricalSynchronisation.PoweredDevices.Remove(this);
	}

	public void PotentialDestroyed()
	{
		if (SelfDestruct)
		{
			//Then you can destroy
		}
	}

	public void PowerUpdateStructureChange() { }
	public void PowerUpdateStructureChangeReact() { }
	public void InitialPowerUpdateResistance() {
		foreach (KeyValuePair<IElectricityIO,HashSet<PowerTypeCategory>> Supplie in poweredDevice.Data.ResistanceToConnectedDevices) {
			poweredDevice.ResistanceInput(ElectricalSynchronisation.currentTick, 1.11111111f, Supplie.Key.GameObject(), null);
			ElectricalSynchronisation.NUCurrentChange.Add (Supplie.Key.InData.ControllingUpdate);
		}
	}
	public void PowerUpdateResistanceChange() {
		foreach (KeyValuePair<IElectricityIO,HashSet<PowerTypeCategory>> Supplie in poweredDevice.Data.ResistanceToConnectedDevices) {
			poweredDevice.ResistanceInput(ElectricalSynchronisation.currentTick, 1.11111111f, Supplie.Key.GameObject(), null);
			ElectricalSynchronisation.NUCurrentChange.Add (Supplie.Key.InData.ControllingUpdate);
		}

	}
	public void PowerUpdateCurrentChange()
	{

	}

	public void PowerNetworkUpdate()
	{
		Voltage = poweredDevice.Data.ActualVoltage;
		Current = poweredDevice.Data.CurrentInWire;

		UpdateLights();
		if (dirtyResistance)
		{
			ResistanceClass.Ohms = Resistance;
			ElectricalSynchronisation.ResistanceChange.Add (this);
			dirtyResistance = false;
		}
	}

	//FIXME: Objects at runtime do not get destroyed. Instead they are returned back to pool
	//FIXME: that also renderers IDevice useless. Please reassess
	public void OnDestroy()
	{
//		ElectricalSynchronisation.StructureChangeReact = true;
//		ElectricalSynchronisation.ResistanceChange = true;
//		ElectricalSynchronisation.CurrentChange = true;
		ElectricalSynchronisation.PoweredDevices.Remove(this);
		SelfDestruct = true;
		//Making Invisible
	}
	public void TurnOffCleanup ()
	{

	}

	// -----------------------------------------------------
	//					APC STATE THINGS
	// -----------------------------------------------------
	/// <summary>
	/// The current state of the APC, possible values:
	/// <para>
	/// Full, Charging, Critical, Dead
	/// </para>
	/// </summary>
	public enum APCState
	{
		Full, 		// Internal battery full, sufficient power from wire
		Charging,	// Not fully charged, sufficient power from wire to charge.
		Critical,	// Running off of internal battery, not enough power from wire.
		Dead		// Internal battery is empty, no power from wire.
	}
	private APCState _state = APCState.Full;
	/// <summary>
	/// The current state of this APC. Can only be set internally and calls OnStateChange when changed.
	/// </summary>
	public APCState State
	{
		get
		{
			return _state;
		}
		private set
		{
			if (_state != value)
			{
				_state = value;
				OnStateChange();
			}
		}
	}
	private void OnStateChange()
	{
		switch (State)
		{
			case APCState.Full:
				loadedScreenSprites = fullSprites;
				EmergencyState = false;
				if (!RefreshDisplay) StartRefresh();
				break;
			case APCState.Charging:
				loadedScreenSprites = chargingSprites;
				EmergencyState = false;
				if (!RefreshDisplay) StartRefresh();
				break;
			case APCState.Critical:
				loadedScreenSprites = criticalSprites;
				EmergencyState = false;
				if (!RefreshDisplay) StartRefresh();
				break;
			case APCState.Dead:
				screenDisplay.sprite = null;
				EmergencyState = true;
				StopRefresh();
				break;
		}
	}
	// -----------------------------------------------------
	//					DISPLAY THINGS
	// -----------------------------------------------------
	/// <summary>
	/// The screen sprites which are currently being displayed
	/// </summary>
	Sprite[] loadedScreenSprites;
	/// <summary>
	/// The animation sprites for when the APC is in a critical state
	/// </summary>
	public Sprite[] criticalSprites;
	/// <summary>
	/// The animation sprites for when the APC is charging
	/// </summary>
	public Sprite[] chargingSprites;
	/// <summary>
	/// The animation sprites for when the APC is fully charged
	/// </summary>
	public Sprite[] fullSprites;
	/// <summary>
	/// The sprite renderer for the APC display
	/// </summary>
	public SpriteRenderer screenDisplay;
	/// <summary>
	/// The sprite index for the display animation
	/// </summary>
	private int displayIndex = 0;
	/// <summary>
	/// Determines if the screen should refresh or not
	/// </summary>
	private bool RefreshDisplay = false;

	private void StartRefresh()
	{
		RefreshDisplay = true;
		StartCoroutine( Refresh() );
	}
	public void RefreshOnce()
	{
		RefreshDisplay = false;
		StartCoroutine( Refresh() );
	}
	private void StopRefresh()
	{
		RefreshDisplay = false;
	}
	private IEnumerator Refresh()
	{
		RefreshDisplayScreen();
		yield return new WaitForSeconds( 2f );
		if ( RefreshDisplay ) {
			StartCoroutine( Refresh() );
		}
	}

	/// <summary>
	/// Animates the APC screen sprites
	/// </summary>
	private void RefreshDisplayScreen()
	{
		if (++displayIndex >= loadedScreenSprites.Length)
		{
			displayIndex = 0;
		}
		screenDisplay.sprite = loadedScreenSprites[displayIndex];
	}

	// -----------------------------------------------------
	//					CONNECTED LIGHTS AND BATTERY THINGS
	// -----------------------------------------------------
	/// <summary>
	/// The list of emergency lights connected to this APC
	/// </summary>
	public List<EmergencyLightAnimator> ConnectedEmergencyLights = new List<EmergencyLightAnimator>();

	/// <summary>
	/// Dictionary of all the light switches and their lights connected to this APC
	/// </summary>
	public Dictionary<LightSwitchTrigger,List<LightSource>> ConnectedSwitchesAndLights = new Dictionary<LightSwitchTrigger,List<LightSource>> ();

	// TODO make apcs detect connected department batteries
	/// <summary>
	/// List of the department batteries connected to this APC
	/// </summary>
	public List<DepartmentBattery> ConnectedDepartmentBatteries = new List<DepartmentBattery> ();

	/// <summary>
	/// Change brightness of lights connected to this APC proportionally to voltage
	/// </summary>
	public void UpdateLights()
	{
		float CalculatingResistance = new float();
		foreach (KeyValuePair<LightSwitchTrigger,List<LightSource>> SwitchTrigger in  ConnectedSwitchesAndLights)
		{
			SwitchTrigger.Key.PowerNetworkUpdate (Voltage);
			if (SwitchTrigger.Key.isOn)
			{
				for (int i = 0; i < SwitchTrigger.Value.Count; i++)
				{
					SwitchTrigger.Value[i].PowerLightIntensityUpdate(Voltage);
					CalculatingResistance += (1/SwitchTrigger.Value [i].Resistance);
				}
			}

		}
		Resistance = (1 / CalculatingResistance);
	}

	private bool _emergencyState = false;
	/// <summary>
	/// The state of the emergency lights. Calls SetEmergencyLights when changes.
	/// </summary>
	private bool EmergencyState
	{
		get
		{
			return _emergencyState;
		}
		set
		{
			if (_emergencyState != value)
			{
				_emergencyState = value;
				SetEmergencyLights(value);
			}
		}
	}
	/// <summary>
	/// Set the state of the emergency lights associated with this APC
	/// </summary>
	void SetEmergencyLights(bool isOn)
	{
		if (ConnectedEmergencyLights.Count == 0)
		{
			return;
		}
		for (int i = 0; i < ConnectedEmergencyLights.Count; i++)
		{
			ConnectedEmergencyLights[i].Toggle(isOn);
		}
	}
}