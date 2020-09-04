using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pipes
{
	public class Pump : MonoPipe
	{
		public SpriteHandler spriteHandlerOverlay = null;

		private float MaxPressure = 4500f;
		private float TransferMoles = 500f;



		public bool IsOn = false;

		public override void Start()
		{
			pipeData.PipeAction = new MonoActions();
			base.Start();
			spriteHandlerOverlay.PushClear();
		}

		public override void Interaction(HandApply interaction)
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

			var PressureDensity = pipeData.mixAndVolume.Density();
			if (PressureDensity.x > MaxPressure && PressureDensity.y > MaxPressure)
			{
				return;
			}

			var tomove = new Vector2(Mathf.Abs((PressureDensity.x / MaxPressure) - 1),
				Mathf.Abs((PressureDensity.y / MaxPressure) - 1));

			Vector2 AvailableReagents = new Vector2(0f, 0f);
			foreach (var Pipe in pipeData.ConnectedPipes)
			{
				if (pipeData.Outputs.Contains(Pipe) == false && CanEqualiseWithThis(Pipe))
				{
					var Data = PipeFunctions.PipeOrNet(Pipe);
					AvailableReagents += Data.Total;
				}
			}

			Vector2 TotalRemove = Vector2.zero;
			TotalRemove.x = (TransferMoles) > AvailableReagents.x ? AvailableReagents.x : TransferMoles;
			TotalRemove.y = (TransferMoles) > AvailableReagents.y ? AvailableReagents.y : TransferMoles;

			TotalRemove.x = tomove.x > 1 ? 0 : TotalRemove.x;
			TotalRemove.y = tomove.y > 1 ? 0 : TotalRemove.y;


			foreach (var Pipe in pipeData.ConnectedPipes)
			{
				if (pipeData.Outputs.Contains(Pipe) == false && CanEqualiseWithThis(Pipe))
				{
					//TransferTo
					var Data = PipeFunctions.PipeOrNet(Pipe);
					Data.TransferTo(pipeData.mixAndVolume,
						(Data.Total / AvailableReagents) * TotalRemove);
				}
			}

			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);
		}

		public bool CanEqualiseWithThis(PipeData Pipe)
		{
			if (Pipe.NetCompatible == false)
			{
				return PipeFunctions.CanEqualiseWith(this.pipeData, Pipe);
			}

			return true;
		}
	}
}
