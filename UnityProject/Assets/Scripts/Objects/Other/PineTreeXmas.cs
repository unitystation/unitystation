using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AddressableReferences;
using Audio.Containers;
using Managers;
using Mirror;
using ScriptableObjects.TimedGameEvents;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Objects.Other
{
	public class PineTreeXmas : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerSpawn
	{
		[SerializeField] private SpriteDataSO xmasSpriteSO;
		[SerializeField] private TimedGameEventSO eventData;
		[SerializeField] private GameObject giftObject;
		[SerializeField] private AddressableAudioSource ambientReminder;

		private bool hasClicked = false;

		private List<string> giftedPlayers = new List<string>();
		private bool canPickUpGifts;

		private SpriteHandler spriteHandler;

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (TimedEventsManager.Instance.ActiveEvents.Contains(eventData))
			{
				canPickUpGifts = true;
				spriteHandler.SetSpriteSO(xmasSpriteSO);
				UpdateManager.Add(PlayEasterEgg, 60);
			}
			else if (eventData.deleteWhenNotTime)
			{
				_ = Despawn.ServerSingle(gameObject);
			}
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PlayEasterEgg);
		}

		[ContextMenu("Play the reminder of our finite time")]
		private void PlayEasterEgg()
		{
			if(gameObject == null) return;

			if (DMMath.Prob(10))
			{
				_ = SoundManager.PlayNetworkedAtPosAsync(ambientReminder, gameObject.WorldPosServer());
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (canPickUpGifts == false) return false;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.HandSlot.IsOccupied) return false;

			if (hasClicked)
			{
				if (interaction.IsHighlight == false)
				{
					Chat.AddExamineMsg(interaction.Performer, "You already received a gift!", side);
				}

				return false;
			}

			if (side == NetworkSide.Client && interaction.IsHighlight == false)
			{
				hasClicked = true;
			}
			else
			{
				if(giftedPlayers.Contains(interaction.PerformerPlayerScript.connectedPlayer.UserId)) return false;
			}

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{


			Inventory.ServerSpawnPrefab(giftObject, interaction.HandSlot, ReplacementStrategy.DropOther);
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You pick up a gift with your name on it.",
				$"{interaction.PerformerPlayerScript.visibleName} picks up a gift with {interaction.PerformerPlayerScript.characterSettings.TheirPronoun(interaction.PerformerPlayerScript)} name on it.");
			giftedPlayers.Add(interaction.PerformerPlayerScript.connectedPlayer.UserId);
		}
	}
}

