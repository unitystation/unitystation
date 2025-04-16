using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using Chemistry;
using Core.Utils;
using HealthV2;
using Logs;
using Objects.Construction;
using UnityEngine;

namespace Mobs.BrainAI.States.SimpleBot
{

	public class MedBotTaskAi : SimpleBotTaskAi
	{
		private LivingHealthMasterBase creatureToHeal = null;
		[SerializeField] private List<PlayerHealthData> blackListedSpecies = new List<PlayerHealthData>();
		public override void OnEnterState()
		{
			if (creatureToHeal == false)
			{
				Loggy.Error("MedBotTaskAi: Attempted to enter state but creatureToHeal was null!");
				master.RemoveAddState(this, findSimpleTaskAi);
				return;
			}

			searchRadius = 3;
			taskPerformCoroutine = null;

			DoTask();
		}

		protected override IEnumerator PerformTask()
		{
			SoundManager.PlayNetworkedAtPos(IsEmagged ? emaggedPerformSound : taskPerformSound, LivingHealthMaster.gameObject.AssumedWorldPosServer(), global: false);

			if (IsEmagged && IsCurrentTaskValid() == true) creatureToHeal.ApplyDamageToRandomBodyPart(master.Body.gameObject, 5f, AttackType.Melee, DamageType.Brute);

			yield return WaitFor.Seconds(taskPerformDuration);

			if (IsCurrentTaskValid() == true)
			{
				creatureToHeal.HealDamageOnAll(master.Body.gameObject, 5f, DamageType.Brute);
				creatureToHeal.HealDamageOnAll(master.Body.gameObject, 5f, DamageType.Burn);
			}

			taskPerformCoroutine = null;

			searchRadius = 1; //Search nearby tiles to see if it can continue to heal without moving
			bool found = FindTarget(out targetCell, out targetMatrix);
			searchRadius = 5;

			if (found == false) master.RemoveAddState(this, findSimpleTaskAi); //If cant heal without moving, return to search state
			else DoTask();
		}
		protected override bool IsCurrentTaskValid()
		{
			if (creatureToHeal == false || creatureToHeal.OverallHealth < 0.95f * creatureToHeal.MaxHealth)
				return false;

			Vector3 worldPos = creatureToHeal.gameObject.AssumedWorldPosServer();
			return Vector3.Distance(worldPos, LivingHealthMaster.gameObject.AssumedWorldPosServer()) <= 1.5f;
		}

		public override bool FindTarget(out Vector3Int targetPosition, out Matrix targetMatrixLocal)
		{
			targetMatrixLocal = master.Body.UniversalObjectPhysics.registerTile.Matrix;
			var currentPosition = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(targetMatrixLocal);

			targetPosition = currentPosition;
			creatureToHeal = null;

			var possibleTargets = Physics2D.OverlapCircleAll(currentPosition.ToWorld(targetMatrixLocal), searchRadius, LayerMask.GetMask("Players"));
			foreach (var possiblePlayer in possibleTargets)
			{

				var health = possiblePlayer.GetComponentCustom<LivingHealthMasterBase>();
				if (health == LivingHealthMaster) continue;
				if (blackListedSpecies.Contains(health.InitialSpecies)) continue;

				Debug.Log("Found Player to heal");
				if (health == false || health.OverallHealth < 0.95f * health.MaxHealth) continue;
				Debug.Log("Player was valid");
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