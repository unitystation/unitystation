using System;
using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using Light2D;
using UnityEngine;
using UnityEngine.Events;
using Utility = UnityEngine.Networking.Utility;
using Mirror;
using UnityEngine.Profiling;

namespace Drones
{
	public class DronePower : MonoBehaviour
	{
		public int mobID { get; private set; }

		public float maxPower = 100;

		public float OverallPower { get; private set; } = 100;

		void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}
		[Server]
		public void CalculateOverallPower()
		{
			float newPower = 100;
			newPower -= UpgradeDrain();
		}
		public float UpgradeDrain()
		{
			return 10;
		}
		public void UpdateMe()
		{
			CalculateOverallPower();
		}
	}
}