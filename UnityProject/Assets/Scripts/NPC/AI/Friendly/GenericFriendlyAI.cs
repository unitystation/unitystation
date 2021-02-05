using System.Collections;
using UnityEngine;
using WebSocketSharp;
using Systems.Mob;
using Random = UnityEngine.Random;

namespace Systems.MobAIs
{
	public class GenericFriendlyAI : MobAI, IServerSpawn
	{
		protected float timeForNextRandomAction;
		protected float timeWaiting;
		[SerializeField]
		protected float minTimeBetweenRandomActions = 10f;
		[SerializeField]
		protected float maxTimeBetweenRandomActions = 30f;
		[SerializeField]
		protected bool doRandomActionWhenInTask = false;
		protected SimpleAnimal simpleAnimal;
		public string MobName => mobName.Capitalize();

		protected override void Awake()
		{
			base.Awake();
			simpleAnimal = GetComponent<SimpleAnimal>();
			BeginExploring();
		}

		protected override void UpdateMe()
		{
			if (!MatrixManager.IsInitialized || health.IsDead || health.IsCrit || health.IsCardiacArrest) return;

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

		protected virtual void DoRandomAction() {}

		public void OnSpawnServer(SpawnInfo info)
		{
			OnSpawnMob();
		}

		protected virtual void OnSpawnMob()
		{
			mobSprite.SetToNPCLayer();
			registerObject.RestoreAllToDefault();
			if (simpleAnimal != null)
			{
				simpleAnimal.SetDeadState(false);
			}
		}

		protected override void OnAttackReceived(GameObject damagedBy = null)
		{
			StartFleeing(damagedBy, 5f);
		}

		protected virtual void OnExploringStopped(){}
		protected virtual void OnFleeingStopped(){}
		protected virtual void OnFollowStopped(){}

	}
}
