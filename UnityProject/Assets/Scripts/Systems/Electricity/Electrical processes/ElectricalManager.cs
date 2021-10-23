using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Mirror;

namespace Systems.Electricity
{
	public class ElectricalManager : MonoBehaviour
	{
		public CableTileList HighVoltageCables;
		public CableTileList MediumVoltageCables;
		public CableTileList LowVoltageCables;
		public ElectricalSynchronisation electricalSync;

		private bool roundStartedServer = false;
		public bool Running { get; private set; }
		public float MSSpeed = 100;

		public static ElectricalManager Instance {
			get {
				return instance;
			}
			set { instance = value; }
		}

		private static ElectricalManager instance;

		public ElectricalMode Mode;

		public bool DOCheck;

		private Object electricalLock = new Object();

		public static Object ElectricalLock => Instance.electricalLock;

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
		}

		//Server Side Only
		private void UpdateMe()
		{
			if(roundStartedServer == false) return;

			if (Mode == ElectricalMode.GameLoop && Running)
			{
				electricalSync.DoUpdate(false);
			}

			if (Running)
			{
				if (electricalSync.MainThreadProcess)
				{
					lock (ElectricalLock)
					{
						electricalSync.PowerNetworkUpdate();
						electricalSync.MainThreadProcess = false;
						Monitor.Pulse(ElectricalLock);
					}
				}
			}
		}

		private void OnApplicationQuit()
		{
			StopSim();
		}

		private void OnEnable()
		{
			EventManager.AddHandler(Event.PostRoundStarted, StartSim);
			EventManager.AddHandler(Event.RoundEnded, StopSim);

			if(Application.isEditor == false && NetworkServer.active == false) return;

			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.PostRoundStarted, StartSim);
			EventManager.RemoveHandler(Event.RoundEnded, StopSim);

			if(Application.isEditor == false && NetworkServer.active == false) return;

			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		public void StartSim()
		{
			if (!CustomNetworkManager.Instance._isServer) return;


			roundStartedServer = true;
			Running = true;

			electricalSync.Initialise();
			if (Mode == ElectricalMode.Threaded)
			{
				electricalSync.SetSpeed((int)MSSpeed);
				electricalSync.StartSim();
			}

			Logger.Log("Round Started", Category.Electrical);
		}

		public void StopSim()
		{
			if (!CustomNetworkManager.Instance._isServer) return;

			Running = false;
			electricalSync.StopSim();
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
}
