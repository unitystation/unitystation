using System.Collections;
using AddressableReferences;
using Logs;
using Tiles;
using UnityEngine;

namespace Mobs.BrainAI.States.SimpleBot
{
	public class SimpleBotTaskAi : BrainMobState
	{
		[SerializeField] protected FindSimpleTaskAi findSimpleTaskAi = null;
		[SerializeField] protected AddressableAudioSource taskPerformSound;
		[SerializeField] protected AddressableAudioSource emaggedPerformSound;
		protected bool isEmagged = false;
		protected Coroutine taskPerformCoroutine = null;

		public override void OnEnterState()
		{
			//SimpleBotTaskAi implements no AI behaviour by default. See subclasses for behaviours
		}

		public override void OnExitState()
		{
			//SimpleBotTaskAi implements no AI behaviour by default. See subclasses for behaviours
		}

		public override void OnUpdateTick()
		{
			master.AddRemoveState(this, findSimpleTaskAi); //Exit on first update
		}

		protected virtual bool IsTaskValid()
		{
			return true;
		}

		public virtual bool FindTarget(out Vector3Int targetPosition, out Matrix targetMatrix)
		{
			targetPosition = Vector3Int.zero;
			targetMatrix = null;
			return false;
		}


		public override bool HasGoal()
		{
			return true;
		}
	}
}