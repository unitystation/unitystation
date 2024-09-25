using System;
using System.Collections.Generic;
using Messages.Server;
using UnityEngine;
using Systems.Interaction;
using Systems.Pipes;
using UI.Systems.Tooltips.HoverTooltips;

namespace Objects.Atmospherics
{
	public class TemperatureGate: MonoPipe, IHoverTooltip, IExaminable
	{
		public SpriteHandler spriteHandlerOverlay = null;

		[NonSerialized] public float MaxTemperature = 4500f;
		[NonSerialized] public float MinTemperature = 2.7f;
		[NonSerialized] public float TargetTemperature = 273.15f;
		[NonSerialized] public float ThresholdPressure = 10f;
		
		public bool IsOn = false;
		private bool isInverted;

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
				TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.TemperatureGate, TabAction.Open);
			}
			else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				Chat.AddExamineMsg(interaction.Performer, isInverted ? "You set the Temperature Gate sensors to the default settings." : "You invert the Temperature Gate sensors.");
				isInverted = !isInverted;
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
				TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.TemperatureGate, TabAction.Open);
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
			
			float inputTemp = inputPipe.GetMixAndVolume.Temperature;
			
			if (isInverted ? inputTemp < TargetTemperature : inputTemp > TargetTemperature) return;

			Vector2 inputDensity = inputPipe.GetMixAndVolume.Density();
			Vector2 pressureDensity = pipeData.mixAndVolume.Density();
			if (inputDensity.x - pressureDensity.x > ThresholdPressure || inputDensity.y - pressureDensity.y > ThresholdPressure)
			{
				pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.ConnectedPipes);
			}
		}


		public string Examine(Vector3 worldPos = default)
		{
			return $"The gate has been set to let gas flow when the input gas temp is {(isInverted ? "higher" : "lower")} than set threshold.";
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
				new() { Color = Color.green, Text = $"Left Click with screwdriver: {(isInverted ? "Reset" : "Invert" )} temperature sensor." },
				new() { Color = Color.green, Text = "Alt Click: Open GUI." }
			};
			return list;
		}
	}
}