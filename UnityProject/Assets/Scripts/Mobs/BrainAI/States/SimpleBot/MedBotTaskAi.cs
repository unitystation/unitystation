using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HealthV2;
using Logs;
using UnityEngine;

namespace Mobs.BrainAI.States.SimpleBot
{

	public class MedBotTaskAi : SimpleBotTaskAi
	{
		private LivingHealthMasterBase creatureToHeal = null;
		[SerializeField] private List<PlayerHealthData> blackListedSpecies = new List<PlayerHealthData>();

		private Collider2D[] possiblePlayers = new Collider2D[5];
		[SerializeField] private ContactFilter2D contactFilter;

		public override void OnEnterState()
		{
			if (creatureToHeal == false)
			{
				Loggy.Error("MedBotTaskAi: Attempted to enter state but creatureToHeal was null!");
				master.RemoveAddState(this, findSimpleTaskAi);
				return;
			}

			searchRadius = 5;
			isPerformingTask = false;

			DoTask();
		}

		protected override async UniTask PerformTask()
		{
			isPerformingTask = true;
			SoundManager.PlayNetworkedAtPos(IsEmagged ? emaggedPerformSound : taskPerformSound, LivingHealthMaster.gameObject.AssumedWorldPosServer(), global: false);

			if (IsEmagged && IsCurrentTaskValid() == true) creatureToHeal.ApplyDamageToRandomBodyPart(master.Body.gameObject, 5f, AttackType.Melee, DamageType.Brute);

			bool isCancelled = await UniTask
				.Delay(TimeSpan.FromSeconds(taskPerformDuration), cancellationToken: cancellationTokenSource.Token)
				.SuppressCancellationThrow();

			isPerformingTask = false;

			if (isCancelled)
			{
				master.RemoveAddState(this, findSimpleTaskAi);
				return;
			}
			if (IsEmagged == false && IsCurrentTaskValid() == true)
			{
				creatureToHeal.HealDamageOnAll(master.Body.gameObject, 2f, DamageType.Brute);
				creatureToHeal.HealDamageOnAll(master.Body.gameObject, 2f, DamageType.Burn);
			}

			searchRadius = 1; //Search nearby tiles to see if it can continue to heal without moving
			bool found = FindTarget(out targetCell, out targetMatrix);
			searchRadius = 5;

			if (found == false) master.RemoveAddState(this, findSimpleTaskAi); //If cant heal without moving, return to search state
			else DoTask();
		}
		protected override bool IsCurrentTaskValid()
		{
			if (creatureToHeal == false || creatureToHeal.mobID == LivingHealthMaster.mobID) return false;
			if (blackListedSpecies.Contains(creatureToHeal.InitialSpecies)) return false;
			if (IsEmagged)
			{
				var worldPosHurt = creatureToHeal.gameObject.AssumedWorldPosServer();
				return Vector3.Distance(worldPosHurt, LivingHealthMaster.gameObject.AssumedWorldPosServer()) <= 1.5f;
			}

			var damage = creatureToHeal.GetBruteBurnTotal();
			if (damage >= 0f) return false;

			Vector3 worldPos = creatureToHeal.gameObject.AssumedWorldPosServer();
			return Vector3.Distance(worldPos, LivingHealthMaster.gameObject.AssumedWorldPosServer()) <= 1.5f;
		}

		public override bool FindTarget(out Vector3Int targetPosition, out Matrix targetMatrixLocal)
		{
			targetMatrixLocal = master.Body.UniversalObjectPhysics.registerTile.Matrix;
			var currentPosition = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(targetMatrixLocal);

			targetPosition = currentPosition;
			creatureToHeal = null;

			var targetCount = Physics2D.OverlapCircle(currentPosition.ToWorld(targetMatrixLocal), searchRadius, contactFilter, possiblePlayers);
			for (int i = 0; i < targetCount; i++)
			{
				var health = possiblePlayers[i].GetComponentCustom<LivingHealthMasterBase>();

				if (health == false || health.mobID == LivingHealthMaster.mobID) continue;
				if (blackListedSpecies.Contains(health.InitialSpecies)) continue;

				var damage = health.GetBruteBurnTotal();

				if (IsEmagged && damage < -100f) continue;
				if (IsEmagged == false && damage >= 0f) continue;

				var worldPos = health.gameObject.AssumedWorldPosServer();
				targetPosition = worldPos.ToLocalInt(targetMatrixLocal);

				this.creatureToHeal = health;
				targetMatrix = targetMatrixLocal;
				targetCell = targetPosition;
				return true;
			}

			targetMatrix = null;
			targetMatrixLocal = null;
			return false;
		}
	}
}