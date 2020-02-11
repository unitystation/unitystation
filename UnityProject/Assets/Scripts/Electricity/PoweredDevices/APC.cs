using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class APC : NetworkBehaviour, IInteractable<HandApply>, INodeControl
{
	// -----------------------------------------------------
	//					ELECTRICAL THINGS
	// -----------------------------------------------------
	/// <summary>
	/// Holds information about wire connections to this APC
	/// </summary>


	/// <summary>
	/// The current voltage of this APC. Calls OnVoltageChange when changed.
	/// </summary>
	[SyncVar(hook = nameof(SyncVoltage))]
	private float voltageSync;
	public bool PowerMachinery = true;
	public bool PowerLights = true;
	public bool PowerEnvironment = true;
	public bool BatteryCharging = false;
	public int CashOfConnectedDevices = 0;

	public float Voltage => voltageSync;

	public float Current;

	public ElectricalNodeControl ElectricalNodeControl;

	public ResistanceSourceModule ResistanceSourceModule;

	/// <summary>
	/// Function for setting the voltage via the property. Used for the voltage SyncVar hook.
	/// </summary>
	private void SyncVoltage(float oldVoltage, float newVoltage)
	{
		voltageSync = newVoltage;
		UpdateDisplay();
	}

	public override void OnStartServer()
	{
		SyncVoltage(voltageSync, voltageSync);
	}

	public override void OnStartClient()
	{
		SyncVoltage(voltageSync, voltageSync);
	}

	private void OnDisable()
	{
		ElectricalSynchronisation.PoweredDevices.Remove(ElectricalNodeControl);
	}

	public void PowerNetworkUpdate()
	{
		//Logger.Log("humm...");
		if (!(CashOfConnectedDevices == ElectricalNodeControl.Node.Data.ResistanceToConnectedDevices.Count))
		{
			CashOfConnectedDevices = ElectricalNodeControl.Node.Data.ResistanceToConnectedDevices.Count;
			ConnectedDepartmentBatteries.Clear();
			foreach (KeyValuePair<ElectricalOIinheritance, HashSet<PowerTypeCategory>> Device in ElectricalNodeControl.Node.Data.ResistanceToConnectedDevices)
			{
				if (Device.Key.InData.Categorytype == PowerTypeCategory.DepartmentBattery)
				{
					if (!(ConnectedDepartmentBatteries.Contains(Device.Key.GameObject().GetComponent<DepartmentBattery>())))
					{
						ConnectedDepartmentBatteries.Add(Device.Key.GameObject().GetComponent<DepartmentBattery>());
					}
				}
			}
		}
		BatteryCharging = false;
		foreach (var bat in ConnectedDepartmentBatteries)
		{
			if (bat.BatterySupplyingModule.ChargingWatts > 0)
			{
				BatteryCharging = true;
			}
		}
		SyncVoltage(voltageSync, ElectricalNodeControl.Node.Data.ActualVoltage);
		Current = ElectricalNodeControl.Node.Data.CurrentInWire;
		HandleDevices();
		UpdateDisplay();
	}

	public void UpdateDisplay()
	{
		if (Voltage > 270)
		{
			State = APCState.Critical;
		}
		else if (Voltage > 219f)
		{
			State = APCState.Full;
		}
		else if (Voltage > 0f)
		{
			State = APCState.Critical;
		}
		else
		{
			State = APCState.Critical;
		}
		if (BatteryCharging)
		{
			State = APCState.Charging;
		}
	}

	/// <summary>
	/// Change brightness of lights connected to this APC proportionally to voltage
	/// </summary>
	public void HandleDevices()
	{
		//Lights
		float Voltages = Voltage;
		if (Voltages > 270)
		{
			Voltages = 0.001f;
		}
		float CalculatingResistance = new float();
		if (PowerLights)
		{
			foreach (KeyValuePair<LightSwitch, List<LightSource>> SwitchTrigger in ConnectedSwitchesAndLights)
			{
				SwitchTrigger.Key.PowerNetworkUpdate(Voltages);
				if (SwitchTrigger.Key.isOn == LightSwitch.States.On)
				{
					for (int i = 0; i < SwitchTrigger.Value.Count; i++)
					{
						SwitchTrigger.Value[i].PowerLightIntensityUpdate(Voltages);
						CalculatingResistance += (1 / SwitchTrigger.Value[i].Resistance);
					}
				}
			}
		}
		else {
			foreach (KeyValuePair<LightSwitch, List<LightSource>> SwitchTrigger in ConnectedSwitchesAndLights)
			{
				SwitchTrigger.Key.PowerNetworkUpdate(0);
				if (SwitchTrigger.Key.isOn == LightSwitch.States.On)
				{
					for (int i = 0; i < SwitchTrigger.Value.Count; i++)
					{
						SwitchTrigger.Value[i].PowerLightIntensityUpdate(0);
					}
				}
			}
		}

		//Machinery
		if (PowerMachinery)
		{
			foreach (APCPoweredDevice Device in ConnectedDevices)
			{

				Device.PowerNetworkUpdate(Voltages);
				CalculatingResistance += (1 / Device.Resistance);
			}
		}
		else {
			foreach (APCPoweredDevice Device in ConnectedDevices)
			{
				Device.PowerNetworkUpdate(0);
			}
		}
		//Environment
		if (PowerEnvironment)
		{
			foreach (APCPoweredDevice Device in EnvironmentalDevices)
			{
				Device.PowerNetworkUpdate(Voltages);
				CalculatingResistance += (1 / Device.Resistance);
			}
		}
		else {
			foreach (APCPoweredDevice Device in EnvironmentalDevices)
			{
				Device.PowerNetworkUpdate(0);
			}
		}
		ResistanceSourceModule.Resistance = (1 / CalculatingResistance);
	}

	public void FindPoweredDevices()
	{
		//yeah They be manually assigned for now
		//needs a way of checking that doesn't cause too much lag and  can respond adequately to changes in the environment E.G a device getting destroyed/a new device being made
	}


	public NetTabType NetTabType;

	public void ServerPerformInteraction(HandApply interaction)
	{
		TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType, TabAction.Open);
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
		Full,       // Internal battery full, sufficient power from wire
		Charging,   // Not fully charged, sufficient power from wire to charge.
		Critical,   // Running off of internal battery, not enough power from wire.
		Dead        // Internal battery is empty, no power from wire.
	}
	private APCState _state = APCState.Full;
	//	/// <summary>
	//	/// The current state of this APC. Can only be set internally and calls OnStateChange when changed.
	//	/// </summary>
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
				EmergencyState = true;
				if (!RefreshDisplay) StartRefresh();
				break;
			case APCState.Dead:
				screenDisplay.sprite = null;
				EmergencyState = true;
				StopRefresh();
				break;
		}
	}
	//	// -----------------------------------------------------
	//	//					DISPLAY THINGS
	//	// -----------------------------------------------------
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
		StartCoroutine(Refresh());
	}
	public void RefreshOnce()
	{
		RefreshDisplay = false;
		StartCoroutine(Refresh());
	}
	private void StopRefresh()
	{
		RefreshDisplay = false;
	}
	private IEnumerator Refresh()
	{
		RefreshDisplayScreen();
		yield return WaitFor.Seconds(2f);
		if (RefreshDisplay)
		{
			StartCoroutine(Refresh());
		}
	}

	//	///// <summary>
	//	///// Animates the APC screen sprites
	//	///// </summary>
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
	private List<EmergencyLightAnimator> ConnectedEmergencyLights = new List<EmergencyLightAnimator>();

	/// <summary>
	/// Dictionary of all the light switches and their lights connected to this APC
	/// </summary>
	public Dictionary<LightSwitch, List<LightSource>> ConnectedSwitchesAndLights = new Dictionary<LightSwitch, List<LightSource>>();

	/// <summary>
	/// list of connected machines to the APC
	/// </summary>
	public List<APCPoweredDevice> ConnectedDevices = new List<APCPoweredDevice>();

	/// <summary>
	/// list of connected machines to the APC
	/// </summary>
	public List<APCPoweredDevice> EnvironmentalDevices = new List<APCPoweredDevice>();

	// TODO make apcs detect connected department batteries
	/// <summary>
	/// List of the department batteries connected to this APC
	/// </summary>
	public List<DepartmentBattery> ConnectedDepartmentBatteries = new List<DepartmentBattery>();

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
	/// Register the light with this APC, so that it will be toggled based on emergency status
	/// </summary>
	/// <param name="newLight"></param>
	public void ConnectEmergencyLight(EmergencyLightAnimator newLight)
	{
		if (!ConnectedEmergencyLights.Contains(newLight))
		{
			ConnectedEmergencyLights.Add(newLight);
			SetEmergencyLights(EmergencyState);
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
			if (ConnectedEmergencyLights[i]) //might be destroyed
			{
				ConnectedEmergencyLights[i].Toggle(isOn);
			}
		}
	}

}