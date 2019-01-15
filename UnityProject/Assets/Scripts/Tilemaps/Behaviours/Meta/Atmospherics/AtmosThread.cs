using System.Collections.Generic;
using System.Threading;
using Atmospherics;
using Tilemaps.Behaviours.Meta;
using UnityEngine;

public class AtmosThread
{
	private bool running = true;

	private Object lockGetWork = new Object();

	private AtmosSimulation simulation;

	public AtmosThread(MetaDataLayer metaDataLayer)
	{
		simulation = new AtmosSimulation(metaDataLayer);
	}

	public void Enqueue(Vector3Int position)
	{
		simulation.AddToUpdateList(position);

		lock (lockGetWork)
		{
			Monitor.PulseAll(lockGetWork);
		}
	}

	public void Run()
	{
		while (running)
		{
			if (!simulation.IsIdle)
			{
				simulation.Run();
			}
			else
			{
				lock (lockGetWork)
				{
					Monitor.Wait(lockGetWork);
				}
			}
		}
	}

	public void Stop()
	{
		running = false;

		lock (lockGetWork)
		{
			Monitor.PulseAll(lockGetWork);
		}
	}

	public void SetSpeed(float speed)
	{
		simulation.Speed = speed;
	}

	public int GetUpdateListCount()
	{
		return simulation.UpdateListCount;
	}
}