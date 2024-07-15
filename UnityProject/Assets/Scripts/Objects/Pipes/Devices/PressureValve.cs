using System;
using System.Collections.Generic;
using Messages.Server;
using UnityEngine;
using Systems.Interaction;
using Systems.Pipes;
using UI.Systems.Tooltips.HoverTooltips;

namespace Objects.Atmospherics
{
	public class PressureValve: MonoPipe, IHoverTooltip
	{
		public SpriteHandler spriteHandlerOverlay = null;

		[NonSerialized] public float MaxPressure = 4500f;
		[NonSerialized] public float ThresholdPressure = 10f;
		[NonSerialized] public float TargetPressure = AtmosConstants.ONE_ATMOSPHERE;

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
			}
		}

		public string HoverTip()
		{
			return null;
		}

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			var list = new List<TextColor>
			{
				new() { Color = Color.green, Text = "Left Click: Toggle Power." },
				new() { Color = Color.green, Text = "Alt Click: Open GUI." }
			};
			return list;
		}
	}
}