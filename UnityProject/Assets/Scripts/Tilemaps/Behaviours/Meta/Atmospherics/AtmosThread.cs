using System;
using System.Collections.Generic;
using System.Threading;
using Systems.Atmospherics;
using System.Diagnostics;
using UnityEngine.Profiling;

public static class AtmosThread
{
	private static bool running;

	private static Stopwatch StopWatch = new Stopwatch();

	private static int MillieSecondDelay; // = 40‬;

	private static UnityEngine.Object lockGetWork = new UnityEngine.Object();

	private static AtmosSimulation simulation;

	private static CustomSampler sampler;

	public static List<ReactionManager> reactionManagerList {get; private set;} = new List<ReactionManager>();

	//Can't be in LavaTileInteraction SO as it gets saved and not reset over rounds
	public static bool runLavaFireTick;

	static AtmosThread()
	{
		simulation = new AtmosSimulation();
		sampler = CustomSampler.Create("AtmosphericsStep");
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
			running = true;
			new Thread(Run).Start();
		}
	}

	public static void Stop()
	{
		running = false;
		reactionManagerList.Clear();

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
		AtmosManager.Instance.DoTick();
		foreach (var reactionManger in reactionManagerList)
		{
			reactionManger.DoTick();
		}
	}

	private static void Run()
	{
		Profiler.BeginThreadProfiling("Unitystation", "Atmospherics");
		while (running)
		{
			sampler.Begin();
			StopWatch.Restart();

			try
			{
				RunStep();
			}
			catch (Exception e)
			{
				Logger.LogError($"Atmos Thread Error! {e.GetStack()}", Category.Atmos);
			}

			StopWatch.Stop();
			sampler.End();
			if (StopWatch.ElapsedMilliseconds < MillieSecondDelay)
			{
				Thread.Sleep(MillieSecondDelay - (int)StopWatch.ElapsedMilliseconds);
			}
		}
		Profiler.EndThreadProfiling();
	}

	public static bool IsInUpdateList(MetaDataNode node)
	{
		return simulation.IsInUpdateList(node);
	}

}