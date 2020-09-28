using System.Collections;
using System.Collections.Generic;
using Electric.Inheritance;
using UnityEngine;
using Mirror;

public class APC : SubscriptionController, ICheckedInteractable<HandApply>, INodeControl, IServerDespawn, ISetMultitoolMaster
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

	private void Start()
	{
		CheckListOfDevicesForNulls();
	}

	private void CheckListOfDevicesForNulls()
	{
		if (ConnectedDevices.Count == 0) return;
		for (int i = ConnectedDevices.Count -1; i >= 0; i--)
		{
			if (ConnectedDevices[i] == null)
			{
				UnityEngine.Debug.Log($"{this.name} has a null value in {i}.");
				ConnectedDevices.RemoveAt(i);
			}
		}
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
		if (ElectricalNodeControl == null) return;
		if(ElectricalManager.Instance == null)return;
		if(ElectricalManager.Instance.electricalSync == null)return;
		ElectricalManager.Instance.electricalSync.PoweredDevices.Remove(ElectricalNodeControl);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TargetObject != gameObject) return false;
		if (interaction.HandObject != null) return false;
		return true;
	}

	public void PowerNetworkUpdate()
	{
		//Logger.Log("humm...");
		if (!(CashOfConnectedDevices == ElectricalNodeControl.Node.InData.Data.ResistanceToConnectedDevices.Count))
		{
			CashOfConnectedDevices = ElectricalNodeControl.Node.InData.Data.ResistanceToConnectedDevices.Count;
			ConnectedDepartmentBatteries.Clear();
			foreach (var Device in ElectricalNodeControl.Node.InData.Data.ResistanceToConnectedDevices)
			{
				if (Device.Key.InData.Categorytype == PowerTypeCategory.DepartmentBattery)
				{
					if (!(ConnectedDepartmentBatteries.Contains(Device.Key.GetComponent<DepartmentBattery>())))
					{
						ConnectedDepartmentBatteries.Add(Device.Key.GetComponent<DepartmentBattery>());
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
		ElectricityFunctions.WorkOutActualNumbers(ElectricalNodeControl.Node.InData);
		SyncVoltage(voltageSync, ElectricalNodeControl.Node.InData.Data.ActualVoltage);
		Current = ElectricalNodeControl.Node.InData.Data.CurrentInWire;
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
		float Voltages = Voltage;
		if (Voltages > 270)
		{
			Voltages = 0.001f;
		}
		float CalculatingResistance = new float();

		foreach (APCPoweredDevice Device in ConnectedDevices)
		{
			Device.PowerNetworkUpdate(Voltages);
			CalculatingResistance += (1 / Device.Resistance);
		}

		ResistanceSourceModule.Resistance = (1 / CalculatingResistance);
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
				TriggerSoundOff();
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
		gameObject.SetActive(true);
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
	/// Devices connected to APC
	/// </summary>
	public List<APCPoweredDevice> ConnectedDevices = new List<APCPoweredDevice>();

	// TODO make apcs detect connected department batteries
	/// <summary>
	/// List of the department batteries connected to this APC
	/// </summary>
	public List<DepartmentBattery> ConnectedDepartmentBatteries = new List<DepartmentBattery>();

	private bool _emergencyState = false;
	public void OnDespawnServer(DespawnInfo info)
	{
		for (int i = ConnectedDevices.Count-1; i >= 0; i--)
		{
			ConnectedDevices[i].RemoveFromAPC();
		}
	}
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
			}
		}
	}

	#region Editor

	void OnDrawGizmosSelected()
	{
		var sprite = GetComponentInChildren<SpriteRenderer>();
		if (sprite == null)
			return;

		//Highlighting all controlled lightSources
		Gizmos.color = new Color(0.5f, 0.5f, 1, 1);
		for (int i = 0; i < ConnectedDevices.Count; i++)
		{
			var lightSource = ConnectedDevices[i];
			if(lightSource == null) continue;
			Gizmos.DrawLine(sprite.transform.position, lightSource.transform.position);
			Gizmos.DrawSphere(lightSource.transform.position, 0.25f);
		}
	}

	#endregion

	//######################################## Multitool interaction ##################################
	[SerializeField]
	private MultitoolConnectionType conType = MultitoolConnectionType.APC;
	public MultitoolConnectionType ConType  => conType;

	[SerializeField]
	private bool multiMaster = true;
	public bool MultiMaster  => multiMaster;

	public void AddSlave(object SlaveObject)
	{
	}

	public void RemoveDevice(APCPoweredDevice APCPoweredDevice)
	{
		if (ConnectedDevices.Contains(APCPoweredDevice))
		{
			ConnectedDevices.Remove(APCPoweredDevice);
			APCPoweredDevice.PowerNetworkUpdate(0.1f);

		}
	}
	public void AddDevice(APCPoweredDevice APCPoweredDevice)
	{
		if (!ConnectedDevices.Contains(APCPoweredDevice))
		{
			ConnectedDevices.Add(APCPoweredDevice);
		}
	}

	public override IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects)
	{
		var approvedObjects = new List<GameObject>();

		foreach (var potentialObject in potentialObjects)
		{
			var poweredDevice = potentialObject.GetComponent<APCPoweredDevice>();
			if (poweredDevice == null) continue;
			AddDeviceFromScene(poweredDevice);
			approvedObjects.Add(potentialObject);
		}

		return approvedObjects;
	}

	private void AddDeviceFromScene(APCPoweredDevice poweredDevice)
	{
		if (ConnectedDevices.Contains(poweredDevice))
		{
			ConnectedDevices.Remove(poweredDevice);
			poweredDevice.RelatedAPC = null;
		}
		else
		{
			ConnectedDevices.Add(poweredDevice);
			poweredDevice.RelatedAPC = this;
		}
	}

	public void TriggerSoundOff()
	{
		if(!CustomNetworkManager.IsServer) return;

		StartCoroutine(TriggerSoundOffRoutine());
	}

	private IEnumerator TriggerSoundOffRoutine()
	{
		yield return new WaitForSeconds(1f);

		if (State != APCState.Critical) yield break;

		SoundManager.PlayNetworkedAtPos("APCPowerOff", gameObject.WorldPosServer());
	}
}

