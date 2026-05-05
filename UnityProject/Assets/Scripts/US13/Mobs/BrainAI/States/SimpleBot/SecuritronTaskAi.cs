using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using US13.Core;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Items;
using US13.Items.Traits;
using US13.Mobs.Traversal;
using US13.Player;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Utils;
using US13.UI.Objects.Security.SecurityRecordsConsole;
using US13.UI.Systems.Jobs;
using Util;

namespace US13.Mobs.BrainAI.States.SimpleBot
{

	public class SecuritronTaskAi : SimpleBotTaskAi
	{
		private ItemAttributesV2 stunBatonItem = null;
		private GameObject restraints = null;
		[SerializeField] private ItemStorage securitronInternalStorage = null;
		[SerializeField] private ItemTrait handCuffTrait = null;

		private PlayerScript targetPlayer = null;

		public override void OnEnterState()
		{
			isPerformingTask = false;
			if (targetPlayer == null)
			{
				master.RemoveAddState(this, findSimpleTaskAi);
				return;
			}

			if (stunBatonItem == null) stunBatonItem = securitronInternalStorage.GetIndexedItemSlot(0)?.ItemAttributes;

			searchRadius = 5;
			DoTask();
		}

		private GameObject FindCuffs()
		{
			var occupiedSlots = securitronInternalStorage.GetOccupiedSlots();
			foreach (var occupiedSlot in occupiedSlots)
			{
				var item = occupiedSlot.ItemAttributes;
				if (item == stunBatonItem) continue;
				if (item.HasTrait(handCuffTrait) == false) continue;
				return item.gameObject;
			}

			return null;
		}

		protected override async UniTask PerformTask()
		{
			isPerformingTask = true;
			bool didCuff = ArrestPlayer(targetPlayer);

			float timeDelay = didCuff ? taskPerformDuration * 1.5f : taskPerformDuration;
			bool isCancelled = await UniTask
				.Delay(TimeSpan.FromSeconds(timeDelay), cancellationToken: cancellationTokenSource.Token)
				.SuppressCancellationThrow();

			isPerformingTask = false;

			if (isCancelled)
			{
				master.RemoveAddState(this, findSimpleTaskAi);
				return;
			}

			searchRadius = 2;
			var path = FindTarget(out targetCell, out targetMatrix);
			searchRadius = 5;

			if (path == null || path.Count == 0) master.RemoveAddState(this, findSimpleTaskAi);
			else DoTask();
		}

		protected override bool IsCurrentTaskValid()
		{
			return Vector3.Distance(targetCell.ToWorld(targetMatrix), master.Body.gameObject.AssumedWorldPosServer()) <= 1.5f;
		}

		private bool ArrestPlayer(PlayerScript player)
		{
			PlayerScript securitron = master.Body.LivingHealth.playerScript;
			Vector3 relative = player.AssumedWorldPos - securitron.AssumedWorldPos;
			if(player.RegisterPlayer.IsLayingDown && player.playerMove.IsCuffed == false)
			{
				if (restraints == null) restraints = FindCuffs();
				player.playerMove.Cuff(stunBatonItem.gameObject, player.gameObject);
				restraints = null;
				return true;
			}
			securitron.WeaponNetworkActions.ServerPerformMeleeAttack(player.gameObject, relative.To2().normalized, BodyPartType.Chest, LayerType.None, stunBatonItem, true);
			return false;
		}

		public override List<Vector3Int> FindTarget(out Vector3Int targetPosition, out Matrix targetMatrixLocal)
		{
			targetMatrixLocal = null;
			targetPosition = Vector3Int.zero;
			targetMatrix = null;
			targetPlayer = null;

			var targets = ComponentsTracker<LivingHealthMasterBase>.GetAllNearbyTypesToTarget(master.Body.gameObject, searchRadius, bypassInventories: false);
			if (targets == null) return null;

			targetMatrixLocal = master.Body.UniversalObjectPhysics.registerTile.Matrix;
			var currentPosition  = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(targetMatrixLocal);

			foreach(var player in targets)
			{
				bool isWanted = false;
				foreach (var record in CrewManifestManager.Instance.SecurityRecords)
				{
					if (record.characterSettings.Name.Equals(player.playerScript?.visibleName) == false) continue;
					if (record.Status != SecurityStatus.Criminal && record.Status != SecurityStatus.Arrest) continue;
					isWanted = true;
					break;
				}
				if(isWanted == false && IsEmagged == false) continue;
				if (IsEmagged && player.playerScript?.PlayerSync.IsCuffed == true) continue;

				var worldPos = player.gameObject.AssumedWorldPosServer();
				targetPosition = worldPos.ToLocalInt(targetMatrixLocal);

				var possiblePath = MobTraversal.GeneratePath(currentPosition, targetPosition, targetMatrixLocal, PathfinderType.AStar);
				if (possiblePath == null || possiblePath.Count == 0) break; //we don't need to keep looking through records if theres no path to this location

				targetPlayer = player.playerScript;
				targetMatrix = targetMatrixLocal;
				targetCell = targetPosition;

				return possiblePath;
			}
			return null;
		}
	}
}