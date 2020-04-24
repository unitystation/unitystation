using System.Collections;
using UnityEngine;

namespace NPC
{
	public class GenericFriendlyAI : MobAI
	{
		protected string mobNameCap;
		protected float timeForNextRandomAction;
		protected float timeWaiting;
		[SerializeField]
		protected float minTimeBetweenRandomActions = 10f;
		[SerializeField]
		protected float maxTimeBetweenRandomActions = 30f;

		protected override void Awake()
		{
			base.Awake();
			mobNameCap = char.ToUpper(mobName[0]) + mobName.Substring(1);
			BeginExploring();
		}

		protected override void UpdateMe()
		{
			if (health.IsDead || health.IsCrit || health.IsCardiacArrest) return;

			base.UpdateMe();
			MonitorExtras();
		}
		protected override void AIStartServer()
		{
			exploringStopped.AddListener(OnExploringStopped);
			fleeingStopped.AddListener(OnFleeingStopped);
			followingStopped.AddListener(OnFollowStopped);
		}

		protected virtual void MonitorExtras()
		{
			if (IsPerformingTask) return;

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
				$"{mobNameCap} start chasing its own tail!",
				$"{mobNameCap} start chasing its own tail!");

			for (int timesSpun = 0; timesSpun <= times; timesSpun++)
			{
				for (int spriteDir = 1; spriteDir < 5; spriteDir++)
				{
					dirSprites.DoManualChange(spriteDir);
					yield return WaitFor.Seconds(0.3f);
				}
			}

			yield return WaitFor.EndOfFrame;
		}

		protected virtual void DoRandomAction() {}

		protected override void OnAttackReceived(GameObject damagedBy = null)
		{
			StartFleeing(damagedBy, 5f);
		}

		protected virtual void OnExploringStopped(){}
		protected virtual void OnFleeingStopped(){}
		protected virtual void OnFollowStopped(){}

	}
}