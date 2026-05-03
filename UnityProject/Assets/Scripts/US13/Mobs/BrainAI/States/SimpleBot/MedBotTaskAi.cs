using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Logs;
using UnityEngine;
using US13.Core;
using US13.Health.Objects;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Managers;
using US13.Mobs.Traversal;
using US13.Player;
using US13.Tilemaps.Behaviours.Layers;
using Util;

namespace US13.Mobs.BrainAI.States.SimpleBot
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
			var path = FindTarget(out targetCell, out targetMatrix);
			searchRadius = 5;

			if (path == null || path.Count == 0) master.RemoveAddState(this, findSimpleTaskAi); //If cant heal without moving, return to search state
			else DoTask();
		}
		protected override bool IsCurrentTaskValid()
		{
			if (creatureToHeal == false || creatureToHeal.mobID == LivingHealthMaster.mobID) return false;
			if (blackListedSpecies.Contains(creatureToHeal.InitialSpecies)) return false;

			var damage = creatureToHeal.GetBruteBurnTotal();
			if (IsEmagged && damage > -100f) return false;
			if (IsEmagged == false && damage <= 0f) return false;

			Vector3 worldPos = creatureToHeal.gameObject.AssumedWorldPosServer();
			return Vector3.Distance(worldPos, LivingHealthMaster.gameObject.AssumedWorldPosServer()) <= 1.5f;
		}

		public override List<Vector3Int> FindTarget(out Vector3Int targetPosition, out Matrix targetMatrixLocal)
		{
			targetMatrixLocal = master.Body.UniversalObjectPhysics.registerTile.Matrix;
			var currentPosition = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(targetMatrixLocal);

			targetPosition = currentPosition;
			creatureToHeal = null;

			var targets = ComponentsTracker<LivingHealthMasterBase>.GetAllNearbyTypesToTarget(master.Body.gameObject, searchRadius, bypassInventories: false);
			foreach(var living in targets)
			{
				if (living.mobID == LivingHealthMaster.mobID) continue;
				if (blackListedSpecies.Contains(living.InitialSpecies)) continue;

				var damage = living.GetBruteBurnTotal();

				if (IsEmagged && damage > -100f) continue;
				if (IsEmagged == false && damage <= 0f) continue;

				var possiblePath = MobTraversal.GeneratePath(currentPosition, targetPosition, targetMatrixLocal, PathfinderType.AStar);
				if (possiblePath == null || possiblePath.Count == 0) continue;

				var worldPos = living.gameObject.AssumedWorldPosServer();
				targetPosition = worldPos.ToLocalInt(targetMatrixLocal);

				this.creatureToHeal = living;
				targetMatrix = targetMatrixLocal;
				targetCell = targetPosition;
				return possiblePath;
			}

			targetMatrix = null;
			targetMatrixLocal = null;
			return null;
		}
	}
}