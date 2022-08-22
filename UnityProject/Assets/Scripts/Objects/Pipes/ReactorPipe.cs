using System.Collections.Generic;
using UnityEngine;
using Chemistry;


namespace Objects.Atmospherics
{
	public class ReactorPipe : MonoPipe
	{
		public Reagent Water;
		public List<ReactorPipe> ConnectedCores = new List<ReactorPipe>(); //needs To check properly
		public override void OnSpawnServer(SpawnInfo info)
		{
			pipeData.PipeAction = new ReservoirAction();
			pipeData.GetMixAndVolume.GetReagentMix().Add(Water, 100);
			base.OnSpawnServer(info);
		}

		public override void TickUpdate() { }
	}
}
