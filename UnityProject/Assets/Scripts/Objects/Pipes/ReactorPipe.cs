using System.Collections.Generic;
using UnityEngine;
using Chemistry;


namespace Objects.Atmospherics
{
	public class ReactorPipe : MonoPipe
	{
		[SerializeField] private float reservoirVolume = 10;

		public override void OnSpawnServer(SpawnInfo info)
		{
			pipeData.PipeAction = new ReservoirAction();
			pipeData.mixAndVolume.SetVolume(reservoirVolume);
			base.OnSpawnServer(info);
		}

		public override void TickUpdate() { }
	}
}
