using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;
using Managers;

namespace Systems.Radiation
{
	public class RadiationManager : SingletonManager<RadiationManager>
	{
		public List<RadiationPulse> PulseQueue = new List<RadiationPulse>();
		public CustomSampler sampler;
		private RadiationThread radiationThread;

		public override void Awake()
		{
			base.Awake();
			radiationThread = gameObject.AddComponent<RadiationThread>();
			sampler = CustomSampler.Create("RadiationUpdate");
		}

		private void OnEnable()
		{
			EventManager.AddHandler(Event.PostRoundStarted, OnPostRoundStart);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.PostRoundStarted, OnPostRoundStart);
			radiationThread.StopThread();
		}

		private void OnPostRoundStart()
		{
			if (CustomNetworkManager.IsServer == false) return;

			radiationThread.StartThread();
		}

		public void RequestPulse(Vector3Int Location, float Strength, int InSourceID)
		{
			lock (PulseQueue)
			{
				PulseQueue.Add(new RadiationPulse(Location, Strength, InSourceID));
			}
		}
	}

	public struct RadiationPulse
	{
		public Vector3Int Location;
		public float Strength;
		public int SourceID;

		public RadiationPulse( Vector3Int InLocation, float InStrength, int InSourceID)
		{
			Location = InLocation;
			Strength = InStrength;
			SourceID = InSourceID;
		}
	}
}