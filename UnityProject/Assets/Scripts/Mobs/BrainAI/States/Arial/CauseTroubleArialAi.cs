using System;
using AddressableReferences;
using Core;
using Items;
using UnityEngine;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

namespace Mobs.BrainAI.States.Arial
{
	public class CauseTroubleArialAi : BrainMobState
	{
		public GameObject Target;
		private UniversalObjectPhysics thingToThrow;
		private TimeSpan troubleEnterTime;
		[SerializeField] private LookForTroubleArialAi troubleState;
		[SerializeField] private AddressableAudioSource stateEnter;
		[SerializeField] private AddressableAudioSource stateExit;

		public override void OnEnterState()
		{
			thingToThrow = GetObjectToThrow();
			if (stateEnter is not null) SoundManager.PlayNetworkedAtPos(stateEnter, master.gameObject.AssumedWorldPosServer());
			troubleEnterTime = DateTime.Now.TimeOfDay;
		}

		public override void OnExitState()
		{
			if (stateExit is not null) SoundManager.PlayNetworkedAtPos(stateExit, master.gameObject.AssumedWorldPosServer());
			thingToThrow = null;
		}

		public override void OnUpdateTick()
		{
			if (thingToThrow == null)
			{
				master.AddRemoveState(this, troubleState);
				return;
			}
			thingToThrow.SetTransform(LivingHealthMaster.gameObject.AssumedWorldPosServer(), true);
			if (troubleEnterTime.TotalSeconds + 5 > DateTime.Now.TimeOfDay.TotalSeconds) return;
			thingToThrow.NewtonianPush((Target.AssumedWorldPosServer() - thingToThrow.gameObject.AssumedWorldPosServer()).normalized, 35);
			master.AddRemoveState(this, troubleState);
		}

		public override bool HasGoal()
		{
			return true;
		}

		private UniversalObjectPhysics GetObjectToThrow()
		{
			if (DMMath.Prob(1.25f))
			{
				var p = PlayerList.Instance.GetAlivePlayers().PickRandom();
				if (p != null) return p.Script.playerMove;
			}
			var objects = ComponentsTracker<ItemAttributesV2>.GetAllNearbyTypesToTarget(master.gameObject, 9, false);
			if (objects == null || objects.Count == 0)
			{
				return null;
			}
			return objects.PickRandom().gameObject.GetComponent<UniversalObjectPhysics>();
		}
	}
}