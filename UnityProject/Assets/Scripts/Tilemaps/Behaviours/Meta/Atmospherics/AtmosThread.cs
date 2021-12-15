using System;
using Systems.Atmospherics;

public class AtmosThread : ThreadedBehaviour
{
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