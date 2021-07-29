using System.Collections;
using UnityEngine;
using Systems.Mob;
using Random = UnityEngine.Random;

namespace Systems.MobAIs
{
	public class GenericFriendlyAI : MobAI
	{
		protected float timeForNextRandomAction;
		protected float timeWaiting;
		[SerializeField]
		protected float minTimeBetweenRandomActions = 10f;
		[SerializeField]
		protected float maxTimeBetweenRandomActions = 30f;
		[SerializeField]
		protected bool doRandomActionWhenInTask = false;
		public string MobName => mobName.Capitalize();

		#region Lifecycle

		protected override void OnSpawnMob()
		{
			exploringStopped.AddListener(OnExploringStopped);
			fleeingStopped.AddListener(OnFleeingStopped);
			followingStopped.AddListener(OnFollowStopped);
		}

		protected override void OnAIStart()
		{
			BeginExploring();
		}

		#endregion Lifecycle

		protected override void UpdateMe()
		{
			if (MatrixManager.IsInitialized == false || health.IsDead || health.IsCrit) return;

			base.UpdateMe();
			MonitorExtras();
		}

		protected virtual void MonitorExtras()
		{
			if (IsPerformingTask && !doRandomActionWhenInTask)
			{
				return;
			}

			timeWaiting += Time.deltaTime;
			if (timeWaiting < timeForNextRandomAction) return;
			timeWaiting = 0f;
			timeForNextRandomAction = Random.Range(minTimeBetweenRandomActions, maxTimeBetweenRandomActions);
			DoRandomAction();
		}

		protected IEnumerator ChaseTail(int times)
		{
			Chat.AddActionMsgToChat(
				gameObject,
				$"{MobName} starts chasing its own tail!",
				$"{MobName} starts chasing its own tail!");

			for (int timesSpun = 0; timesSpun <= times; timesSpun++)
			{
				for (int spriteDir = 1; spriteDir < 5; spriteDir++)
				{
					directional.FaceDirection(directional.CurrentDirection.Rotate(1));
					yield return WaitFor.Seconds(0.3f);
				}
			}

			yield return WaitFor.EndOfFrame;
		}

		protected override void OnAttackReceived(GameObject damagedBy = null)
		{
			StartFleeing(damagedBy, 5f);
		}

		protected virtual void OnExploringStopped() {}
		protected virtual void OnFleeingStopped() {}
		protected virtual void OnFollowStopped() {}
		protected virtual void DoRandomAction() { }
	}
}
