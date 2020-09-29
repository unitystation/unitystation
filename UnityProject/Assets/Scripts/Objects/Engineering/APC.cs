using System.Collections;
using System.Collections.Generic;
using Electricity.Inheritance;
using Mirror;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Electricity.PoweredDevices
{
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
		[SyncVar(hook = nameof(SyncVoltage))] private float voltageSync;

		[SerializeField] [Tooltip("Currently unused! If true, this APC will power machinery.")]
		private bool powerMachinery = true;
		[SerializeField] [Tooltip("Currently unused! If true, this APC will power lights.")]
		private bool powerLights = true;
		[SerializeField] [Tooltip("Currently unused! If true, this APC will power environment.")]
		private bool powerEnvironment = true;

		private int cashOfConnectedDevices;
		private bool batteryCharging;

		public float Voltage => voltageSync;

		private float current;
		public float Current => current;

		[SerializeField][FormerlySerializedAs("ElectricalNodeControl")]
		private ElectricalNodeControl ElectricalNodeControl;
		[SerializeField][FormerlySerializedAs("ResistanceSourceModule")]
		private ResistanceSourceModule ResistanceSourceModule;
		[SerializeField][FormerlySerializedAs("NetTabType")]
		private NetTabType netTabType;

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
			if (connectedDevices.Count == 0) return;
			for (int i = connectedDevices.Count -1; i >= 0; i--)
			{
				if (connectedDevices[i] != null)
				{
					continue;
				}

				Debug.Log($"{name} has a null value in {i}.");
				connectedDevices.RemoveAt(i);
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
			if (cashOfConnectedDevices != ElectricalNodeControl.Node.InData.Data.ResistanceToConnectedDevices.Count)
			{
				cashOfConnectedDevices = ElectricalNodeControl.Node.InData.Data.ResistanceToConnectedDevices.Count;
				connectedDepartmentBatteries.Clear();
				foreach (var device in ElectricalNodeControl.Node.InData.Data.ResistanceToConnectedDevices)
				{
					if (device.Key.InData.Categorytype != PowerTypeCategory.DepartmentBattery) continue;

					if (!connectedDepartmentBatteries.Contains(device.Key.GetComponent<DepartmentBattery>()))
					{
						connectedDepartmentBatteries.Add(device.Key.GetComponent<DepartmentBattery>());
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
			ElectricityFunctions.WorkOutActualNumbers(ElectricalNodeControl.Node.InData);
			SyncVoltage(voltageSync, ElectricalNodeControl.Node.InData.Data.ActualVoltage);
			current = ElectricalNodeControl.Node.InData.Data.CurrentInWire;
			HandleDevices();
			UpdateDisplay();
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
			else if (Voltage > 0f)
			{
				State = APCState.Critical;
			}
			else
			{
				State = APCState.Critical;
			}
			if (batteryCharging)
			{
				State = APCState.Charging;
			}
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

			ResistanceSourceModule.Resistance = (1 / CalculatingResistance);
		}


		public void ServerPerformInteraction(HandApply interaction)
		{
			TabUpdateMessage.Send(interaction.Performer, gameObject, netTabType, TabAction.Open);
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

		//######################################## Multitool interaction ##################################
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
}

