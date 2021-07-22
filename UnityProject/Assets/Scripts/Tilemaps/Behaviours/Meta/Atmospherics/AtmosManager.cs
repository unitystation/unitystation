using System;
using System.Collections.Generic;
using Pipes;
using UnityEngine;
using UnityEngine.SceneManagement;
using Objects.Wallmounts;

namespace Systems.Atmospherics
{
	public class AtmosManager : SingletonManager<AtmosManager>
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
		private ThreadSafeList<PipeData> pipeList = new ThreadSafeList<PipeData>();
		private GenericDelegate<PipeData> processPipeDelegator;

		public static int currentTick;
		private const int Steps = 1;

		public bool StopPipes = false;

		public GameObject fireLight = null;

		public GameObject iceShard = null;
		public GameObject hotIce = null;

		private void Awake() => processPipeDelegator = ProcessPipe;

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
				pipeList.Iterate(processPipeDelegator);
			}

			currentTick = ++currentTick % Steps;
		}

		private void ProcessPipe(PipeData pipeData) => pipeData.TickUpdate();

		public void AddPipe(PipeData pipeData) => pipeList.Add(pipeData);

		public void RemovePipe(PipeData pipeData) => pipeList.Remove(pipeData);

		void OnEnable()
		{
			EventManager.AddHandler(Event.PostRoundStarted, OnPostRoundStart);
			EventManager.AddHandler(Event.RoundEnded, OnRoundEnd);
			SceneManager.activeSceneChanged += OnSceneChange;
		}

		void OnDisable()
		{
			EventManager.RemoveHandler(Event.PostRoundStarted, OnPostRoundStart);
			EventManager.RemoveHandler(Event.RoundEnded, OnRoundEnd);
			SceneManager.activeSceneChanged -= OnSceneChange;
		}

		void OnPostRoundStart()
		{
			if (Mode != AtmosMode.Manual)
			{
				StartSimulation();
			}
		}

		void OnRoundEnd()
		{
			GasReactions.ResetReactionList();
			AtmosThread.ClearAllNodes();
			inGameNewPipes.Clear();
			StopSimulation();
		}

		private void OnApplicationQuit() => StopSimulation();

		public void StartSimulation()
		{
			if (!CustomNetworkManager.Instance.isServer) return;

			Running = true;

			if (Mode == AtmosMode.Threaded)
			{
				AtmosThread.SetSpeed((int)Speed);
				AtmosThread.Start();
			}
		}

		public void StopSimulation()
		{
			if (!CustomNetworkManager.Instance.isServer) return;

			Running = false;

			AtmosThread.Stop();
		}

		public static void SetInternalSpeed() => AtmosThread.SetSpeed((int)Instance.Speed);

		public static void Update(MetaDataNode node) => AtmosThread.Enqueue(node);

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
