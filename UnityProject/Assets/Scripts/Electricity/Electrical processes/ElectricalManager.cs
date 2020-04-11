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

	public static ElectricalManager Instance;

	public ElectricalMode Mode;

	public bool DOCheck;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
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
		StopSim();
	}

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.RoundStarted, StartSim);
		EventManager.AddHandler(EVENT.RoundEnded, StopSim);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.RoundStarted, StartSim);
		EventManager.RemoveHandler(EVENT.RoundEnded, StopSim);
	}

	public void StartSim()
	{
		if (!CustomNetworkManager.Instance._isServer) return;
		roundStartedServer = true;
		Running = true;

		if (Mode == ElectricalMode.Threaded)
		{
			electricalSync.SetSpeed((int)MSSpeed);
			electricalSync.Start();
		}

		Logger.Log("Round Started", Category.Electrical);
	}

	public void StopSim()
	{
		if (!CustomNetworkManager.Instance._isServer) return;

		Running = false;
		electricalSync.Stop();
		roundStartedServer = false;
		electricalSync.Reset();
		Logger.Log("Round Ended", Category.Electrical);
	}

	public static void SetInternalSpeed()
	{
		Instance.electricalSync.SetSpeed((int)Instance.MSSpeed);
	}

}

public enum ElectricalMode
{
	Threaded,
	GameLoop,
	Manual
}