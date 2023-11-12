using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Mirror;
using AddressableReferences;
using Systems.Electricity;
using Systems.Electricity.NodeModules;
using Objects.Lighting;
using Objects.Construction;
using Core.Editor.Attributes;
using CustomInspectors;
using HealthV2;
using Logs;
using Shared.Systems.ObjectConnection;

namespace Objects.Engineering
{
	[RequireComponent(typeof(ElectricalNodeControl))]
	[RequireComponent(typeof(ResistanceSourceModule))]
	public class APC : ImnterfaceMultitoolGUI, ISubscriptionController, INodeControl, ICheckedInteractable<HandApply>, IServerDespawn, IMultitoolMasterable
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

		[Tooltip("Sound used when the APC loses all power.")]
		[SerializeField ]
		private AddressableAudioSource NoPowerSound = null;

		[NonSerialized]
		//Called every power network update
		public UnityEvent<APC> OnPowerNetworkUpdate = new UnityEvent<APC>();

		/// <summary>
		///Used to store all the departmentBatteries that have ever connected to this APC, there might be null values
		///Dont use this to access currently connected batteries.
		/// </summary>
		public List<DepartmentBattery> DepartmentBatteries => departmentBatteries;
		private List<DepartmentBattery> departmentBatteries = new List<DepartmentBattery>();

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
			powerControlSlot = GetComponent<ItemStorage>().GetIndexedItemSlot(0);
			powerCellSlot = GetComponent<ItemStorage>().GetIndexedItemSlot(1);

			electricalNodeControl = GetComponent<ElectricalNodeControl>();
			resistanceSourceModule = GetComponent<ResistanceSourceModule>();
			integrity = GetComponent<Integrity>();

			connectedDevices.RemoveAndSerialize(this, gameObject.scene, device => device == null);
		}
		public override void OnEnable()
		{
			integrity.OnWillDestroyServer.AddListener(WhenDestroyed);
			base.OnEnable();
		}

		private void OnDisable()
		{
			integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);
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

					if (connectedDepartmentBatteries.Contains(device.Key.Data.GetComponent<DepartmentBattery>()) == false)
					{
						connectedDepartmentBatteries.Add(device.Key.Data.GetComponent<DepartmentBattery>());

						if (departmentBatteries.Contains(device.Key.Data.GetComponent<DepartmentBattery>()) == false)
						{
							departmentBatteries.Add(device.Key.Data.GetComponent<DepartmentBattery>());
						}
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
			float voltages = Voltage;

			if (voltages > 270)
			{
				voltages = 0.001f;
			}

			float calculatingResistance = 0f;
			var connectedDevicesCount = connectedDevices.Count;
			for (int i = 0; i < connectedDevicesCount; i++)
			{
				connectedDevices[i].PowerNetworkUpdate(voltages);
				calculatingResistance += (1 / connectedDevices[i].Resistance);
			}

			resistanceSourceModule.Resistance = (1 / calculatingResistance);
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
					_ = SoundManager.PlayAtPosition(NoPowerSound, gameObject.AssumedWorldPosServer());
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
		//	// -----------------------------------------------------
		//	//					INTERACTION THINGS
		//	// -----------------------------------------------------
		/// <summary>
		/// Can this APC not be deconstructed?
		/// </summary>
		public bool canNotBeDeconstructed;

		[Tooltip("Time taken to screwdrive to deconstruct this.")]
		[SerializeField]
		private float secondsToScrewdrive = 2f;

		private ItemSlot powerControlSlot;
		private ItemSlot powerCellSlot;

		[Tooltip("The board that this APC should contain")]
		[SerializeField]
		private GameObject powerControlModule = null;

		[Tooltip("The power cell that this APC uses")]
		[SerializeField]
		private GameObject powerCell = null;

		private Integrity integrity;

		[SerializeField]
		private GameObject APCFrameObj = null;

		#region Multitool Interaction

		public MultitoolConnectionType ConType => MultitoolConnectionType.APC;

		[SerializeField]
		private bool multiMaster = true;
		public bool MultiMaster => multiMaster;

		int IMultitoolMasterable.MaxDistance => 30;

		public void RemoveDevice(APCPoweredDevice apcPoweredDevice)
		{
			if (!connectedDevices.Contains(apcPoweredDevice))
			{
				return;
			}

			connectedDevices.Remove(apcPoweredDevice);
			apcPoweredDevice.OnDeviceUnLinked?.Invoke();
			apcPoweredDevice.PowerNetworkUpdate(0.1f);
		}

		public void AddDevice(APCPoweredDevice apcPoweredDevice)
		{
			if (!connectedDevices.Contains(apcPoweredDevice))
			{
				connectedDevices.Add(apcPoweredDevice);
				apcPoweredDevice.OnDeviceLinked?.Invoke();
			}
		}

		public IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects)
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

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (canNotBeDeconstructed)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "This APC is too well built to be deconstructed.");
				return;
			}

			float voltage = Voltage*10;
			Vector3 shockpos = gameObject.AssumedWorldPosServer();
			Electrocution electrocution = new Electrocution(voltage, shockpos, "APC");

			interaction.Performer.GetComponent<PlayerHealthV2>().Electrocute(electrocution);

			ToolUtils.ServerUseToolWithActionMessages(interaction, secondsToScrewdrive,
					$"You start to disconnect the {gameObject.ExpensiveName()}'s electronics...",
					$"{interaction.Performer.ExpensiveName()} starts to disconnect the {gameObject.ExpensiveName()}'s electronics...",
					$"You disconnect the {gameObject.ExpensiveName()}'s electronics.",
					$"{interaction.Performer.ExpensiveName()} disconnects the {gameObject.ExpensiveName()}'s electronics.",
					() =>
					{
						WhenDestroyed(null);
					});
		}
		public void WhenDestroyed(DestructionInfo info)
		{
			// rare cases were gameObject is destroyed for some reason and then the method is called
			if (gameObject == null) return;

			Inventory.ServerSpawnPrefab(powerControlModule, powerControlSlot, ReplacementStrategy.Cancel);
			Inventory.ServerSpawnPrefab(powerCell, powerCellSlot, ReplacementStrategy.Cancel);

			SpawnResult frameSpawn = Spawn.ServerPrefab(APCFrameObj, SpawnDestination.At(gameObject));
			if (frameSpawn.Successful == false)
			{
				Loggy.LogError($"Failed to spawn frame! Is {this} missing references in the inspector?", Category.Construction);
				return;
			}

			GameObject frame = frameSpawn.GameObject;
			frame.GetComponent<APCFrame>().ServerInitFromComputer(this);

			var Directional = frame.GetComponent<Rotatable>();
			if (Directional != null) Directional.FaceDirection(gameObject.GetComponent<Rotatable>().CurrentDirection);

			_ = Despawn.ServerSingle(gameObject);

			integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);
		}
	}

}

