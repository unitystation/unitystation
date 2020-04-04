using System.Collections;
using UnityEngine;
using System.Threading;


public class ElectricalManager : MonoBehaviour
{
	public CableTileList HighVoltageCables;
	public CableTileList MediumVoltageCables;
	public CableTileList LowVoltageCables;
	
	private bool roundStartedServer = false;
	public bool Running { get; private set; }
	public float MSSpeed = 100;
	public ElectricalMode Mode = ElectricalMode.Threaded;

	public static ElectricalManager Instance;

	public bool DOCheck;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
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
			ElectricalSynchronisation.DoUpdate(false);
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