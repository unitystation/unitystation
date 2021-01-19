using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pipes
{
	public class ReactorPipe : MonoPipe
	{
		public Chemistry.Reagent Water;
		public List<ReactorPipe> ConnectedCores = new List<ReactorPipe>(); //needs To check properly
		public override void Start()
		{
			pipeData.PipeAction = new ReservoirAction();
			pipeData.GetMixAndVolume.GetReagentMix().Add(Water, 100);
			base.Start();
		}

		public override void TickUpdate()
		{

		}
	}
}
