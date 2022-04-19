using System.Collections;
using Mirror;
using NaughtyAttributes;
using Player;
using UnityEngine;

namespace Objects
{
	public class FlasherBase : NetworkBehaviour
	{
		[SerializeField] protected float flashRadius = 12f;
		[SerializeField, MinMaxSlider(3,20)] protected float flashStrength = 12f;
		[SerializeField] protected float flashCooldown = 24f;
		[SerializeField] protected ItemTrait sunglassesTrait;
		[SerializeField] protected bool stunsPlayers = true;
		[SyncVar] private bool onCooldown = false;
		public bool OnCooldown => onCooldown;
		private LayerMask pLayerMask;

		[Server]
		public void Flash()
		{
			if(onCooldown) return;
			var possibleTargets = Physics2D.OverlapCircleAll(gameObject.AssumedWorldPosServer(), flashRadius, pLayerMask);
			foreach (var target in possibleTargets)
			{
				if(gameObject == target.gameObject) continue;
				if(target.gameObject.TryGetComponent<PlayerFlashEffects>(out var flashEffector) == false) continue;
				if(target.gameObject.TryGetComponent<DynamicItemStorage>(out var playerStorage) == false) continue;
				foreach (var slots in playerStorage.ServerContents)
				{
					if(slots.Key != NamedSlot.eyes) continue;
					foreach (var onSlots in slots.Value)
					{
						if (onSlots.IsEmpty)
						{
							flashEffector.ServerSendMessageToClient(target.gameObject, flashStrength);
							continue;
						}
						if(onSlots.ItemAttributes.HasTrait(sunglassesTrait)) continue;
						flashEffector.ServerSendMessageToClient(target.gameObject, flashStrength);
						break;
					}
				}
			}
			StartCoroutine(Cooldown());
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