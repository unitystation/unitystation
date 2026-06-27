using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Lifecycle;
using US13.Player;
using US13.Systems.Fluids;

namespace US13.Objects.Pipes
{
	public class ReactorPipe : MonoPipe
	{
		[SerializeField] private float reservoirVolume = 10;

		public ReagentContainer Container;

		public override void OnSpawnServer(SpawnInfo info)
		{
			pipeData.PipeAction = new ReservoirAction();
			Container.SetIProvideReagentMix(pipeData);
			pipeData.GetMixAndVolume.SetReagentMix(Container.InitialReagentMix);
			pipeData.GetMixAndVolume.SetVolume(Container.MaxCapacity);
			base.OnSpawnServer(info);
		}
	}
}
