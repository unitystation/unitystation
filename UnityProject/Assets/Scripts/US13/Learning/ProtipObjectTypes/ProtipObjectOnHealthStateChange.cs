using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using UnityEngine.Serialization;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.HealthV2.Living.Metabolism;
using US13.Managers.NetworkManagement;

namespace US13.Learning.ProtipObjectTypes
{
	public class ProtipObjectOnHealthStateChange : ProtipObject
	{
		private HealthStateController healthState;

		private ProtipSO fatsoEvent;
		[FormerlySerializedAs("CritHealthEvent")] [SerializeField]
		private ProtipSO critHealthEvent;
		[FormerlySerializedAs("SoftHealthEvent")] [SerializeField]
		private ProtipSO softHealthEvent;
		[FormerlySerializedAs("DeathlHealthEvent")] [SerializeField]
		private ProtipSO deathlHealthEvent;

		[FormerlySerializedAs("UnconsciousEvent")] [SerializeField]
		private ProtipSO unconsciousEvent;
		[FormerlySerializedAs("BarelyConsciousEvent")] [SerializeField]
		private ProtipSO barelyConsciousEvent;


		private HungerState lastHungerState = HungerState.Normal;

		private const float PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM = 35f;

		private HashSet<ProtipSO> OnCooldown = new HashSet<ProtipSO>();

		private void Awake()
		{
			healthState = GetComponentInParent<HealthStateController>();
			if (healthState != null) return;
			Loggy.Error($"[Protips/HealthStateChangeTip/{gameObject} - Missing Health State controller.]");
			Destroy(this);
		}

		private void OnEnable()
		{
			if(CustomNetworkManager.IsHeadless) return;
			healthState.ConsciousEvent += TriggerConsciousEvent;

		}

		private void OnDisable()
		{
			if(CustomNetworkManager.IsHeadless) return;
			healthState.ConsciousEvent -= TriggerConsciousEvent;
		}

		private void TriggerConsciousEvent(ConsciousState state)
		{
			if (IsPlayerGhostInBody()) return;
			switch (state)
			{
				case ConsciousState.BARELY_CONSCIOUS:
					StandardTrigger(barelyConsciousEvent);
					break;
				case ConsciousState.UNCONSCIOUS:
					StandardTrigger(unconsciousEvent);
					break;
				case ConsciousState.DEAD:
					StandardTrigger(deathlHealthEvent);
					break;
				default:
					break;
			}
		}


		public void StandardTrigger(ProtipSO ProtipSO)
		{
			if (OnCooldown.Contains(ProtipSO)) return;
			TriggerTip(ProtipSO);
			StartCoroutine(Cooldown(ProtipSO));
		}


		private bool IsPlayerGhostInBody()
		{
			return healthState.livingHealthMasterBase.playerScript.IsGhost;
		}

		private IEnumerator Cooldown(ProtipSO ProtipSO)
		{
			OnCooldown.Add(ProtipSO);
			yield return WaitFor.Seconds(PROTIP_COOLDOWN_TO_AVOID_QUEUE_SPAM);
			OnCooldown.Remove(ProtipSO);
		}


	}
}