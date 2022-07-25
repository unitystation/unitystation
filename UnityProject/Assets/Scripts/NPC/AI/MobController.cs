using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Systems.Mob.MobV2.AI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Systems.MobAIs
{
	public class MobController : MonoBehaviour
	{
		public RegisterTile RegisterTile;

		public MobStateMachine StateMachine;

		private const float STATE_MACHINE_UPDATE_CALL_TIME = 2.25f;

		public void Awake()
		{
			RegisterTile = GetComponent<RegisterTile>();
		}

		private void OnEnable()
		{
			if(CustomNetworkManager.IsServer == false) return;
			UpdateManager.Add(UpdateMe, STATE_MACHINE_UPDATE_CALL_TIME);
		}

		private void OnDisable()
		{
			if(CustomNetworkManager.IsServer == false) return;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			if(StateMachine.ProccessingStates) return;
			StartCoroutine(StateMachine.CycleThroughStates());
		}
	}
}