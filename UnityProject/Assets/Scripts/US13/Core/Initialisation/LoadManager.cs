using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Logs;
using NaughtyAttributes;
using Shared.Managers;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers.UpdateManager;
using US13.PlayerPrefs;

namespace US13.Core.Initialisation
{
	public class LoadManager : SingletonManager<LoadManager>
	{
		[ReorderableList] public List<MonoBehaviour> GamesStartInitialiseSystems = new List<MonoBehaviour>();

		public int TotalTargetMSprefFrame = 50;

		private int TargetMSprefFramePreStep = 25;

		public Stopwatch stopwatch = new Stopwatch();

		public static Queue<Action> QueueInitialise = new Queue<Action>();

		public static List<DelayedAction> DelayedActions = new List<DelayedAction>();

		public List<DelayedAction> ToClear = new List<DelayedAction>();

		public List<Material> Materials = new List<Material>();

		public bool IsExecuting = false;
		public bool IsExecutingGeneric = false;
		public Action LastInvokedAction {get; private set;}

		public class DelayedAction
		{
			public float Frames;
			public Action Action;
		}

		public bool LoadingInitialSystems => GamesStartInitialiseSystems.Count > 0;

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


		public override void Awake()
		{
			Loggy.MainGameThread = Thread.CurrentThread; //Initialises logger

			// Set the global culture to InvariantCulture
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;


			SetMaterialStatus(null);
		}


		public void SetMaterialStatus(bool? Shadow)
		{
			if (Shadow == null)
			{
				if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.ItemDropShadow) == false)
				{
					UnityEngine.PlayerPrefs.SetString(PlayerPrefKeys.ItemDropShadow, true.ToString());
				}
				Shadow = bool.Parse(UnityEngine.PlayerPrefs.GetString(PlayerPrefKeys.ItemDropShadow));
			}
			else
			{
				UnityEngine.PlayerPrefs.SetString(PlayerPrefKeys.ItemDropShadow, Shadow.Value.ToString());
			}


			foreach (var Material in Materials)
			{
				if (Shadow.Value)
				{
					Material.EnableKeyword("USE_SHADOW");

				}
				else
				{
					Material.DisableKeyword("USE_SHADOW");
				}
			}
		}



		public static void DoInMainThread(Action InAction)
		{
			lock (QueueInitialise)
			{
				QueueInitialise.Enqueue(InAction);
			}
		}


		public static void RegisterAction(Action InAction)
		{
			lock (QueueInitialise)
			{
				QueueInitialise.Enqueue(InAction);
			}
		}

		public static void RegisterActionDelayed(Action InAction, int Frames)
		{
			var ToADD = new DelayedAction();
			ToADD.Action = InAction;
			ToADD.Frames = Frames;
			DelayedActions.Add(ToADD);
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		public void UpdateMe()
		{
			if (GamesStartInitialiseSystems.Count > 0)
			{
				IsExecuting = true;
				var ToProcess = GamesStartInitialiseSystems[0];
				GamesStartInitialiseSystems.RemoveAt(0);
				var InInterface = ToProcess as IInitialise;
				if (InInterface == null) return;
				try
				{
					LastInvokedAction = InInterface.Initialise;
					InInterface.Initialise();
				}
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
				}
				IsExecuting = false;
			}

			if (DelayedActions.Count > 0)
			{
				IsExecuting = true;
				IsExecutingGeneric = true;
				//Loggy.Log(QueueInitialise.Count.ToString() + " < in queue ");
				stopwatch.Start();

				int i = 0;
				while (stopwatch.ElapsedMilliseconds < TargetMSprefFramePreStep && DelayedActions.Count > i)
				{
					if (DelayedActions.Count > 0)
					{
						DelayedAction delayedAction = DelayedActions[i];
						delayedAction.Frames -= 1;
						if (delayedAction.Frames <= 0)
						{
							try
							{
								ToClear.Add(delayedAction);
								LastInvokedAction = delayedAction.Action.Invoke;
								delayedAction.Action.Invoke();
							}
							catch (Exception e)
							{
								Loggy.Error(e.ToString());
							}
						}

						i++;
					}
					else
					{
						break;
					}
				}

				stopwatch.Stop();
				stopwatch.Reset();
				IsExecuting = false;
				IsExecutingGeneric = false;
				//Loggy.Log(stopwatch.ElapsedMilliseconds.ToString() + " < ElapsedMilliseconds ");
			}

			if (QueueInitialise.Count > 0)
			{
				lock (QueueInitialise)
				{
					//Loggy.Log(QueueInitialise.Count.ToString() + " < in queue ");
					stopwatch.Start();
					IsExecuting = true;
					Action QueueAction = null;
					while (stopwatch.ElapsedMilliseconds < TargetMSprefFramePreStep)
					{
						if (QueueInitialise.Count > 0)
						{
							QueueAction = QueueInitialise.Dequeue();
							try
							{
								LastInvokedAction = QueueAction.Invoke;
								QueueAction.Invoke();
							}
							catch (Exception e)
							{
								Loggy.Error(e.ToString());
							}
						}
						else
						{
							break;
						}
					}

					stopwatch.Stop();
					stopwatch.Reset();
					IsExecuting = false;
					IsExecutingGeneric = false;
					//Loggy.Log(stopwatch.ElapsedMilliseconds.ToString() + " < ElapsedMilliseconds ");
				}
			}

			foreach (var delayedAction in ToClear)
			{
				DelayedActions.Remove(delayedAction);
			}
			ToClear.Clear();

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
		Addressables,
		GameData,
		VoiceChat,
		TranslationSystem
	}

	public interface IInitialise
	{
		InitialisationSystems Subsystem { get; }
		void Initialise();
	}
}