using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using AddressableReferences;
using ScriptableObjects;
using Systems.Interaction;
using Systems.ObjectConnection;
using Doors;


namespace Objects.Wallmounts
{
	public class FireAlarm : SubscriptionController, IServerLifecycle, ICheckedInteractable<HandApply>, IMultitoolMasterable, ICheckedInteractable<AiActivate>
	{
		public List<FireLock> FireLockList = new List<FireLock>();
		private MetaDataNode metaNode;
		public bool activated = false;
		public float coolDownTime = 1.0f;
		public bool isInCooldown = false;

		[SyncVar(hook = nameof(SyncSprite))] private FireAlarmState stateSync;
		public SpriteHandler spriteHandler;
		public Sprite topLightSpriteNormal;
		public Sprite openEmptySprite;
		public Sprite openCabledSprite;
		public SpriteDataSO topLightSpriteAlert;

		public bool coverOpen;
		public bool hasCables = true;

		[SerializeField]
		private AddressableAudioSource FireAlarmSFX = null;

		public enum FireAlarmState
		{
			TopLightSpriteAlert,
			OpenEmptySprite,
			TopLightSpriteNormal,
			OpenCabledSprite
		};


		public void SendCloseAlerts()
		{
			if (!hasCables)
				return;
			if (!activated && !isInCooldown)
			{
				activated = true;
				stateSync = FireAlarmState.TopLightSpriteAlert;
				SoundManager.PlayNetworkedAtPos(FireAlarmSFX, metaNode.Position);
				StartCoroutine(SwitchCoolDown());
				foreach (var firelock in FireLockList)
				{
					if (firelock == null) continue;
					firelock.ReceiveAlert();
				}
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			var integrity = GetComponent<Integrity>();
			integrity.OnExposedEvent.AddListener(SendCloseAlerts);
			UpdateManager.Add(UpdateMe, 1);
			RegisterTile registerTile = GetComponent<RegisterTile>();
			MetaDataLayer metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
			var wallMount = GetComponent<WallmountBehavior>();
			var direction = wallMount.CalculateFacing().CutToInt();
			metaNode = metaDataLayer.Get(registerTile.LocalPositionServer + direction, false);
			foreach (var firelock in FireLockList)
			{
				firelock.fireAlarm = this;
			}
			if (!info.SpawnItems)
			{
				hasCables = false;
				coverOpen = true;
				stateSync = FireAlarmState.OpenEmptySprite;
			}

		}

		public void UpdateMe()
		{
			if (!activated)
			{
				if (metaNode.GasMix.Pressure < AtmosConstants.WARNING_LOW_PRESSURE || metaNode.GasMix.Pressure > AtmosConstants.WARNING_HIGH_PRESSURE)
				{
					SendCloseAlerts();
				}
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
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.Intent == Intent.Harm) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				if (coverOpen)
				{
					coverOpen = false;
					if (activated)
					{
						stateSync = FireAlarmState.TopLightSpriteAlert;
					}
					else
					{
						stateSync = FireAlarmState.TopLightSpriteNormal;
					}
				}
				else
				{
					coverOpen = true;
					if (hasCables)
					{
						stateSync = FireAlarmState.OpenCabledSprite;
					}
					else
					{
						stateSync = FireAlarmState.OpenEmptySprite;
					}
				}
				ToolUtils.ServerPlayToolSound(interaction);
				return;
			}
			if (coverOpen)
			{
				if (hasCables && Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wirecutter))
				{
					//cut out cables
					Chat.AddActionMsgToChat(interaction, $"You remove the cables.",
						$"{interaction.Performer.ExpensiveName()} removes the cables.");
					ToolUtils.ServerPlayToolSound(interaction);
					Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, SpawnDestination.At(gameObject), 5);
					stateSync = FireAlarmState.OpenEmptySprite;
					hasCables = false;
					activated = false;
					return;
				}

				if (!hasCables && Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Cable) &&
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
							stateSync = FireAlarmState.OpenCabledSprite;
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
				stateSync = FireAlarmState.TopLightSpriteNormal;
				StartCoroutine(SwitchCoolDown());
				foreach (var firelock in FireLockList)
				{
					if (firelock == null) continue;
					var controller = firelock.Controller;
					if (controller == null) continue;

					controller.TryOpen();
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

		public void SyncSprite(FireAlarmState stateOld, FireAlarmState stateNew)
		{
			stateSync = stateNew;
			if (stateNew == FireAlarmState.TopLightSpriteAlert)
			{

				spriteHandler.SetSpriteSO(topLightSpriteAlert);
			}
			else if (stateNew == FireAlarmState.OpenEmptySprite)
			{
				spriteHandler.SetSprite(openEmptySprite);
			}
			else if (stateNew == FireAlarmState.TopLightSpriteNormal)
			{
				spriteHandler.SetSprite(topLightSpriteNormal);
			}
			else if (stateNew == FireAlarmState.OpenCabledSprite)
			{
				spriteHandler.SetSprite(openCabledSprite);
			}
		}

		#region Editor

		public override IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects)
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
