using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using Electricity.Inheritance;
using Systems.Electricity;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;
using Systems.Electricity.NodeModules;
using Objects.Lighting;
using UnityEngine.Events;

namespace Objects.Engineering
{
	[RequireComponent(typeof(ElectricalNodeControl))]
	[RequireComponent(typeof(ResistanceSourceModule))]
	public class APC : SubscriptionController, INodeControl, IServerDespawn, ISetMultitoolMaster
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
		[SyncVar(hook = nameof(SyncVoltage))] private float voltageSync;

		[SerializeField] [Tooltip("Currently unused! If true, this APC will power machinery.")]
		private bool powerMachinery = true;
		[SerializeField] [Tooltip("Currently unused! If true, this APC will power lights.")]
		private bool powerLights = true;
		[SerializeField] [Tooltip("Currently unused! If true, this APC will power environment.")]
		private bool powerEnvironment = true;

		private int cacheOfConnectedDevices;
		private bool batteryCharging;

		public float Voltage => voltageSync;

		public float Current { get; private set; }

		private ElectricalNodeControl electricalNodeControl;
		private ResistanceSourceModule resistanceSourceModule;


		[SerializeField, FormerlySerializedAs("NetTabType")]
		private NetTabType netTabType = NetTabType.APC;

		[Tooltip("Sound used when the APC loses all power.")]
		[SerializeField] private AddressableAudioSource NoPowerSound = null;

		[NonSerialized]
		//Called every power network update
		public UnityEvent<APC> OnPowerNetworkUpdate = new UnityEvent<APC>();

		/// <summary>
		/// Function for setting the voltage via the property. Used for the voltage SyncVar hook.
		/// </summary>
		private void SyncVoltage(float oldVoltage, float newVoltage)
		{
			voltageSync = newVoltage;
			UpdateDisplay();
		}

		#region Lifecycyle

		private void Awake()
		{
			electricalNodeControl = GetComponent<ElectricalNodeControl>();
			resistanceSourceModule = GetComponent<ResistanceSourceModule>();
		}

		private void Start()
		{
			CheckListOfDevicesForNulls();
		}

		private void CheckListOfDevicesForNulls()
		{
			if (connectedDevices.Count == 0) return;
			for (int i = connectedDevices.Count - 1; i >= 0; i--)
			{
				if (connectedDevices[i] != null)
				{
					continue;
				}

				Logger.Log($"{name} has a null value in {i}.", Category.Electrical);
				connectedDevices.RemoveAt(i);
			}
		}

		private void OnDisable()
		{
			if (electricalNodeControl == null) return;
			if(ElectricalManager.Instance == null)return;
			if(ElectricalManager.Instance.electricalSync == null)return;
			ElectricalManager.Instance.electricalSync.PoweredDevices.Remove(electricalNodeControl);
		}

		#endregion

		public void PowerNetworkUpdate()
		{
			if (cacheOfConnectedDevices != electricalNodeControl.Node.InData.Data.ResistanceToConnectedDevices.Count)
			{
				cacheOfConnectedDevices = electricalNodeControl.Node.InData.Data.ResistanceToConnectedDevices.Count;
				connectedDepartmentBatteries.Clear();
				foreach (var device in electricalNodeControl.Node.InData.Data.ResistanceToConnectedDevices)
				{

					if (device.Key.Data.InData.Categorytype != PowerTypeCategory.DepartmentBattery) continue;

					if (!connectedDepartmentBatteries.Contains(device.Key.Data.GetComponent<DepartmentBattery>()))
					{
						connectedDepartmentBatteries.Add(device.Key.Data.GetComponent<DepartmentBattery>());
					}
				}
			}
			batteryCharging = false;
			foreach (var bat in connectedDepartmentBatteries)
			{
				if (bat.BatterySupplyingModule.ChargingWatts > 0)
				{
					batteryCharging = true;
				}
			}
			ElectricityFunctions.WorkOutActualNumbers(electricalNodeControl.Node.InData);
			SyncVoltage(voltageSync, electricalNodeControl.Node.InData.Data.ActualVoltage);
			Current = electricalNodeControl.Node.InData.Data.CurrentInWire;
			HandleDevices();

			OnPowerNetworkUpdate.Invoke(this);
		}

		private void UpdateDisplay()
		{
			if (Voltage > 270)
			{
				State = APCState.Critical;
			}
			else if (Voltage > 219f)
			{
				State = APCState.Full;
			}
			else if (batteryCharging)
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

		private float CalculateMaxCapacity()
		{
			float newCapacity = 0;
			foreach (DepartmentBattery battery in ConnectedDepartmentBatteries)
			{
				newCapacity += battery.BatterySupplyingModule.CapacityMax;
			}

			return newCapacity;
		}

		//Percentage in decimal, 0-1
		public float CalculateChargePercentage()
		{
			var maxCapacity = CalculateMaxCapacity();

			if (maxCapacity.Approx(0))
			{
				return 0;
			}

			float newCapacity = 0;
			foreach (DepartmentBattery battery in ConnectedDepartmentBatteries)
			{
				newCapacity += battery.BatterySupplyingModule.CurrentCapacity;
			}

			return (newCapacity / maxCapacity);
		}

		// Percentage as string, 0% to 100%
		public string CalculateChargePercentageString()
		{
			return CalculateChargePercentage().ToString("P0");
		}

		/// <summary>
		/// Change brightness of lights connected to this APC proportionally to voltage
		/// </summary>
		private void HandleDevices()
		{
			float Voltages = Voltage;
			if (Voltages > 270)
			{
				Voltages = 0.001f;
			}
			float CalculatingResistance = new float();

			foreach (APCPoweredDevice Device in connectedDevices)
			{
				Device.PowerNetworkUpdate(Voltages);
				CalculatingResistance += (1 / Device.Resistance);
			}

			resistanceSourceModule.Resistance = (1 / CalculatingResistance);
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
					_ = SoundManager.PlayAtPosition(NoPowerSound, gameObject.WorldPosServer());
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
		[SerializeField][FormerlySerializedAs("ConnectedDevices")]
		private List<APCPoweredDevice> connectedDevices = new List<APCPoweredDevice>();
		public List<APCPoweredDevice> ConnectedDevices => connectedDevices;

		// TODO make apcs detect connected department batteries
		/// <summary>
		/// List of the department batteries connected to this APC
		/// </summary>
		[SerializeField][FormerlySerializedAs("ConnectedDepartmentBatteries")]
		private List<DepartmentBattery> connectedDepartmentBatteries = new List<DepartmentBattery>();
		public List<DepartmentBattery> ConnectedDepartmentBatteries => connectedDepartmentBatteries;

		private bool _emergencyState = false;
		public void OnDespawnServer(DespawnInfo info)
		{
			for (int i = connectedDevices.Count-1; i >= 0; i--)
			{
				connectedDevices[i].RemoveFromAPC();
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
			for (int i = 0; i < connectedDevices.Count; i++)
			{
				var lightSource = connectedDevices[i];
				if(lightSource == null) continue;
				Gizmos.DrawLine(sprite.transform.position, lightSource.transform.position);
				Gizmos.DrawSphere(lightSource.transform.position, 0.25f);
			}
		}

		#endregion

		#region Multitool Interaction

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.APC;
		public MultitoolConnectionType ConType  => conType;

		[SerializeField]
		private bool multiMaster = true;
		public bool MultiMaster  => multiMaster;

		public void AddSlave(object slaveObject)
		{
		}

		public void RemoveDevice(APCPoweredDevice apcPoweredDevice)
		{
			if (!connectedDevices.Contains(apcPoweredDevice))
			{
				return;
			}

			connectedDevices.Remove(apcPoweredDevice);
			apcPoweredDevice.PowerNetworkUpdate(0.1f);
		}

		public void AddDevice(APCPoweredDevice apcPoweredDevice)
		{
			if (!connectedDevices.Contains(apcPoweredDevice))
			{
				connectedDevices.Add(apcPoweredDevice);
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
			if (connectedDevices.Contains(poweredDevice))
			{
				connectedDevices.Remove(poweredDevice);
				poweredDevice.RelatedAPC = null;
			}
			else
			{
				connectedDevices.Add(poweredDevice);

				if (poweredDevice.RelatedAPC != null)
				{
					//Already connected to something so remove it
					poweredDevice.RelatedAPC.RemoveDevice(poweredDevice);
				}

				poweredDevice.RelatedAPC = this;
			}
		}

		#endregion
	}
}
