using System;
using Core.Threading;

namespace Systems.Atmospherics
{
	public class AtmosThread : ThreadedBehaviour
	{
		//Can't be in LavaTileInteraction SO as it gets saved and not reset over rounds
		public static bool runLavaFireTick;

		public override void ThreadedWork()
		{
			var atmosManager = AtmosManager.Instance;
			atmosManager.sampler.Begin();

			atmosManager.simulation.Run();

			if (atmosManager.StopPipes == false)
			{
				atmosManager.pipeList.Iterate(atmosManager.processPipeDelegator);
			}

			foreach (var reactionManger in atmosManager.reactionManagerList)
			{
				reactionManger.DoTick();
			}


			atmosManager.sampler.End();
		}
	}
}