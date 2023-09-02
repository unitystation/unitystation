using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using AddressableReferences;
using ScriptableObjects;
using Systems.Interaction;
using Shared.Systems.ObjectConnection;
using CustomInspectors;
using Doors;
using Logs;


namespace Objects.Wallmounts
{
	public class FireAlarm : ImnterfaceMultitoolGUI, ISubscriptionController, IServerLifecycle,
		ICheckedInteractable<HandApply>, IMultitoolMasterable, ICheckedInteractable<AiActivate>
	{
		public List<FireLock> FireLockList = new List<FireLock>();
		private MetaDataNode metaNode;
		public bool activated = false;
		public float coolDownTime = 1.0f;
		public bool isInCooldown = false;

		public SpriteHandler baseSpriteHandler;
		public SpriteHandler topLightSpriteHandler;
		public SpriteHandler bottomLightSpriteHandler;

		public bool coverOpen;
		public bool hasCables = true;

		[SerializeField] private RegisterTile registerTile;
		[SerializeField] private Integrity integrity;
		[SerializeField] private AddressableAudioSource FireAlarmSFX = null;

		public enum FireAlarmState
		{
			TopLightSpriteAlert,
			OpenEmptySprite,
			TopLightSpriteNormal,
			OpenCabledSprite
		};

		private void Awake()
		{
			registerTile ??= GetComponent<RegisterTile>();
			integrity ??= GetComponent<Integrity>();
		}


		public void SendCloseAlerts()
		{
			if (hasCables == false) return;

			if (activated || isInCooldown) return;
			activated = true;

			SyncSprite(FireAlarmState.TopLightSpriteAlert);
			SoundManager.PlayNetworkedAtPos(FireAlarmSFX, registerTile.ObjectPhysics.Component.OfficialPosition);
			StartCoroutine(SwitchCoolDown());

			foreach (var firelock in FireLockList)
			{
				if (firelock == null) continue;
				firelock.ReceiveAlert();
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			integrity.OnExposedEvent.AddListener(SendCloseAlerts);
			MetaDataLayer metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
			var wallMount = GetComponent<WallmountBehavior>();
			var direction = wallMount.CalculateFacing().CutToInt();
			metaNode = metaDataLayer.Get(registerTile.LocalPositionServer + direction, false);

			foreach (var firelock in FireLockList)
			{
				if (firelock != null) firelock.fireAlarm = this;
				else Loggy.LogWarning("[Object/FireAlarm/OnSpawnServer] Firelock list on fire alarm has null entry.", Category.ItemSpawn);
			}

			if (info.SpawnItems == false)
			{
				hasCables = false;
				coverOpen = true;
				SyncSprite(FireAlarmState.OpenEmptySprite);
			}

			UpdateManager.Add(UpdateMe, 1);
		}

		public void UpdateMe()
		{
			if(activated) return;

			if(metaNode.Exists == false) return;

			if(metaNode.MetaDataSystem.SetUpDone == false) return;

			if (metaNode.GasMix.Pressure < AtmosConstants.WARNING_LOW_PRESSURE || metaNode.GasMix.Pressure > AtmosConstants.WARNING_HIGH_PRESSURE)
			{
				SendCloseAlerts();
			}
		}

		public void OnDestroy()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.Intent == Intent.Harm) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				if (coverOpen)
				{
					coverOpen = false;
					if (activated)
					{
						SyncSprite(FireAlarmState.TopLightSpriteAlert);
					}
					else
					{
						SyncSprite(FireAlarmState.TopLightSpriteNormal);
					}
				}
				else
				{
					coverOpen = true;
					if (hasCables)
					{
						SyncSprite(FireAlarmState.OpenCabledSprite);
					}
					else
					{
						SyncSprite(FireAlarmState.OpenEmptySprite);
					}
				}
				ToolUtils.ServerPlayToolSound(interaction);
				return;
			}
			if (coverOpen)
			{
				if (hasCables && Validations.HasItemTrait(interaction, CommonTraits.Instance.Wirecutter))
				{
					//cut out cables
					Chat.AddActionMsgToChat(interaction, $"You remove the cables.",
						$"{interaction.Performer.ExpensiveName()} removes the cables.");
					ToolUtils.ServerPlayToolSound(interaction);
					Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, SpawnDestination.At(gameObject), 5);
					SyncSprite(FireAlarmState.OpenEmptySprite);
					hasCables = false;
					activated = false;
					return;
				}

				if (!hasCables && Validations.HasItemTrait(interaction, CommonTraits.Instance.Cable) &&
					Validations.HasUsedAtLeast(interaction, 5))
				{
					//add 5 cables
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start adding cables to the frame...",
						$"{interaction.Performer.ExpensiveName()} starts adding cables to the frame...",
						"You add cables to the frame.",
						$"{interaction.Performer.ExpensiveName()} adds cables to the frame.",
						() =>
						{
							Inventory.ServerConsume(interaction.HandSlot, 5);
							hasCables = true;
							SyncSprite(FireAlarmState.OpenCabledSprite);
						});
				}
			}
			else
			{
				InternalToggleState();
			}
		}

		private void InternalToggleState()
		{
			if (activated && !isInCooldown)
			{
				activated = false;
				SyncSprite(FireAlarmState.TopLightSpriteNormal);
				StartCoroutine(SwitchCoolDown());
				foreach (var firelock in FireLockList)
				{
					if (firelock == null) continue;
					var controller = firelock.DoorMasterController;

					controller.TryOpen(null);
				}
			}
			else
			{
				SendCloseAlerts();
			}
		}

		private IEnumerator SwitchCoolDown()
		{
			isInCooldown = true;
			yield return WaitFor.Seconds(coolDownTime);
			isInCooldown = false;
		}

		public void SyncSprite(FireAlarmState stateNew)
		{
			switch (stateNew)
			{

				case FireAlarmState.TopLightSpriteAlert:
					baseSpriteHandler.ChangeSprite(0);
					topLightSpriteHandler.ChangeSprite(1);
					bottomLightSpriteHandler.ChangeSprite(2);
					break;
				case FireAlarmState.OpenEmptySprite:
					baseSpriteHandler.ChangeSprite(2);
					topLightSpriteHandler.PushClear();
					bottomLightSpriteHandler.PushClear();
					break;
				case FireAlarmState.TopLightSpriteNormal:
					baseSpriteHandler.ChangeSprite(0);
					topLightSpriteHandler.ChangeSprite(0);
					bottomLightSpriteHandler.ChangeSprite(0);
					break;
				case FireAlarmState.OpenCabledSprite:
					baseSpriteHandler.ChangeSprite(1);
					topLightSpriteHandler.PushClear();
					bottomLightSpriteHandler.PushClear();
					break;
			}
		}

		#region Editor

		public IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects)
		{
			var approvedObjects = new List<GameObject>();

			foreach (var potentialObject in potentialObjects)
			{
				var fireLock = potentialObject.GetComponent<FireLock>();
				if (fireLock == null) continue;
				AddFireLockFromScene(fireLock);
				approvedObjects.Add(potentialObject);
			}

			return approvedObjects;
		}

		private void AddFireLockFromScene(FireLock fireLock)
		{
			if (FireLockList.Contains(fireLock))
			{
				FireLockList.Remove(fireLock);
				fireLock.fireAlarm = null;
			}
			else
			{
				FireLockList.Add(fireLock);
				fireLock.fireAlarm = this;
			}
		}

		#endregion

		#region Ai Interaction

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			if (interaction.ClickType != AiActivate.ClickTypes.NormalClick) return false;

			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			InternalToggleState();
		}

		#endregion

		#region Multitool Interaction

		public MultitoolConnectionType ConType => MultitoolConnectionType.FireAlarm;
		public bool MultiMaster => true;
		int IMultitoolMasterable.MaxDistance => int.MaxValue;

		#endregion
	}
}
