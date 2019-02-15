using System.Collections.Generic;
using System.Threading;
using Atmospherics;
using Tilemaps.Behaviours.Meta;
using UnityEngine;

public static class AtmosThread
{
	private static bool running;

	private static Object lockGetWork = new Object();

	private static AtmosSimulation simulation;

	static AtmosThread()
	{
		simulation = new AtmosSimulation();
	}

	public static void Enqueue(MetaDataNode node)
	{
		simulation.AddToUpdateList(node);

		lock (lockGetWork)
		{
			Monitor.PulseAll(lockGetWork);
		}
	}

	public static void Start()
	{
		if (!running)
		{
			new Thread(Run).Start();

			running = true;
		}
	}

	public static void Stop()
	{
		running = false;

		lock (lockGetWork)
		{
			Monitor.PulseAll(lockGetWork);
		}
	}

	public static void SetSpeed(float speed)
	{
		simulation.Speed = speed;
	}

	public static int GetUpdateListCount()
	{
		return simulation.UpdateListCount;
	}

	public static void RunStep()
	{
		simulation.Run();
	}

	private static void Run()
	{
		while (running)
		{
			if (!simulation.IsIdle)
			{
				RunStep();
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
}