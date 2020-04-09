using System.Collections;
using UnityEngine;
using System.Threading;


public class ElectricalManager : MonoBehaviour
{
	public CableTileList HighVoltageCables;
	public CableTileList MediumVoltageCables;
	public CableTileList LowVoltageCables;
	public ElectricalSynchronisation electricalSync;

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
			electricalSync.SetSpeed((int)MSSpeed);
			electricalSync.Start();
		}
	}

	public void StopSimulation()
	{
		Running = false;
		electricalSync.Stop();
	}

	private void Update()
	{
		if (roundStartedServer && CustomNetworkManager.Instance._isServer && Mode == ElectricalMode.GameLoop && Running)
		{
			electricalSync.DoUpdate(false);
		}

		if (roundStartedServer && CustomNetworkManager.Instance._isServer && Running)
		{
			lock (electricalSync.Electriclock)
			{
				if (electricalSync.MainThreadProcess)
				{
					electricalSync.PowerNetworkUpdate();
				}
				electricalSync.MainThreadProcess = false;
				Monitor.Pulse(electricalSync.Electriclock);
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
		electricalSync.Start();
		roundStartedServer = true;
		Logger.Log("Round Started", Category.Electrical);
	}

	void OnRoundEnd()
	{
		electricalSync.Stop();
		roundStartedServer = false;
		electricalSync.Reset();
		Logger.Log("Round Ended", Category.Electrical);
	}
}

public enum ElectricalMode
{
	Threaded,
	GameLoop,
	Manual
}