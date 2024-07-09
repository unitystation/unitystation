using System;
using Messages.Server;
using UnityEngine;
using Systems.Interaction;
using Systems.Pipes;

namespace Objects.Atmospherics
{
	public class PressureValve: MonoPipe
	{
		public SpriteHandler spriteHandlerOverlay = null;

		[NonSerialized] public float MaxPressure = 4500f;
		[NonSerialized] public float ThresholdPressure = 10f;
		[NonSerialized] public float TargetPressure = 101.325f;

		public bool IsOn = false;

		public override void OnSpawnServer(SpawnInfo info)
		{
			if (IsOn)
			{
				spriteHandlerOverlay.PushTexture();
			}
			else
			{
				spriteHandlerOverlay.PushClear();
			}
			base.OnSpawnServer(info);
		}

		public override void HandApplyInteraction(HandApply interaction)
		{
			if (interaction.IsAltClick)
			{
				TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.PressureValve, TabAction.Open);
			}
			else
			{
				ToggleState();
			}
		}

		//Ai interaction
		public override void AiInteraction(AiActivate interaction)
		{
			if (interaction.ClickType == AiActivate.ClickTypes.AltClick)
			{
				TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.PressureValve, TabAction.Open);
			}
			else
			{
				ToggleState();
			}
		}

		private void ToggleState()
		{
			IsOn = !IsOn;
			if (IsOn)
			{
				spriteHandlerOverlay.PushTexture();
			}
			else
			{
				spriteHandlerOverlay.PushClear();
			}
		}

		public override void TickUpdate()
		{
			if (IsOn == false)
			{
				return;
			}
			
			PipeData inputPipe = pipeData.Connections.GetFlagToDirection(FlagLogic.InputOne)?.Connected;
			if (inputPipe == null) return;
			
			Vector2 inputDensity = inputPipe.GetMixAndVolume.Density();
			if (inputDensity.x < TargetPressure && inputDensity.y < TargetPressure) return;
			
			Vector2 pressureDensity = pipeData.mixAndVolume.Density();
			
			if (inputDensity.x - pressureDensity.x > ThresholdPressure || inputDensity.y - pressureDensity.y > ThresholdPressure)
			{
				pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.ConnectedPipes);
				//spriteHandlerOverlay.AnimateOnce(1);
			}
		}
	}
}