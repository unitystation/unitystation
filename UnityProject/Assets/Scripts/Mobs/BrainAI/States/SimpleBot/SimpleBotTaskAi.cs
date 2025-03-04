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

		[SerializeField] protected float taskPerformDuration = 2f;

		//How far should this bot search for a target? Consider using lower values for less performant checks
		protected int searchRadius = 5;

		protected Matrix targetMatrix;
		protected Vector3Int targetCell;

		protected bool isEmagged = false;
		protected Coroutine taskPerformCoroutine = null;

		public override void OnEnterState()
		{
			DoTask();
		}

		public override void OnExitState()
		{
			targetMatrix = null;
			targetCell = Vector3Int.zero;
			taskPerformCoroutine = null;
		}

		public void DoTask()
		{
			if (taskPerformCoroutine == null && IsCurrentTaskValid() == false)
			{
				master.AddRemoveState(this, findSimpleTaskAi);
				return;
			}

			if (taskPerformCoroutine is not null) return;

			taskPerformCoroutine = StartCoroutine(PerformTask());
		}

		public override void OnUpdateTick()
		{
			//Do nothing
		}

		protected virtual IEnumerator PerformTask()
		{
			yield return WaitFor.Seconds(taskPerformDuration);

			taskPerformCoroutine = null;

			DoTask();
		}

		protected virtual bool IsCurrentTaskValid()
		{
			return false;
		}

		public virtual bool FindTarget(out Vector3Int targetPosition, out Matrix targetMatrix)
		{
			targetPosition = Vector3Int.zero;
			targetMatrix = null;
			return false;
		}


		public override bool HasGoal()
		{
			return targetMatrix;
		}
	}
}