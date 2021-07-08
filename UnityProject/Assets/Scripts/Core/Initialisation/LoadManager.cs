using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NaughtyAttributes;
using UnityEngine;

namespace Initialisation
{
	public class LoadManager : MonoBehaviourSingleton<LoadManager>
	{
		[ReorderableList] public List<MonoBehaviour> GamesStartInitialiseSystems = new List<MonoBehaviour>();

		public int TargetMSprefFrame = 50;

		public Stopwatch stopwatch = new Stopwatch();

		public static Queue<Action> QueueInitialise = new Queue<Action>();


		//ServerData Awake moved to Start
		//OptionsMenu Awake moved to Start
		//ThemeManager Awake moved to Start

		//CustomNetworkManager Needs be first

		//add Sound manager
		//ok  one for Game start
		//another one for Round load
		//Don't put heavy stuff in awake
		//Don't use for awake
		//Have hard references for defined systems,
		//Otherwise
		//call Manager with function and what to Load before

		public static void RegisterAction(Action InAction)
		{
			QueueInitialise.Enqueue(InAction);
		}

		public void Update()
		{
			if (GamesStartInitialiseSystems.Count > 0)
			{
				var ToProcess = GamesStartInitialiseSystems[0];
				GamesStartInitialiseSystems.RemoveAt(0);
				var InInterface = ToProcess as IInitialise;
				if (InInterface == null) return;
				InInterface.Initialise();
			}

			if (QueueInitialise.Count > 0)
			{
				//Logger.Log(QueueInitialise.Count.ToString() + " < in queue ");
				stopwatch.Reset();
				stopwatch.Start();
				Action QueueAction = null;
				while (stopwatch.ElapsedMilliseconds < TargetMSprefFrame)
				{
					if (QueueInitialise.Count > 0)
					{
						QueueAction = QueueInitialise.Dequeue();
						QueueAction.Invoke();
					}
					else
					{
						break;
					}
				}

				stopwatch.Stop();
				//Logger.Log(stopwatch.ElapsedMilliseconds.ToString() + " < ElapsedMilliseconds ");
			}
			SpawnSafeThread.Process();
		}
	}

	public enum InitialisationSystems
	{
		None = 0,
		GameManager,
		AutoMod,
		CentComm,
		TileManager,
		Synth,
		ServerData,
		StringManager,
		VariableViewerManager,
		OptionsMenu,
		ThemeManager,
		UIManager,
		ResponsiveUI,
		Highlight,
		ChatBubbleManager,
		ServerInfoUI,
		CustomNetworkManager,
		ServerInfoUILobby,
		Addressables
	}

	public interface IInitialise
	{
		InitialisationSystems Subsystem { get; }
		void Initialise();
	}
}