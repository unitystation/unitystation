using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtmosManager : MonoBehaviour
{
	public float Speed = 0.01f;

	public int NumberThreads = 1;

	public AtmosMode Mode = AtmosMode.Threaded;

	public bool Running { get; private set; }

	public bool roundStartedServer = false;
	public List<Pipe> roundStartPipes = new List<Pipe>();

	private static AtmosManager atmosManager;
	public static AtmosManager Instance
	{
		get
		{
			if (atmosManager == null)
			{
				atmosManager = FindObjectOfType<AtmosManager>();
			}
			return atmosManager;
		}
	}

	private void OnValidate()
	{
		AtmosThread.SetSpeed(Speed);

		// TODO set number of threads
	}

	private void Start()
	{
		if (Mode != AtmosMode.Manual)
		{
			StartSimulation();
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
			catch ( Exception e )
			{
				Logger.LogError( $"Exception in Atmos Thread! Will no longer mix gases!\n{e.StackTrace}", Category.Atmos );
				throw;
			}
		}
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
		StartCoroutine(SetPipes());
	}

	private IEnumerator SetPipes() /// TODO: FIX ALL MANAGERS LOADING ORDER AND REMOVE ANY WAITFORSECONDS
	{
		yield return new WaitForSeconds(2);
		for (int i = 0; i < roundStartPipes.Count; i++)
		{
			roundStartPipes[i].Attach();
		}
	}

	void OnRoundEnd()
	{
		roundStartedServer = false;
	}


	private void OnApplicationQuit()
	{
		StopSimulation();
	}

	public void StartSimulation()
	{
		Running = true;

		if (Mode == AtmosMode.Threaded)
		{
			AtmosThread.Start();
		}
	}

	public void StopSimulation()
	{
		Running = false;

		AtmosThread.Stop();
	}

	public static void Update(MetaDataNode node)
	{
		AtmosThread.Enqueue(node);
	}
}

public enum AtmosMode
{
	Threaded,
	GameLoop,
	Manual
}