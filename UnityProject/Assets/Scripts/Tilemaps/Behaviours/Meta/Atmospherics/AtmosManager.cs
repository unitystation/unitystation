using System;
using System.Collections;
using System.Collections.Generic;
using Pipes;
using UnityEngine;
using UnityEngine.SceneManagement;
using Objects.Wallmounts;
using System.Collections.Concurrent;

namespace Systems.Atmospherics
{
	public class AtmosManager : MonoBehaviour
	{
		/// <summary>
		/// Time it takes per update in milliseconds
		/// </summary>
		public float Speed = 40;
		public int NumberThreads = 1;

		public AtmosMode Mode = AtmosMode.Threaded;

		public bool Running { get; private set; }

		public HashSet<PipeData> inGameNewPipes = new HashSet<PipeData>();
		public HashSet<FireAlarm> inGameFireAlarms = new HashSet<FireAlarm>();
		public ConcurrentBag<PipeData> pipeToAdd = new ConcurrentBag<PipeData>();

		public static int currentTick;
		private const int Steps = 1;

		public static AtmosManager Instance;

		public bool StopPipes = false;

		public GameObject fireLight = null;

		public GameObject iceShard = null;
		public GameObject hotIce = null;

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(this);
			}
		}

		private void Update()
		{
			if (Mode == AtmosMode.GameLoop && Running)
			{
				try
				{
					AtmosThread.RunStep();
				}
				catch (Exception e)
				{
					Logger.LogError($"Exception in Atmos Thread! Will no longer mix gases!\n{e.StackTrace}",
						Category.Atmos);
					throw;
				}
			}
		}

		public void DoTick()
		{
			if (StopPipes == false)
			{
				foreach (var pipeData in pipeToAdd)
				{
					if(pipeData.MonoPipe == null)
						continue;
					pipeData.TickUpdate();
				}
			}

			currentTick = ++currentTick % Steps;
		}

		public void AddPipe(PipeData pipeData)
		{
			pipeToAdd.Add(pipeData);
		}

		public void RemovePipe(PipeData pipeData)
		{
			pipeToAdd.TryTake(out pipeData);
		}

		void OnEnable()
		{
			EventManager.AddHandler(EVENT.RoundStarted, OnRoundStart);
			EventManager.AddHandler(EVENT.RoundEnded, OnRoundEnd);
			SceneManager.activeSceneChanged += OnSceneChange;
		}

		void OnDisable()
		{
			EventManager.RemoveHandler(EVENT.RoundStarted, OnRoundStart);
			EventManager.RemoveHandler(EVENT.RoundEnded, OnRoundEnd);
			SceneManager.activeSceneChanged -= OnSceneChange;
		}

		void OnRoundStart()
		{
			if (Mode != AtmosMode.Manual)
			{
				StartSimulation();
			}
		}

		void OnRoundEnd()
		{
			AtmosThread.ClearAllNodes();
			inGameNewPipes.Clear();
			StopSimulation();
		}

		private void OnApplicationQuit()
		{
			StopSimulation();
		}

		public void StartSimulation()
		{
			if (!CustomNetworkManager.Instance._isServer) return;

			Running = true;

			if (Mode == AtmosMode.Threaded)
			{
				AtmosThread.SetSpeed((int)Speed);
				AtmosThread.Start();
			}
		}

		public void StopSimulation()
		{
			if (!CustomNetworkManager.Instance._isServer) return;

			Running = false;

			AtmosThread.Stop();
		}

		public static void SetInternalSpeed()
		{
			AtmosThread.SetSpeed((int)Instance.Speed);
		}

		public static void Update(MetaDataNode node)
		{
			AtmosThread.Enqueue(node);
		}

		void OnSceneChange(Scene oldScene, Scene newScene)
		{
			inGameNewPipes.Clear();
			inGameFireAlarms.Clear();
		}
	}

	public enum AtmosMode
	{
		Threaded,
		GameLoop,
		Manual
	}
}
