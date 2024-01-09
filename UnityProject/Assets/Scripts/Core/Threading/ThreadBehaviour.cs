﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Logs;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Profiling;

namespace Core.Threading
{
	public class ThreadedBehaviour : MonoBehaviour
	{
		public bool running { get; private set; }
		public uint ticker;

		[Range(1, 5000)]
		public int tickDelay = 100;

		private Thread workingThread;
		public static readonly List<ThreadedBehaviour> currentThreads = new List<ThreadedBehaviour>();

		[OnValueChanged(nameof(OnThreadModeChange))]
		public ThreadMode threadMode = ThreadMode.Threaded;
		private bool manual => threadMode == ThreadMode.Manual;
		private bool notManual => threadMode != ThreadMode.Manual;

		private Stopwatch mainThreadTimer;

		public bool midTick;

		public string threadName;


		private void Awake()
		{
			threadName = GetType().Name;
			mainThreadTimer = new Stopwatch();
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			StopThread();
		}

		/// <summary>
		/// Main thread loop, if set to it
		/// </summary>
		private void UpdateMe()
		{
			if(running && threadMode == ThreadMode.MainThread && midTick == false)
			{
				if (mainThreadTimer.Elapsed.Milliseconds < tickDelay)
				{
					return;
				}
				mainThreadTimer.Restart();
				try
				{
					RunTick();
				}
				catch (Exception e)
				{
					Loggy.LogError(e.ToString(), Category.Threading);
					midTick = false;
				}

				ticker++;
			}
		}

		/// <summary>
		/// The thread loop, it will constantly sleep and call the main tick
		/// </summary>
		private void ThreadLoop()
		{
			running = true;
			Profiler.BeginThreadProfiling("Unitystation", threadName);
			while (running && threadMode == ThreadMode.Threaded && midTick == false)
			{
#if UNITY_EDITOR
				if (PauseStateChangedEditor.IsPaused && threadMode == ThreadMode.Threaded)
				{
					Thread.Sleep(5000);
				}
#endif


				try
				{
					RunTick();
				}
				catch (Exception e)
				{
					ThreadLoggy.AddLog(e.ToString(), Category.Threading);
					midTick = false;
					if (threadMode == ThreadMode.Threaded)
					{
						Thread.Sleep(10000); //Resume after a 10s
					}
				}

				Thread.Sleep(tickDelay);
				ticker++;
			}
			Profiler.EndThreadProfiling();
			running = false;
		}

		/// <summary>
		/// Starts the thread
		/// </summary>
		[DisableIf(EConditionOperator.Or, nameof(running), nameof(manual))]
		[Button("Start")]
		public void StartThread()
		{
			if (threadMode == ThreadMode.Threaded)
			{
				workingThread = new Thread (ThreadLoop);
				workingThread.Start();
				currentThreads.Add(this);
				Loggy.LogFormat("<b>{0}</b> Started", Category.Threading, GetType().Name);
			}
			else if (threadMode == ThreadMode.MainThread)
			{
				mainThreadTimer.Start();
				running = true;
			}

		}

		/// <summary>
		/// Signal the thread to stop, it wont be instant
		/// </summary>
		[EnableIf(EConditionOperator.And, nameof(running), nameof(notManual))]
		[Button("Stop")]
		public void StopThread()
		{
			if (workingThread != null)
			{
				workingThread.Abort();
				workingThread = null;
			}
			currentThreads.Remove(this);
			Loggy.LogFormat("<b>{0}</b> Stopped", Category.Threading, GetType().Name);
			running = false;
		}

		private void RunTick()
		{
			midTick = true;
			ThreadedWork();
			midTick = false;
		}

		/// <summary>
		/// Runs for each 'tick' of the thread, override this!
		/// </summary>
		public virtual void ThreadedWork()
		{
		}

		/// <summary>
		/// An user runs this to do 1 step, it'll be in the main thread
		/// </summary>
		[EnableIf(nameof(manual))]
		[Button("Manual Step")]
		public void ManualStep()
		{
			if (running == false)
			{
				running = true;
				ThreadedWork();
				ticker++;
				running = false;
			}
		}

		public void OnThreadModeChange()
		{
			StopThread();
		}

	}

	public enum ThreadMode
	{
		Threaded,
		MainThread,
		Manual
	}
}

