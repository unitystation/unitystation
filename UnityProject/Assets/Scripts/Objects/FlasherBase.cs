using System;
using System.Collections;
using AddressableReferences;
using Mirror;
using NaughtyAttributes;
using Player;
using UnityEngine;

namespace Objects
{
	public class FlasherBase : NetworkBehaviour
	{
		[SerializeField] protected float flashRadius = 12f;
		[SerializeField] protected float flashTime = 12f;
		[SerializeField, ShowIf(nameof(stunsPlayers))] protected float stunExtraTime = 3f;
		[SerializeField] protected float flashCooldown = 24f;
		[SerializeField] protected ItemTrait sunglassesTrait;
		[SerializeField] protected AddressableAudioSource flashSound;
		[SerializeField] protected bool stunsPlayers = true;
		[SyncVar] private bool onCooldown = false;
		public bool OnCooldown => onCooldown;
		private LayerMask pLayerMask;
		private LayerMask layerMask;

		private void Start()
		{
			//(Max) : How would this work when we get to the mind rework? Will we force all objects that inherit from player to change their mask to "Players"? Or will we make a new one?
			pLayerMask = LayerMask.GetMask("Players");
			layerMask = LayerMask.GetMask("Door Closed"); //I copied this from ChatRelay.cs
		}

		[Server]
		public void FlashInRadius()
		{
			if (onCooldown) return;
			if (flashSound != null) _ = SoundManager.PlayNetworkedAtPosAsync(flashSound, gameObject.AssumedWorldPosServer());
			var possibleTargets = Physics2D.OverlapCircleAll(gameObject.AssumedWorldPosServer(), flashRadius, pLayerMask);
			foreach (var target in possibleTargets)
			{
				if(gameObject == target.gameObject) continue;
				if (MatrixManager.Linecast(gameObject.AssumedWorldPosServer(), LayerTypeSelection.Walls,
					    layerMask, target.gameObject.AssumedWorldPosServer()).ItHit) continue;
				if(target.gameObject.TryGetComponent<RegisterPlayer>(out var player) == false) continue; //If it's not a player, check next
				if(target.gameObject.TryGetComponent<PlayerFlashEffects>(out var flashEffector) == false) continue; //If the player doesn't have the ability to be flashed for whatever reason, check next
				if(target.gameObject.TryGetComponent<DynamicItemStorage>(out var playerStorage) == false) continue; //If the player has no storage for whatever reason... would this be a bug?
				foreach (var slots in playerStorage.ServerContents)
				{
					if(slots.Key != NamedSlot.eyes) continue;
					foreach (var onSlots in slots.Value)
					{
						if (onSlots.IsEmpty) //Nothing protecting the eye, flash immediately
						{
							TellClientThatTheyHaveBeenFlashed(flashEffector, player);
							continue;
						}
						if(onSlots.ItemAttributes.HasTrait(sunglassesTrait)) continue; //If the player is wearing protective glasses, check next.
						TellClientThatTheyHaveBeenFlashed(flashEffector, player);
						break;
					}
				}
			}
			StartCoroutine(Cooldown());
			Chat.AddLocalMsgToChat($"The {gameObject.ExpensiveName()} flashes everyone in its radius.", gameObject);
		}

		[Server]
		public void FlashTarget(GameObject target)
		{
			if (onCooldown) return;
			if (flashSound != null) _ = SoundManager.PlayNetworkedAtPosAsync(flashSound, gameObject.AssumedWorldPosServer());
			StartCoroutine(Cooldown());
			if (target.TryGetComponent<RegisterPlayer>(out var player) == false) return;
			if (target.gameObject.TryGetComponent<PlayerFlashEffects>(out var flashEffector) == false) return;
			if (target.gameObject.TryGetComponent<DynamicItemStorage>(out var playerStorage) == false) return;

			bool hasProtection = false;

			foreach (var slots in playerStorage.ServerContents)
			{
				if(slots.Key != NamedSlot.eyes && slots.Key != NamedSlot.mask) continue;
				foreach (var onSlots in slots.Value)
				{
					if(onSlots.IsEmpty) continue;
					if(onSlots.ItemAttributes.HasTrait(sunglassesTrait))
					{
						hasProtection = true;
						break;
					}
				}
			}
			if(hasProtection == false) TellClientThatTheyHaveBeenFlashed(flashEffector, player);
		}

		[Server]
		private void TellClientThatTheyHaveBeenFlashed(PlayerFlashEffects effects, RegisterPlayer player)
		{
			effects.ServerSendMessageToClient(player.gameObject, flashTime);
			if(stunsPlayers) player.ServerStun(flashTime + stunExtraTime);
		}

		[Server]
		private IEnumerator Cooldown()
		{
			onCooldown = true;
			yield return WaitFor.Seconds(flashCooldown);
			onCooldown = false;
		}
	}
}