using UnityEngine;
using Systems.Atmospherics;
using Systems.Electricity;
using Systems.Interaction;


namespace Objects.Atmospherics
{
	/// <summary>
	/// Moves gases from the pipenet to the injector's tile without regard for a target pressure.
	/// It is regulated only by flow rate, similar to /tg/ volumetric pumps.
	/// </summary>
	public class AirInjector : MonoPipe, IAPCPowerable, IExaminable
	{
		private enum Mode
		{
			Injecting = 0,
			Extracting = 1,
		}

		/// <summary>Maps to <c>SpriteHandler</c>'s sprite SO catalogue.</summary>
		private enum Sprite
		{
			Off = 0,
			On = 1,
			Injecting = 2,
		}

		private readonly float molesRate = 100;

		[SerializeField]
		[Tooltip("Set the injector's on/off switch (also requires power for operation)")]
		private bool isTurnedOn = false;

		[SerializeField]
		[Tooltip("The direction of gas transfer: injecting into air vs. extracting into pipe")]
		private Mode operatingMode = Mode.Injecting;

		private bool isOperating = false;
		private float Effectiveness => voltageMultiplier;

		private MetaDataNode metaNode;
		private MetaDataLayer metaDataLayer;

		private GasMix pipeMix;
		private GasMix sourceMix;
		private GasMix targetMix;

		public override void OnSpawnServer(SpawnInfo info)
		{
			metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
			metaNode = metaDataLayer.Get(registerTile.LocalPositionServer, false);
			pipeMix = pipeData.GetMixAndVolume.GetGasMix();

			UpdateState();
			base.OnSpawnServer(info);
		}

		public override void TickUpdate()
		{
			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);

			if (isOperating)
			{
				if (operatingMode == Mode.Extracting && pipeData.mixAndVolume.Density().y > MaxInternalPressure)
				{
					return;
				}

				GasMix.TransferGas(targetMix, sourceMix, molesRate * Effectiveness);
				metaDataLayer.UpdateSystemsAt(registerTile.LocalPositionServer, SystemType.AtmosSystem);
			}
		}

		public override void HandApplyInteraction(HandApply interaction)
		{
			if (interaction.HandSlot.IsOccupied) return;

			if (interaction.IsAltClick)
			{
				operatingMode = operatingMode.Next();
			}
			else
			{
				isTurnedOn = !isTurnedOn;
			}

			UpdateState();
		}

		//Ai interaction
		public override void AiInteraction(AiActivate interaction)
		{
			if (interaction.ClickType == AiActivate.ClickTypes.AltClick)
			{
				operatingMode = operatingMode.Next();
			}
			else
			{
				isTurnedOn = !isTurnedOn;
			}

			UpdateState();
		}

		private void UpdateState()
		{
			isOperating = powerState != PowerState.Off && isTurnedOn;

			if (CustomNetworkManager.IsServer)
			{
				switch (operatingMode)
				{
					default:
					case Mode.Injecting:
						sourceMix = pipeMix;
						targetMix = metaNode.GasMix;
						break;
					case Mode.Extracting:
						sourceMix = metaNode.GasMix;
						targetMix = pipeMix;
						break;
				}
			}


			Sprite sprite = operatingMode == Mode.Injecting ? Sprite.Injecting : Sprite.On;
			sprite = isOperating ? sprite : Sprite.Off;
			spritehandler.ChangeSprite((int)sprite);
		}

		#region IAPCPowerable

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
			UpdateState();
		}

		#endregion

		public string Examine(Vector3 worldPos = default)
		{
			string helpMessage = "You see a small selector switch, which appears to control the device's power and mode.";

			string stateMessage;
			if (isTurnedOn == false)
			{
				stateMessage = "It is turned off.";
			}
			else if (isTurnedOn && isOperating == false)
			{
				stateMessage = "It is turned on but the device seems to be unpowered.";
			}
			else
			{
				stateMessage = operatingMode == Mode.Injecting
						? "It is currently set to inject gas into the immediate area."
						: "It is currently set to extract gas from the immediate area.";
			}

			return $"{helpMessage}\n{stateMessage}";
		}
	}
}
