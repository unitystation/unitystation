using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using UnityEngine;
using Core.Editor.Attributes;
using AddressableReferences;
using ScriptableObjects.Atmospherics;
using Systems.Atmospherics;
using Systems.Electricity;


namespace Objects.Atmospherics
{
	/// <summary>
	/// <para>Scrubbers are, in normal operation, responsible for removing unwanted gases
	/// from the local atmosphere for collection by the atmospherics waste loop.</para>
	/// <remarks>Typically controlled by an <see cref="AirController"/>.</remarks>
	/// </summary>
	public class Scrubber : MonoPipe, IServerSpawn, IAPCPowerable, IAcuControllable, IExaminable
	{
		public enum Mode
		{
			Scrubbing = 0,
			Siphoning = 1,
		}

		/// <summary>Maps to <c>SpriteHandler</c>'s sprite SO catalogue.</summary>
		private enum Sprite
		{
			Off = 0,
			Scrubbing = 1,
			Siphoning = 2,
			WideRange = 3,
			Welded = 4,
		}

		[SerializeField ]
		[Tooltip("Sound to play when the welding task is complete.")]
		private AddressableAudioSource weldFinishSfx = default;

		[SerializeField, SceneModeOnly]
		[Tooltip("If enabled, allows the scrubber to operate without being connected to a pipenet (magic). Usage is discouraged.")]
		private bool selfSufficient = false;
		public bool SelfSufficient => selfSufficient;

		private List<GasSO> defaultFilteredGases;
		private List<GasSO> defaultContaminatedGases;

		private MetaDataNode metaNode;
		private MetaDataLayer metaDataLayer;

		#region Lifecycle

		public override void Awake()
		{
			base.Awake();

			if (CustomNetworkManager.IsServer)
			{
				defaultFilteredGases = new List<GasSO>() { Gas.CarbonDioxide };
				defaultContaminatedGases = new List<GasSO>(Gas.Gases.Values);
				defaultContaminatedGases.Remove(Gas.Oxygen);
				defaultContaminatedGases.Remove(Gas.Nitrogen);

				FilteredGases = new ObservableCollection<GasSO>(defaultFilteredGases);
				FilteredGases.CollectionChanged += OnFilteredGasesChanged;
				scrubbingGasMoles = new float[Gas.Gases.Count];

				powerable = GetComponent<APCPoweredDevice>();
			}
		}

		public override void OnSpawnServer(SpawnInfo info)
		{
			metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
			metaNode = metaDataLayer.Get(registerTile.LocalPositionServer);
			pipeMix = selfSufficient ? GasMix.NewGasMix(GasMixes.BaseEmptyMix) : pipeData.GetMixAndVolume.GetGasMix();

			if (TryGetComponent<AcuDevice>(out var device) && device.Controller != null)
			{
				SetOperatingMode(device.Controller.DesiredMode);
			}

			base.OnSpawnServer(info);
		}

		#endregion

		public override void TickUpdate()
		{
			base.TickUpdate();
			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);

			if (IsOperating == false || isWelded) return;
			if (CanTransfer() == false) return;

			switch (OperatingMode)
			{
				case Mode.Scrubbing:
					ModeScrub();
					break;
				case Mode.Siphoning:
					ModeSiphon();
					break;
			}

			metaDataLayer.UpdateSystemsAt(registerTile.LocalPositionServer, SystemType.AtmosSystem);

			if (selfSufficient)
			{
				pipeMix.CopyFrom(GasMixes.BaseEmptyMix); // We don't need to do this, just void the gas
			}
		}

		#region Operation

		public bool IsTurnedOn { get; private set; } = false;
		public bool IsOperating { get; private set; } = false;
		public Mode OperatingMode { get; private set; } = Mode.Scrubbing;

		public bool IsExpandedRange { get; private set; } = false;


		public float SiphonMultiplier = 2f;

		public float ExpandedRangeNumber = 0.5f;

		public float NormalRangeNumber = 0.25f;

		//public bool IsExpandedRange
		/// <summary>Updates the scrubber's power consumption when the collection is modified.</summary>
		public ObservableCollection<GasSO> FilteredGases;

		private float Effectiveness => voltageMultiplier;
		public float nominalMolesTransferCap = 50;
		private float[] scrubbingGasMoles;

		private GasMix pipeMix;

		private bool CanTransfer()
		{
			// No external gas to take
			if (metaNode.GasMix.Pressure.Approx(0)) return false;
			if (selfSufficient == false)
			{
				// No room in internal pipe to push to
				if (pipeData.mixAndVolume.Density().y > MaxInternalPressure) return false;
			}

			return true;
		}

		private void ModeScrub()
		{
			// Scrub out a portion of each specified gas.
			// If all these gases exceed transfer amount, reduce each gas scrub mole count proportionally.

			var percentageRemoved = (IsExpandedRange ?  ExpandedRangeNumber : NormalRangeNumber) * Effectiveness;

			float scrubbableMolesAvailable = 0;

			lock (metaNode.GasMix.GasesArray) //no Double lock
			{
				foreach (GasValues gas in metaNode.GasMix.GasesArray) //doesn't appear to modify list while iterating
				{
					if (FilteredGases.Contains(gas.GasSO))
					{
						var molesRemoved = gas.Moles * percentageRemoved;
						scrubbingGasMoles[gas.GasSO] = molesRemoved;
						scrubbableMolesAvailable += molesRemoved;
					}
				}
			}

			if (scrubbableMolesAvailable.Approx(0)) return; // No viable gases found

			float molesToTransfer = scrubbableMolesAvailable.Clamp(0, nominalMolesTransferCap * Effectiveness);
			float ratio = molesToTransfer / scrubbableMolesAvailable;
			ratio = ratio.Clamp(0, 1);

			// actual scrubbing
			for (int i = 0; i < Gas.Gases.Count; i++)
			{
				GasSO gas = Gas.Gases[i];
				float transferAmount = scrubbingGasMoles[i] * ratio;
				if (transferAmount.Approx(0)) continue;

				metaNode.GasMix.RemoveGas(gas, transferAmount);
				if (selfSufficient == false)
				{
					pipeMix.AddGas(gas, transferAmount);
				}
			}

			Array.Clear(scrubbingGasMoles, 0, scrubbingGasMoles.Length);
		}

		private void ModeSiphon()
		{
			float moles = metaNode.GasMix.Moles * (IsExpandedRange ? ExpandedRangeNumber : NormalRangeNumber ) * Effectiveness * SiphonMultiplier; // siphon a portion
			moles = moles.Clamp(0, nominalMolesTransferCap);

			if (moles.Approx(0)) return;

			GasMix.TransferGas(pipeMix, metaNode.GasMix, moles);
		}

		#endregion

		#region Interaction

		public override bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side, PlayerTypes.Normal | PlayerTypes.Alien) == false) return false;
			if (interaction.TargetObject != gameObject) return false;

			if (Validations.HasUsedActiveWelder(interaction)) return true;

			if (interaction.PerformerPlayerScript.CanVentCrawl && interaction.HandObject == null) return true;

			return false;
		}

		private bool isWelded = false;

		public override void HandApplyInteraction(HandApply interaction)
		{
			if (Validations.HasUsedActiveWelder(interaction))
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 3,
					$"You begin {(isWelded ? "unwelding" : "welding over")} the scrubber...",
					$"{interaction.PerformerPlayerScript.visibleName} begins {(isWelded ? "unwelding" : "welding")} the scrubber...",
					string.Empty,
					$"{interaction.PerformerPlayerScript.visibleName} {(isWelded ? "unwelds" : "welds")} the scrubber!",
					() =>
					{
						isWelded = !isWelded;
						UpdateSprite();
						SoundManager.PlayNetworkedAtPos(weldFinishSfx, registerTile.WorldPositionServer, sourceObj: gameObject);
					});

				return;
			}

			//Do vent crawl
			DoVentCrawl(interaction, pipeMix);
		}

		public string Examine(Vector3 worldPos = default)
		{
			string stateDescription =
					powerState == PowerState.Off ? "unpowered."
					: IsTurnedOn == false ? "turned off."
					: OperatingMode == Mode.Siphoning && IsExpandedRange ? "manically siphoning!"
					: $"{OperatingMode.ToString().ToLower()}.";

			if (isWelded)
			{
				return IsOperating
						? $"It is tenaciously welded shut, but you can still hear the induction motor {stateDescription}"
						: $"It is teanciously welded shut and is dead silent. It seems to be {stateDescription}";
			}

			return $"It seems to be {stateDescription}";
		}

		#endregion

		private void UpdateSprite()
		{
			Sprite sprite = Sprite.Off;

			if (IsOperating)
			{
				switch (OperatingMode)
				{
					case Mode.Scrubbing:
						sprite = IsExpandedRange ? Sprite.WideRange : Sprite.Scrubbing;
						break;
					case Mode.Siphoning:
						sprite = Sprite.Siphoning;
						break;
				}
			}

			if (isWelded)
			{
				sprite = Sprite.Welded;
			}

			if ((int)sprite == spritehandler.CataloguePage) return;
			spritehandler.ChangeSprite((int)sprite);
		}

		#region IAPCPowerable

		private readonly float siphoningPowerConsumption = 60; // Watts
		private readonly float scrubbingPowerConsumption = 10; // Per enabled filter

		APCPoweredDevice powerable;

		private PowerState powerState = PowerState.Off;
		private float voltageMultiplier = 1;

		public void PowerNetworkUpdate(float voltage)
		{
			voltageMultiplier = voltage / 240;
		}

		public void StateUpdate(PowerState state)
		{
			if (state == powerState) return;
			powerState = state;

			IsOperating = state == PowerState.Off ? false : IsTurnedOn;

			UpdateSprite();
		}

		private void UpdatePowerUsage()
		{
			var basePower = siphoningPowerConsumption;
			if (OperatingMode == Mode.Scrubbing)
			{
				basePower = scrubbingPowerConsumption * FilteredGases.Count;
			}

			if (IsExpandedRange)
			{
				basePower *= 8;
			}

			powerable.Wattusage = basePower;
		}

		#endregion

		#region IAcuControllable

		private static readonly List<AcuMode> acuSiphonModes = new List<AcuMode>()
		{
			AcuMode.Cycle, AcuMode.Draught, AcuMode.Siphon, AcuMode.PanicSiphon
		};

		private AcuSample atmosphericSample = new AcuSample();
		AcuSample IAcuControllable.AtmosphericSample => atmosphericSample.FromGasMix(metaNode.GasMix);

		public void SetOperatingMode(AcuMode mode)
		{
			// Override all custom changes if the operating mode changes.

			OperatingMode = acuSiphonModes.Contains(mode) ? Mode.Siphoning : Mode.Scrubbing;
			// Create a list copy as this list can be further modified via device settings.
			FilteredGases = new ObservableCollection<GasSO>(mode == AcuMode.Contaminated ? defaultContaminatedGases : defaultFilteredGases);
			FilteredGases.CollectionChanged += OnFilteredGasesChanged;
			IsExpandedRange = mode == AcuMode.Contaminated || mode == AcuMode.Cycle || mode == AcuMode.PanicSiphon;

			SetTurnedOn(mode != AcuMode.Off);
		}

		#endregion

		#region ACU-GUI

		public void SetTurnedOn(bool isTurnedOn)
		{
			IsTurnedOn = isTurnedOn;

			if (powerState != PowerState.Off)
			{
				IsOperating = IsTurnedOn;
			}

			UpdateSprite();
		}

		public void SetOperatingMode(Mode mode)
		{
			OperatingMode = mode;
			UpdatePowerUsage();
			UpdateSprite();
		}

		public void SetExpandedRange(bool isExpanded)
		{
			IsExpandedRange = isExpanded;
			UpdatePowerUsage();
			UpdateSprite();
		}

		private void OnFilteredGasesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			UpdatePowerUsage();
		}

		#endregion
	}
}
