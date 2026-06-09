using UnityEngine;
using US13.Core.Lifecycle;
using US13.Systems.Fluids;

namespace US13.Objects.Pipes
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
