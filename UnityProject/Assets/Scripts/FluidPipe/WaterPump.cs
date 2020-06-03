using System.Collections;
using System.Collections.Generic;
using Pipes;
using UnityEngine;

namespace Pipes
{
	//Improvements for the future
	//make it so it is less effective when pressure drop behind it
	public class WaterPump : MonoPipe
	{
		//Power stuff
		public int UnitPerTick = 100;
		public int PowerPercentage = 100;


		public void Start()
		{
			pipeData.PipeAction = new WaterPumpAction();
			base.Start();
		}

		public override void TickUpdate()
		{
			float AvailableReagents = 0;
			foreach (var Pipe in pipeData.ConnectedPipes)
			{
				if (pipeData.Outputs.Contains(Pipe) == false && CanEqualiseWithThis( Pipe))
				{
					var Data = PipeFunctions.PipeOrNet(Pipe);
					AvailableReagents += Data.Mix.Total;
				}
			}

			float TotalRemove = 0;
			if ((UnitPerTick * PowerPercentage) > AvailableReagents)
			{
				TotalRemove = AvailableReagents;
			}
			else
			{
				TotalRemove = (UnitPerTick * PowerPercentage);
			}

			foreach (var Pipe in pipeData.ConnectedPipes)
			{
				if (pipeData.Outputs.Contains(Pipe) == false && CanEqualiseWithThis(Pipe))
				{
					var Data = PipeFunctions.PipeOrNet(Pipe);
					Data.Mix.TransferTo(pipeData.mixAndVolume.Mix,
						(Data.Mix.Total / AvailableReagents) * TotalRemove);
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