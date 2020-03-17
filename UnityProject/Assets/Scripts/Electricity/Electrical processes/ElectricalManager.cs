using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Atmospherics;
using Tilemaps.Behaviours.Meta;
using System.Diagnostics;
using System;
using System.Threading;


public class ElectricalManager : MonoBehaviour
{
	public static ElectricalManager Instance;
	public DeadEndConnection defaultDeadEnd;
	private bool roundStartedServer = false;
	public bool Running { get; private set; }
	public float MSSpeed = 100;
	public ElectricalMode Mode = ElectricalMode.Threaded;


	public void StartSimulation()
	{
		Running = true;

		if (Mode == ElectricalMode.Threaded)
		{
			ElectricalSynchronisation.SetSpeed((int)MSSpeed);
			ElectricalSynchronisation.Start();
		}
	}

	public void StopSimulation()
	{
		Running = false;
		ElectricalSynchronisation.Stop();
	}

	private void Update()
	{
		if (roundStartedServer && CustomNetworkManager.Instance._isServer && Mode == ElectricalMode.GameLoop && Running)
		{
			//try
			//{
				ElectricalSynchronisation.DoUpdate(false);
			//}
			//catch (Exception e)
			//{
			//	Logger.LogError($"Exception in Electrical Thread! Will no longer Electrical!!\n{e.StackTrace}",
			//	                Category.Electrical);
			//	throw;
			//}
		}
		if (roundStartedServer && CustomNetworkManager.Instance._isServer && Running)
		{
			lock (ElectricalSynchronisation.Electriclock)
			{
				if (ElectricalSynchronisation.MainThreadProcess)
				{
					ElectricalSynchronisation.PowerNetworkUpdate();
				}
				ElectricalSynchronisation.MainThreadProcess = false;
				Monitor.Pulse(ElectricalSynchronisation.Electriclock);
			}
		}

	}


	private void Start()
	{
		if (Mode != ElectricalMode.Manual)
		{
			StartCoroutine(WaitForElectricalInitialisation());
			//StartSimulation();
		}
	}

	IEnumerator WaitForElectricalInitialisation()
	{
		yield return WaitFor.Seconds(4f);
		StartSimulation();
	}

	private void OnApplicationQuit()
	{
		StopSimulation();
	}

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.RoundStarted, OnRoundStart);
		EventManager.AddHandler(EVENT.RoundEnded, OnRoundEnd);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.RoundStarted, OnRoundStart);
		EventManager.RemoveHandler(EVENT.RoundEnded, OnRoundEnd);
	}

	void OnRoundStart()
	{
		roundStartedServer = true;
		Logger.Log("Round Started", Category.Electrical);
	}

	void OnRoundEnd()
	{
		roundStartedServer = false;
		ElectricalSynchronisation.Reset();
		Logger.Log("Round Ended", Category.Electrical);
	}
}

public enum ElectricalMode
{
	Threaded,
	GameLoop,
	Manual
}