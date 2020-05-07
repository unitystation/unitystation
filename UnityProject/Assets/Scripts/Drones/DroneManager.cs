using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;

namespace Drones
{
	public class DroneManager : MonoBehaviour
	{
		public static DroneManager Instance;
		[SerializeField]
		private DroneData DroneData = null;
		private List<DroneStuff> ActiveDrones = new List<DroneStuff>();
		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(gameObject);
			}
		}
		public int DroneCount => ActiveDrones.Count;
		public void RemindDrones()
		{
			foreach(var activeDrone in ActiveDrones)
			{
				activeDrone.DroneMind?.ShowLaws();
			}
		}
		void OnEnable()
		{
			EventManager.AddHandler(EVENT.RoundEnded, OnRoundEnd);
		}

		void OnDisable()
		{
			EventManager.RemoveHandler(EVENT.RoundEnded, OnRoundEnd);
		}
		void OnRoundEnd()
		{
			ResetDrones();
		}
		public void ResetDrones()
		{
			ActiveDrones.Clear();
		}
	}
}