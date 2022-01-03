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
			try
			{
				atmosManager.simulation.Run();

				if (atmosManager.StopPipes == false)
				{
					atmosManager.pipeList.Iterate(atmosManager.processPipeDelegator);
				}

				foreach (var reactionManger in atmosManager.reactionManagerList)
				{
					reactionManger.DoTick();
				}
			}
			catch (Exception e)
			{
				Logger.LogError($"Atmos Thread Error! {e.GetStack()}", Category.Atmos);
			}

			atmosManager.sampler.End();
		}
	}
}