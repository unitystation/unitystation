using System;
using System.Collections;
using System.Collections.Generic;
using Pipes;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AtmosManager : MonoBehaviour
{
	/// <summary>
	/// Time it takes per update in milliseconds
	/// </summary>
	public float Speed = 40;
	public int NumberThreads = 1;

	public AtmosMode Mode = AtmosMode.Threaded;

	public bool Running { get; private set; }

	public bool roundStartedServer = false;
	public HashSet<PipeData> inGameNewPipes = new HashSet<PipeData>();
	public HashSet<FireAlarm> inGameFireAlarms = new HashSet<FireAlarm>();
	public static int currentTick;
	public static float tickRateComplete = 0.25f; //currently set to update every second
	public static float tickRate;
	private static float tickCount = 0f;
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

		if (roundStartedServer)
		{
			if (tickRate == 0)
			{
				tickRate = tickRateComplete / Steps;
			}

			tickCount += Time.deltaTime;

			if (tickCount > tickRate)
			{
				DoTick();
				tickCount = 0f;
				currentTick = ++currentTick % Steps;
			}
		}
	}

	void DoTick()
	{
		if (StopPipes == false)
		{
			foreach (var p in inGameNewPipes)
			{
				p.TickUpdate();
			}
		}

		foreach (FireAlarm firealarm in inGameFireAlarms)
		{
			firealarm.TickUpdate();
		}
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
		StartCoroutine(SetPipes());
	}

	private IEnumerator SetPipes() /// TODO: FIX ALL MANAGERS LOADING ORDER AND REMOVE ANY WAITFORSECONDS
	{
		yield return new WaitForSeconds(2);
		roundStartedServer = true;
	}

	void OnRoundEnd()
	{
		roundStartedServer = false;
		AtmosThread.ClearAllNodes();
		inGameNewPipes.Clear();
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
		if (newScene.name == "Lobby")
		{
			roundStartedServer = false;
		}
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