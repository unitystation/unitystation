using System.Collections.Generic;
using System.Threading;
using UnityEngine.Profiling;

public class ThreadedBehaviour
{
	public bool IsRunning { get; private set; }
	public uint Ticker { get; private set; }
	public int TickSpeed = 100;
	private Thread WorkingThread;
	public static List<ThreadedBehaviour> currentThreads = new List<ThreadedBehaviour>();

	public ThreadMode threadMode = ThreadMode.Threaded;

	/// <summary>
	/// Runs when the manager is started causing the thread to commence
	/// </summary>
	public void StartThread()
	{
		WorkingThread = new Thread (ThreadedLoop);
		WorkingThread.Start();
		currentThreads.Add(this);
		Logger.LogFormat("<b>{0}</b> Started", Category.Threading, GetType().Name);
	}

	/// <summary>
	/// Runs when the manager is stopped causing the thread to be aborted
	/// </summary>
	public void StopThread()
	{
		if (WorkingThread != null)
		{
			WorkingThread.Abort();
			WorkingThread = null;
		}
		currentThreads.Remove(this);
		Logger.LogFormat("<b>{0}</b> Stopped", Category.Threading, GetType().Name); ;
		IsRunning = false;
	}

	private void ThreadedLoop()
	{
		IsRunning = true;
		Profiler.BeginThreadProfiling("Unitystation", "RENAME ME");
		while (IsRunning)
		{
			ThreadedWork();
			Thread.Sleep(TickSpeed);
			Ticker++;
		}
		Profiler.EndThreadProfiling();
		IsRunning = false;
	}

	/// <summary>
	/// Runs for each 'tick' of the thread, override this!
	/// </summary>
	public virtual void ThreadedWork()
	{
	}

	public enum ThreadMode
	{
		Threaded,
		GameLoop,
		Manual
	}
}