using System.Collections.Generic;
using System.Linq;
using Actions.V2.Trackers;
using Core;
using Cysharp.Threading.Tasks;
using Doors;
using HealthV2;
using Logs;
using Objects.Doors;
using UnityEngine;

namespace Items.Implants.Organs.Arachnid
{
	[RequireComponent(typeof(BodyPartActionTracker))]
	[RequireComponent(typeof(BodyPart))]
	public class SpiderExoSkeletonAbilites : BodyPartFunctionality
	{
		[SerializeField] private float empStaggerTime = 0.5f;
		[SerializeField] private float crawlTime = 6f;
		[SerializeField] private float openDoorTime = 6f;
		[SerializeField] private float invisbilityTime = 60f;
		[SerializeField] private float checkForNearbyPlayersWhileInvisbleTime = 3f;

		private bool isStaggered = false;
		private int stayingNearPlayerStack = 0;

		private List<string> stackWarnings = new List<string>
		{
			"seems to have noticed you.",
			"took a glance in your direction.",
			"seems to feel your presence.",
		};

		public void Crawl(Vector2 worldMousePosition)
		{
			if (CanDoAction() == false) return;
			var player = RelatedPart.HealthMaster.playerScript;
			var matrix = player.gameObject.RegisterTile().Matrix;
			var objectsOnTile = matrix
				.Get<DoorMasterController>(worldMousePosition.To3Int().ToLocal(matrix).CutToInt(), CustomNetworkManager.IsServer).ToList();
			if (objectsOnTile.Count == 0 || objectsOnTile[0] == null)
			{
				Chat.AddExamineMsg(player.gameObject, "You can't crawl under anything here.");
				return;
			}
			var door = objectsOnTile[0];
			var timeToCrawl = door.isWindowedDoor ? crawlTime / 2f : crawlTime;
			StandardProgressActionConfig cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction, false, false, false);
			StandardProgressAction.Create(cfg, () =>
			{
				CrawlBehavior(door, matrix);
			}).ServerStartProgress(player.RegisterPlayer.LocalPosition.To3(), timeToCrawl, player.gameObject);
		}

		public void GoInvisible(Vector2 worldMousePosition)
		{
			if (CanDoAction() == false) return;
			UpdateManager.ThinkShot(GoVisible, invisbilityTime);
			LivingHealthMaster.playerScript.PlayerAlpha.Alpha = 0.05f;
			LivingHealthMaster.OnTakeDamageType += BecomeVisibleOnDamage;
			UpdateManager.Add(UnclockWhenNearPlayersForProlongedTime, checkForNearbyPlayersWhileInvisbleTime);
		}

		public void OpenDoors(Vector2 worldMousePosition)
		{
			if (CanDoAction() == false) return;
			var player = RelatedPart.HealthMaster.playerScript;
			var matrix = player.gameObject.RegisterTile().Matrix;
			var objectsOnTile = matrix
				.Get<DoorMasterController>(worldMousePosition.To3Int().ToLocal(matrix).CutToInt(), CustomNetworkManager.IsServer).ToList();
			if (objectsOnTile.Count == 0 || objectsOnTile[0] == null)
			{
				Chat.AddExamineMsg(player.gameObject, "You can't open anything here.");
				return;
			}
			var door = objectsOnTile[0];
			var timeToOpen = door.isWindowedDoor ? openDoorTime / 2f : openDoorTime;
			StandardProgressActionConfig cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction, false, false, false);
			StandardProgressAction.Create(cfg, () =>
			{
				OpenDoorsBehavior(door);
			}).ServerStartProgress(player.RegisterPlayer.LocalPosition.To3(), timeToOpen, player.gameObject);
			OpenDoorsBehavior(door);
		}

		public void GoVisible()
		{
			if (LivingHealthMaster.playerScript.PlayerAlpha.Alpha >= 0.95f) return;
			LivingHealthMaster.playerScript.PlayerAlpha.ServerToClientsResetAlphaToOne();
			Chat.AddActionMsgToChat(LivingHealthMaster.gameObject, $"A wild {LivingHealthMaster.playerScript.visibleName} appears!");
			LivingHealthMaster.OnTakeDamageType -= BecomeVisibleOnDamage;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UnclockWhenNearPlayersForProlongedTime);
		}

		private void BecomeVisibleOnDamage(DamageType type, GameObject damageSource, float damageAmount)
		{
			if (damageAmount <= 1f) return;
			GoVisible();
		}

		private void UnclockWhenNearPlayersForProlongedTime()
		{
			var mobs = ComponentsTracker<LivingHealthMasterBase>.GetAllNearbyTypesToTarget(LivingHealthMaster.gameObject, 2f,
				bypassInventories: false);
			if (mobs.Contains(LivingHealthMaster))
			{
				mobs.Remove(LivingHealthMaster);
			}
			if (mobs.Count > 0)
			{
				stayingNearPlayerStack++;
				if (DMMath.Prob(50))
				{
					Chat.AddExamineMsg(LivingHealthMaster.gameObject,
						$"{mobs.PickRandom().playerScript.visibleName} {stackWarnings.PickRandom()}");
				}
			}
			else
			{
				stayingNearPlayerStack--;
				if (stayingNearPlayerStack < 0)
				{
					stayingNearPlayerStack = 0;
				}
			}
			if (stayingNearPlayerStack >= 6)
			{
				GoVisible();
				stayingNearPlayerStack = 0;
			}

			switch (stayingNearPlayerStack)
			{
				case 0:
					LivingHealthMaster.playerScript.PlayerAlpha.Alpha = 0.01f;
					break;
				case 1:
					LivingHealthMaster.playerScript.PlayerAlpha.Alpha = 0.09f;
					break;
				case 2:
					LivingHealthMaster.playerScript.PlayerAlpha.Alpha = 0.15f;
					break;
				case 3:
					LivingHealthMaster.playerScript.PlayerAlpha.Alpha = 0.20f;
					break;
				case 4:
					LivingHealthMaster.playerScript.PlayerAlpha.Alpha = 0.30f;
					break;
				case 5:
					LivingHealthMaster.playerScript.PlayerAlpha.Alpha = 0.35f;
					break;
				default:
					GoVisible();
					break;
			}
		}

		private void CrawlBehavior(DoorMasterController door, Matrix matrix)
		{
			if (LivingHealthMaster == null || LivingHealthMaster.playerScript == null)
			{
				Loggy.Error("Looks like the player disappeared while they were trying to crawl under the door. Rip.");
				return;
			}

			var positionToTeleportTo = Vector3Int.zero;
			var locationsToCheck = door.RegisterTile.LocalPosition.GetNeighbors();
			foreach (var location in locationsToCheck)
			{
				if (location == LivingHealthMaster.playerScript.RegisterPlayer.LocalPosition) continue;
				if (matrix.IsPassableAtOneMatrixOneTile(location, CustomNetworkManager.IsServer,
					    context: LivingHealthMaster.playerScript.gameObject))
				{
					positionToTeleportTo = location;
				}
			}
			if (positionToTeleportTo == Vector3Int.zero)
			{
				Chat.AddExamineMsg(LivingHealthMaster.gameObject, "You can't crawl under this door. There's no space for you to fit, or you're too far away.");
				return;
			}
			LivingHealthMaster.playerScript.playerMove.AppearAtWorldPositionServer(positionToTeleportTo.ToWorldInt(matrix), true);
			Chat.AddExamineMsg(LivingHealthMaster.gameObject, "You squeeze yourself under the door.");
		}

		private void OpenDoorsBehavior(DoorMasterController door)
		{
			if (LivingHealthMaster == null || LivingHealthMaster.playerScript == null)
			{
				Loggy.Error("Looks like the player disappeared while they were trying to crawl under the door. Rip.");
				return;
			}

			door.Open();
			Chat.AddExamineMsg(LivingHealthMaster.gameObject, "You manage to pry open the door.");
		}

		private bool CanDoAction()
		{
			if (LivingHealthMaster == null || RelatedPart == null)
			{
				Loggy.Error("LivingHealthMaster or RelatedPart is null, cannot perform action.");
				return false;
			}
			if (LivingHealthMaster.IsDead)
			{
				Chat.AddExamineMsg(LivingHealthMaster.gameObject, "Cannot preform this action while dead.");
				return false;
			}
			if (isStaggered)
			{
				Chat.AddExamineMsg(LivingHealthMaster.gameObject, "Cannot preform this action while staggered due to an EMP.");
				return false;
			}
			return true;

		}

		public override void OnEmp(int strength)
		{
			base.OnEmp(strength);
			_ = Stagger();
		}

		private async UniTask Stagger()
		{
			isStaggered = true;
			await UniTask.WaitForSeconds(empStaggerTime);
			isStaggered = false;
		}
	}
}