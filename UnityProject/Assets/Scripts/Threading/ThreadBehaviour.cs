using System.Threading;
using UnityEngine;

public enum ThreadedBehaviourType
{
	Atmospheric = 0,
	Electricity = 1,
	Room = 2,
	FOV = 3
}

public class ThreadedBehaviour : MonoBehaviour
{
	[Header("Threaded Manager")] public bool IsRunning;

	public int Ticker;
	public int TickSpeed = 1;
	public Thread WorkingThread;

	public float TickSpeedMs => TickSpeed / 1000f;

	/// <summary>
	///     Runs when the manager is started causing the thread to commence
	/// </summary>
	public virtual void StartManager()
	{
		IsRunning = true;

		if (WorkingThread != null)
		{
			WorkingThread.Abort();
			WorkingThread = null;
		}

		WorkingThread = new Thread(ThreadedLoop);
		WorkingThread.Start();
		Logger.LogFormat("<b>{0}</b> Started", Category.Threading, GetType().Name);
		//        ConsoleDebug.AddText("<color=#00FFFF>" + str + "</color>");
	}

	/// <summary>
	///     Runs when the manager is stopped causing the thread to be aborted
	/// </summary>
	public virtual void StopManager()
	{
		if (!this)
		{
			return;
		}
		if (WorkingThread != null)
		{
			WorkingThread.Abort();
			WorkingThread = null;
		}

		Logger.LogFormat("<b>{0}</b> Stopped", Category.Threading, GetType().Name);
		//        ConsoleDebug.AddText("<color=#00FFFF>" + str + "</color>");
		IsRunning = false;
	}

	/// <summary>
	///     Runs for each 'tick' of the thread
	/// </summary>
	public virtual void ThreadedWork()
	{
	}

	public void ThreadedLoop()
	{
		// This pattern lets us interrupt the work at a safe point if neeeded.
		while (IsRunning && this)
		{
			ThreadedWork();
			Thread.Sleep(TickSpeed);
			if (Ticker < int.MaxValue)
			{
				Ticker++;
			}
		}
		IsRunning = false;
	}
}