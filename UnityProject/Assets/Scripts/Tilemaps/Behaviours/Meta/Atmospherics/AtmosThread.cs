using System.Threading;
using Tilemaps.Behaviours.Meta;
using UnityEngine;

public class AtmosThread
{
	private bool running = true;

	private Object lockGetWork = new Object();

	private Atmospherics atmos;

	public AtmosThread(MetaDataLayer metaDataLayer)
	{
		atmos = new Atmospherics(metaDataLayer);
	}

	public void Enqueue(Vector3Int position)
	{
		atmos.AddToUpdateList(position);

		lock (lockGetWork)
		{
			Monitor.PulseAll(lockGetWork);
		}
	}

	public void Run()
	{
		while (running)
		{
			if (!atmos.IsIdle)
			{
				atmos.Run();
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
}