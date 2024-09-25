using System.Collections.Generic;
using Messages.Server;
using UnityEngine;
using Systems.Interaction;
using Systems.Pipes;
using UI.Systems.Tooltips.HoverTooltips;


namespace Objects.Atmospherics
{
	public class Pump : MonoPipe, IHoverTooltip
	{
		public SpriteHandler spriteHandlerOverlay = null;

		public float MaxPressure = 4500f;
		public float TargetPressure = 4500f;
		public float TransferMoles = 10000f;

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
				TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.Pump, TabAction.Open);
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
				TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.Pump, TabAction.Open);
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

			var pressureDensity = pipeData.mixAndVolume.Density();
			if (pressureDensity.x > TargetPressure && pressureDensity.y > TargetPressure)
			{
				return;
			}

			var toMove = new Vector2(Mathf.Abs((pressureDensity.x / TargetPressure) - 1),
				Mathf.Abs((pressureDensity.y / TargetPressure) - 1));

			Vector2 availableReagents = new Vector2(0f, 0f);
			foreach (var pipe in pipeData.ConnectedPipes)
			{
				if (pipeData.Outputs.Contains(pipe) == false && PipeFunctions.CanEqualiseWithThis(pipeData, pipe))
				{
					var data = PipeFunctions.PipeOrNet(pipe);
					availableReagents += data.Total;
				}
			}

			Vector2 totalRemove = Vector2.zero;
			totalRemove.x = (TransferMoles) > availableReagents.x ? availableReagents.x : TransferMoles;
			totalRemove.y = (TransferMoles) > availableReagents.y ? availableReagents.y : TransferMoles;

			totalRemove.x = toMove.x > 1 ? 0 : totalRemove.x;
			totalRemove.y = toMove.y > 1 ? 0 : totalRemove.y;


			foreach (var pipe in pipeData.ConnectedPipes)
			{
				if (pipeData.Outputs.Contains(pipe) == false && PipeFunctions.CanEqualiseWithThis(pipeData, pipe))
				{
					//TransferTo
					var data = PipeFunctions.PipeOrNet(pipe);
					data.TransferTo(pipeData.mixAndVolume,
						(data.Total / availableReagents) * totalRemove);
				}
			}

			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);
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
