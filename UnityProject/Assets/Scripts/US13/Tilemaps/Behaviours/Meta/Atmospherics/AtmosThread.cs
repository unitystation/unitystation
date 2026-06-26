using System;
using Logs;
using US13.Core.Threading;

namespace US13.Tilemaps.Behaviours.Meta.Atmospherics
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
				lock (atmosManager.addingpipeList)
				{
					var count = atmosManager.addingpipeList.Count;
					for (int i = 0; i < count; i++)
					{
						if (atmosManager.pipeList.Contains(atmosManager.addingpipeList[i]) == false)
						{
							atmosManager.pipeList.Add(atmosManager.addingpipeList[i]);
						}
					}
					atmosManager.addingpipeList.Clear();

					count = atmosManager.removeingpipeList.Count;
					for (int i = 0; i < count; i++)
					{
						atmosManager.pipeList.Remove(atmosManager.removeingpipeList[i]);
					}
					atmosManager.removeingpipeList.Clear();
				}

				var countpipeList = atmosManager.pipeList.Count;

				for (int i = 0; i < countpipeList; i++)
				{
					try
					{
						atmosManager.ProcessPipe(atmosManager.pipeList[i]);
					}
					catch (Exception e)
					{
						Loggy.Error(e.ToString());
					}

				}


				lock (atmosManager.addingUpdates)
				{
					var count = atmosManager.addingUpdates.Count;
					for (int i = 0; i < count; i++)
					{
						atmosManager.atmosphericsUpdates.Add(atmosManager.addingUpdates[i]);
					}
					atmosManager.addingUpdates.Clear();

					count = atmosManager.removeingUpdates.Count;
					for (int i = 0; i < count; i++)
					{
						atmosManager.atmosphericsUpdates.Remove(atmosManager.removeingUpdates[i]);
					}
					atmosManager.removeingUpdates.Clear();
				}

				var loopFor = atmosManager.atmosphericsUpdates.Count;

				for (int i = 0; i < loopFor; i++)
				{
					try
					{
						atmosManager.ProcessAction(atmosManager.atmosphericsUpdates[i]);
					}
					catch (Exception e)
					{
						Loggy.Error(e.ToString());
					}
				}

			}

			foreach (var reactionManger in atmosManager.reactionManagerList)
			{
				try
				{
					reactionManger.DoTick();
				}
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
				}
			}


			atmosManager.sampler.End();
		}
	}
}