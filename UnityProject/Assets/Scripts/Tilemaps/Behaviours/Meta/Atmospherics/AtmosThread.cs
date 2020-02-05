using System.Collections.Generic;
using System.Threading;
using Atmospherics;
using Tilemaps.Behaviours.Meta;
using UnityEngine;
using System.Diagnostics;
using System;

public static class AtmosThread
{
	private static bool running;

	private static Stopwatch StopWatch = new Stopwatch();

	private static int MillieSecondDelay; // = 40‬;

	private static UnityEngine.Object lockGetWork = new UnityEngine.Object();

	private static AtmosSimulation simulation;

	static AtmosThread()
	{
		simulation = new AtmosSimulation();
	}

	public static void ClearAllNodes()
	{
		simulation.ClearUpdateList();
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

	public static void SetSpeed(int speed)
	{
		MillieSecondDelay = speed;
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
				StopWatch.Restart();
				RunStep();
				StopWatch.Stop();
				if (StopWatch.ElapsedMilliseconds < MillieSecondDelay)
				{
					Thread.Sleep(MillieSecondDelay - (int)StopWatch.ElapsedMilliseconds);
				}
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