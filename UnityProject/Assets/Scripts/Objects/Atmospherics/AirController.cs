using System;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using ScriptableObjects.Atmospherics;
using Systems.Atmospherics;
using Systems.Electricity;
using Objects.Wallmounts;
using Shared.Systems.ObjectConnection;
using Systems.Clearance;
using UI.Objects.Atmospherics.Acu;
using Items;


namespace Objects.Atmospherics
{
	/// <summary>
	/// Represents an <c>ACU</c>'s air quality status.
	/// <remarks>Note: maps to <c>ACU</c> sprite state.</remarks>
	/// </summary>
	public enum AcuStatus
	{
		Off = 0,
		Nominal = 1,
		Caution = 2,
		Alert = 3,
	}

	/// <summary> All operating modes for the <c>ACU</c>.</summary>
	public enum AcuMode
	{
		Off = 0, // Do nothing.
		Filtering = 1, // Only remove contaminants (ignores pressure).
		Contaminated = 2, // Remove contaminants quicker (a (lot) more power used).
		Draught = 3, // Siphon while filling. Useful for displacing poor air with nominal air, particularly in populated areas.
		Refill = 4, // Automatically refill lost air to no more than one atmosphere.
		Cycle = 5, // Siphon most air, then refill. Useful for thoroughly replacing poor air with nominal air, but not suitable for populated areas.
		Siphon = 6, // Siphon the air from the area.
		PanicSiphon = 7, // Quickly siphon the air from the area.
	}

	/// <summary>
	/// <para>Main component for the <c>ACU</c> (known as the air alarm in SS13).</para>
	/// <para>Monitors the local air quality and controls connected <c>ACU</c> devices, such as vents and scrubbers.</para>
	/// <remarks>See also related classes:<list type="bullet">
	/// <item><description><seealso cref="GUI_Acu"/> handles the <c>ACU</c>'s GUI</description></item>
	/// <item><description><seealso cref="AcuDevice"/> allows a connection to form between the <c>ACU</c> and devices</description></item>
	/// </list></remarks>
	/// </summary>
	[RequireComponent(typeof(WallmountBehavior))]
	[RequireComponent(typeof(ClearanceRestricted))]
	public class AirController : MonoBehaviour, IServerSpawn, IAPCPowerable, IMultitoolMasterable, ICheckedInteractable<HandApply>, IExaminable
	{
		[InfoBox("Several presets exist for server rooms, cold rooms, etc. Add as desired.")]

		[SerializeField]
		[Tooltip("Initial operating mode for this ACU and its slaved devices.")]
		private AcuMode initialOperatingMode = AcuMode.Filtering;

		[SerializeField]
		[Tooltip("Initial threshold values with which air quality will be assessed.")]
		private AcuThresholds initialAcuThresholds = default;

		[SerializeField]
		[Tooltip("Whether the ACU should be an air quality sampling source. " +
		         "Disable if the ACU is not in the room it controls. Important to disable for Atmospherics reservoir ACUs.")]
		private bool acuSamplesAir = true;

		private MetaDataNode facingMetaNode;
		private ClearanceRestricted restricted;
		private SpriteHandler spriteHandler;

		/// <summary>Invoked when the air controller's state changes.</summary>
		public Action OnStateChanged;

		public readonly HashSet<IAcuControllable> ConnectedDevices = new HashSet<IAcuControllable>();

		/// <summary>
		/// The mode this controller and the devices it controls should operate with,
		/// given other conditions, like being powered, are met.
		/// </summary>
		public AcuMode DesiredMode { get; private set; }

		/// <summary>
		/// Represents the average values over each <c>GasMix</c>  devices (including the controller, if enabled).
		/// BLAH
		/// </summary>
		public AcuSampleAverage AtmosphericAverage { get; private set; } = new AcuSampleAverage();

		public AcuThresholds Thresholds { get; private set; }

		public bool IsWriteable => IsPowered && IsLocked == false;

		private readonly AcuSample acuSample = new AcuSample();

		private bool isEmagged = false;

		#region Lifecycle

		private bool IsReady => facingMetaNode != null;

		private void Awake()
		{
			restricted = GetComponent<ClearanceRestricted>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();

			Thresholds = initialAcuThresholds.Clone();
			GasLevelStatus = new AcuStatus[Gas.Gases.Count];
			DesiredMode = initialOperatingMode;
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			var registerTile = gameObject.RegisterTile();
			var localPos = registerTile.LocalPositionServer + GetComponent<WallmountBehavior>().CalculateFacing();
			facingMetaNode = registerTile.Matrix.MatrixInfo.MetaDataLayer.Get(localPos.CutToInt());

			if (IsPowered)
			{
				UpdateManager.Add(PeriodicUpdate, 3);
				PeriodicUpdate();
			}
		}

		#endregion

		private void PeriodicUpdate()
		{
			UpdateAtmosphericAverage();
			UpdateStatusProperties();
			spriteHandler.ChangeSprite((int)OverallStatus);

			// Cycle will vacuum air, and then refill.
			if (DesiredMode == AcuMode.Cycle && AtmosphericAverage.Pressure < AtmosConstants.ONE_ATMOSPHERE / 20)
			{
				SetOperatingMode(AcuMode.Refill);
			}
			// Slightly different to /tg/: refill will now return to the initial operating mode once one atmosphere is reached.
			else if (DesiredMode == AcuMode.Refill && AtmosphericAverage.Pressure > AtmosConstants.ONE_ATMOSPHERE)
			{
				SetOperatingMode(initialOperatingMode);
			}
		}

		private void UpdateAtmosphericAverage()
		{
			AtmosphericAverage.Clear();
			foreach (IAcuControllable device in ConnectedDevices)
			{
				AtmosphericAverage.AddSample(device.AtmosphericSample);
			}

			if (acuSamplesAir)
			{
				acuSample.FromGasMix(facingMetaNode.GasMix);
				AtmosphericAverage.AddSample(acuSample);
			}
		}

		#region Status

		public AcuStatus OverallStatus { get; private set; } = AcuStatus.Off;
		public AcuStatus PressureStatus { get; private set; }
		public AcuStatus TemperatureStatus { get; private set; }
		public AcuStatus[] GasLevelStatus { get; private set; }
		public AcuStatus CompositionStatus { get; private set; }

		private void UpdateStatusProperties()
		{
			if (AtmosphericAverage.SampleSize < 1)
			{
				OverallStatus = PressureStatus = TemperatureStatus = CompositionStatus = AcuStatus.Caution;
				return;
			}

			PressureStatus = GetMetricStatus(Thresholds.Pressure, AtmosphericAverage.Pressure);
			TemperatureStatus = GetMetricStatus(Thresholds.Temperature, AtmosphericAverage.Temperature);
			CompositionStatus = AcuStatus.Nominal;

			// We need to loop over all possible gases (or more specifically, a union of detected gases and recognized gases).
			foreach (GasSO gas in Gas.Gases.Values)
			{
				bool gasDetected = AtmosphericAverage.HasGas(gas);
				if (gasDetected && Thresholds.GasMoles.ContainsKey(gas) == false)
				{
					// Let thresholds know about this unrecognized gas, but values are undetermined (let a technician set them).
					Thresholds.GasMoles.Add(gas, AcuThresholds.UnknownValues);
				}

				GasLevelStatus[gas] = gasDetected
					? GetMetricStatus(Thresholds.GasMoles[gas], AtmosphericAverage.GetGasMoles(gas))
					: AcuStatus.Nominal;
				CompositionStatus = GasLevelStatus[gas] > CompositionStatus? GasLevelStatus[gas] : CompositionStatus;
			}

			OverallStatus = PressureStatus;
			OverallStatus = TemperatureStatus > OverallStatus ? TemperatureStatus : OverallStatus;
			OverallStatus = CompositionStatus > OverallStatus ? CompositionStatus : OverallStatus;
		}

		private AcuStatus GetMetricStatus(float[] thresholds, float value)
		{
			for (int i = 0; i < thresholds.Length; i++)
			{
				// ACU does not have thresholds data set for this newly-registered gas.
				if (float.IsNaN(thresholds[i])) return AcuStatus.Caution;
			}

			if (value < thresholds[0]) return AcuStatus.Alert;
			if (value > thresholds[3]) return AcuStatus.Alert;
			if (value > thresholds[2]) return AcuStatus.Caution;
			if (value < thresholds[1]) return AcuStatus.Caution;

			return AcuStatus.Nominal;
		}

		#endregion

		#region UI

		public void RequestImmediateUpdate()
		{
			if (IsPowered)
			{
				PeriodicUpdate();
			}
		}

		public void SetOperatingMode(AcuMode mode)
		{
			if (IsWriteable == false) return;

			DesiredMode = mode;
			foreach (var device in ConnectedDevices)
			{
				device.SetOperatingMode(mode);
			}

			OnStateChanged?.Invoke();
		}

		public void ResetThresholds()
		{
			if (IsWriteable == false) return;

			Thresholds = initialAcuThresholds.Clone();
		}

		#endregion

		#region Interaction

		public bool IsLocked { get; set; } = true;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return Validations.HasItemTrait(interaction, CommonTraits.Instance.Id);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (isEmagged) return;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag) && interaction.HandObject.TryGetComponent<Emag>(out var emag) && emag.EmagHasCharges())
			{
				IsLocked = false;
				isEmagged = true;

				emag.UseCharge(interaction);

				Chat.AddActionMsgToChat(interaction.Performer,
					$"The air controller unit sparks as you wave the emag across it.",
					$"You hear sparking from somewhere nearby...");

				return;
			}

			if (restricted.HasClearance(interaction.HandObject))
			{
				IsLocked = !IsLocked;

				Chat.AddActionMsgToChat(interaction.Performer,
					$"You {(IsLocked ? "lock" : "unlock")} the air controller unit.",
					$"{interaction.PerformerPlayerScript.visibleName} {(IsLocked ? "locks" : "unlocks")} the air controller unit.");

				OnStateChanged?.Invoke();
			}
		}

		public string Examine(Vector3 worldPos = default)
		{
			var situation =
				OverallStatus switch
				{
					AcuStatus.Nominal => "a nominal atmosphere",
					AcuStatus.Caution => "to take caution",
					AcuStatus.Alert => "a hazardous environment",
					_ => "nothing"
				};

			if (IsPowered)
			{
				return $"The display is indicating {situation}, and the controls are {(IsLocked ? "locked" : "unlocked")}.";
			}

			return $"The unit appears to be unpowered, and the controls are {(IsLocked ? "locked" : "unlocked")}.";
		}

		#endregion

		#region IAPCPowerable

		public bool IsPowered { get; private set; }

		public void StateUpdate(PowerState state)
		{
			switch (state)
			{
				case PowerState.On:
				case PowerState.LowVoltage:
				case PowerState.OverVoltage:
					IsPowered = true;
					// StateUpdate() can be invoked before OnEnable() or before the matrix is ready.
					if (IsReady)
					{
						spriteHandler.ChangeSprite((int) AcuStatus.Nominal);
						UpdateManager.Add(PeriodicUpdate, 3);
						PeriodicUpdate();
					}
					break;
				case PowerState.Off:
					OverallStatus = AcuStatus.Off;
					IsPowered = false;
					UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
					spriteHandler.ChangeSprite((int) AcuStatus.Off);
					break;
			}

			OnStateChanged?.Invoke();
		}

		public void PowerNetworkUpdate(float voltage) { }

		#endregion

		#region Multitool

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.Acu;
		bool IMultitoolMasterable.MultiMaster => false;
		int IMultitoolMasterable.MaxDistance => 30;

		public void AddSlave(IAcuControllable device)
		{
			ConnectedDevices.Add(device);
			device.SetOperatingMode(DesiredMode);

			OnStateChanged?.Invoke();
		}

		public void RemoveSlave(IAcuControllable device)
		{
			ConnectedDevices.Remove(device);

			OnStateChanged?.Invoke();
		}

		#endregion
	}
}
