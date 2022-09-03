using UnityEngine;
using Systems.Interaction;
using Systems.Pipes;


namespace Objects.Atmospherics
{
	public class Pump : MonoPipe
	{
		public SpriteHandler spriteHandlerOverlay = null;

		private float MaxPressure = 4500f;
		private float TransferMoles = 500f;

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
			ToggleState();
		}

		//Ai interaction
		public override void AiInteraction(AiActivate interaction)
		{
			ToggleState();
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
			if (pressureDensity.x > MaxPressure && pressureDensity.y > MaxPressure)
			{
				return;
			}

			var toMove = new Vector2(Mathf.Abs((pressureDensity.x / MaxPressure) - 1),
				Mathf.Abs((pressureDensity.y / MaxPressure) - 1));

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
	}
}
