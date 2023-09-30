using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.Editor.Attributes;
using Systems.Atmospherics;
using Systems.Electricity;


namespace Objects.Atmospherics
{
	/// <summary>
	/// <para>Vents, in normal operation, are responsible for releasing air
	/// from the atmospherics distribution loop into the local atmosphere.</para>
	/// <remarks>Typically controlled by an <see cref="AirController"/>.</remarks>
	/// </summary>
	public class AirVent : MonoPipe, IServerSpawn, IAPCPowerable, IAcuControllable, IExaminable
	{
		public enum Mode
		{
			Out = 0,
			In = 1,
		}

		// We have duplicates here because SpriteHandler.AnimateOnce()
		// automatically goes to the next SO when animation is complete
		/// <summary>Maps to <c>SpriteHandler</c>'s sprite SO catalogue.</summary>
		private enum Sprite
		{
			Off = 0,
			OutStarting = 1,
			Out = 2,
			OutStopping = 3,
			OutOff = 4,
			InStarting = 5,
			In = 6,
			InStopping = 7,
			InOff = 8,
			Welded = 9,
		}

		[SerializeField ]
		[Tooltip("Sound to play when the welding task is complete.")]
		private AddressableReferences.AddressableAudioSource weldFinishSfx = default;

		[SerializeField, SceneModeOnly]
		[Tooltip("If enabled, allows the vent to operate without being connected to a pipenet (magic). Usage is discouraged.")]
		private bool selfSufficient = false;
		public bool SelfSufficient => selfSufficient;

		// Pressure regulation
		public bool InternalEnabled { get; set; } = false;
		public bool ExternalEnabled { get; set; } = true;
		public float InternalTarget { get; set; } = 0;
		public float ExternalTarget { get; set; } = AtmosConstants.ONE_ATMOSPHERE;

		private MetaDataNode metaNode;
		private MetaDataLayer metaDataLayer;

		private GasMix pipeMix;

		public override void OnSpawnServer(SpawnInfo info)
		{
			metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
			metaNode = metaDataLayer.Get(registerTile.LocalPositionServer);
			pipeMix = selfSufficient ? GasMix.NewGasMix(GasMixes.BaseAirMix) : pipeData.GetMixAndVolume.GetGasMix();

			if (TryGetComponent<AcuDevice>(out var device) && device.Controller != null)
			{
				SetOperatingMode(device.Controller.DesiredMode);
			}

			base.OnSpawnServer(info);
		}

		public override void TickUpdate()
		{
			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);

			if (IsOperating == false || isWelded) return;

			Operate();

			if (selfSufficient)
			{
				pipeMix.CopyFrom(GasMixes.BaseAirMix);
			}
		}

		#region Operation

		public bool IsTurnedOn { get; private set; } = false;
		public bool IsOperating { get; private set; } = false;
		public Mode OperatingMode { get; private set; } = Mode.Out;

		/// <summary>
		/// A multiplier on the vent's maximum transferrable moles cap.
		/// <para>Final transfer rate is still limited by the pressure difference and available moles.</para>
		/// </summary>
		private float Effectiveness => voltageMultiplier * (isTransitioning ? 0.2f : 1);
		public float nominalMolesTransferCap = 50;

		private void Operate()
		{
			GasMix sourceGasMix = pipeMix;
			GasMix targetGasMix = metaNode.GasMix;
			if (OperatingMode == Mode.In)
			{
				sourceGasMix = metaNode.GasMix;
				targetGasMix = pipeMix;
			}

			GasMix.TransferGas(targetGasMix, sourceGasMix, GetTransferableMoles(sourceGasMix));
			metaDataLayer.UpdateSystemsAt(registerTile.LocalPositionServer, SystemType.AtmosSystem);
		}

		private float GetTransferableMoles(GasMix sourceMix)
		{
			if (selfSufficient == false && OperatingMode == Mode.In)
			{
				// No room in internal pipe to push to
				if (pipeData.mixAndVolume.Density().y > MaxInternalPressure) return 0;
			}

			// The maximum moles that can be transferred is the available moles multiplied by the
			// current pressure and the target pressure ratio, if the relevant regulator is enabled.
			// https://www.desmos.com/calculator/cbfkuo1ik2
			// (https://i.imgur.com/YgD2AB2.png)

			float maxTransfer = nominalMolesTransferCap * Effectiveness;

			// The allowed direction of mole transfer changes depending on the operating mode
			int direction = OperatingMode == Mode.Out ? 1 : -1;

			// Evaluate Internal pressure regulator - want to match pressure in the pipe to the regulator value
			float internalMolLimit = maxTransfer;
			if (InternalEnabled && pipeMix.Pressure.Approx(0) == false)
			{
				internalMolLimit = sourceMix.Moles - (sourceMix.Moles * (InternalTarget / pipeMix.Pressure));
				internalMolLimit *= direction;
			}

			// Evaluate External pressure regulator - want to match pressure in the tile to the regulator value
			float externalMolLimit = maxTransfer;
			if (ExternalEnabled && metaNode.GasMix.Pressure.Approx(0) == false)
			{
				externalMolLimit = sourceMix.Moles - (sourceMix.Moles * (ExternalTarget / metaNode.GasMix.Pressure));
				externalMolLimit *= -direction;
			}

			return Mathf.Min(internalMolLimit, externalMolLimit).Clamp(0, maxTransfer);
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
					$"You begin {(isWelded ? "unwelding" : "welding over")} the vent...",
					$"{interaction.PerformerPlayerScript.visibleName} begins {(isWelded ? "unwelding" : "welding")} the vent...",
					string.Empty,
					$"{interaction.PerformerPlayerScript.visibleName} {(isWelded ? "unwelds" : "welds")} the vent!",
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
					powerState == PowerState.Off ? "unpowered"
					: IsTurnedOn == false ? "turned off"
					: OperatingMode == Mode.In ? "drawing air in" : "pushing air out";

			if (isWelded)
			{
				return IsOperating
						? $"It is tenaciously welded shut, but you can still hear the impeller trying to move air."
						: $"It is teanciously welded shut and is dead silent. It seems to be {stateDescription}.";
			}

			return $"It seems to be {stateDescription}.";
		}

		#endregion

		#region Sprite Animation

		// Alter the vent's effectiveness if the animation indicates it is spooling up or down.
		private bool isTransitioning = false;
		private Coroutine animator;

		private void UpdateSprite()
		{
			Sprite desiredFinalSprite = Sprite.Off;

			if (IsOperating)
			{
				switch (OperatingMode)
				{
					case Mode.Out:
						desiredFinalSprite = Sprite.Out;
						break;
					case Mode.In:
						desiredFinalSprite = Sprite.In;
						break;
				}
			}

			if (isWelded)
			{
				desiredFinalSprite = Sprite.Welded;
			}

			this.RestartCoroutine(AnimateSprite(desiredFinalSprite), ref animator);

		}

		private IEnumerator AnimateSprite(Sprite desiredFinalSprite)
		{
			Sprite currentSprite = (Sprite)spritehandler.CataloguePage;

			if (desiredFinalSprite == currentSprite) yield break;

			var offSprites = new Sprite[] { Sprite.Off, Sprite.InOff, Sprite.OutOff };
			var transitionSprites = new Sprite[] { Sprite.OutStarting, Sprite.OutStopping, Sprite.InStarting, Sprite.InStopping };
			var operatingSprites = new Sprite[] { Sprite.Out, Sprite.In };

			// Run the animatable transitions.
			if (desiredFinalSprite == Sprite.Off && operatingSprites.Contains(currentSprite))
			{
				// Bring impeller to a standstill from operation.
				ToOffFrom(currentSprite);
			}
			else if (operatingSprites.Contains(desiredFinalSprite) &&
					(offSprites.Contains(currentSprite) || transitionSprites.Contains(currentSprite)))
			{
				// Bring impeller to operation from a standstill.
				FromOffTo(desiredFinalSprite);
			}
			else if (operatingSprites.Contains(desiredFinalSprite) && operatingSprites.Contains(currentSprite))
			{
				// Bring impeller to a standstill before reversing direction.
				yield return FromTo(currentSprite, desiredFinalSprite);
			}
			else
			{
				// No transitions; just set instantly.
				spritehandler.ChangeSprite((int)desiredFinalSprite);
			}
		}

		private void FromOffTo(Sprite outOrIn)
		{
			Sprite startupSprite = outOrIn == Sprite.Out ? Sprite.OutStarting : Sprite.InStarting;
			spritehandler.AnimateOnce((int)startupSprite);
		}

		private void ToOffFrom(Sprite outOrIn)
		{
			Sprite shutdownSprite = outOrIn == Sprite.Out ? Sprite.OutStopping : Sprite.InStopping;
			spritehandler.AnimateOnce((int)shutdownSprite);
		}

		private IEnumerator FromTo(Sprite currentDirection, Sprite desiredSprite)
		{
			isTransitioning = true;
			ToOffFrom(currentDirection);

			float animationTime = spritehandler.GetCurrentSpriteSO().Variance[0].Frames.Sum(frame => frame.secondDelay);
			yield return WaitFor.Seconds(animationTime);

			FromOffTo(desiredSprite);
			isTransitioning = false;
		}

		#endregion

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

			IsOperating = state == PowerState.Off ? false : IsTurnedOn;

			UpdateSprite();
		}

		#endregion

		#region IAcuControllable

		private readonly AcuSample atmosphericSample = new AcuSample();
		AcuSample IAcuControllable.AtmosphericSample => atmosphericSample.FromGasMix(metaNode.GasMix);

		public void SetOperatingMode(AcuMode mode)
		{
			OperatingMode = Mode.Out;
			InternalEnabled = false;
			ExternalEnabled = true;
			InternalTarget = 0;
			ExternalTarget = AtmosConstants.ONE_ATMOSPHERE;

			if (mode == AcuMode.Refill)
			{
				ExternalTarget *= 3;
			}

			SetTurnedOn(mode == AcuMode.Filtering || mode == AcuMode.Contaminated || mode == AcuMode.Draught || mode == AcuMode.Refill);
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
			UpdateSprite();
		}

		#endregion
	}
}
