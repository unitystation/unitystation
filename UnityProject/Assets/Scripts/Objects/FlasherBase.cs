using System;
using System.Collections;
using AddressableReferences;
using HealthV2;
using Mirror;
using NaughtyAttributes;
using Player;
using UnityEngine;

namespace Objects
{
	public class FlasherBase : NetworkBehaviour
	{
		[SerializeField] protected float maximumDistance = 12f;
		[SerializeField] protected float flashRadius = 12f;
		[SerializeField] protected float flashTime = 12f;

		[SerializeField, Tooltip("Duration used for when players are standing further away from the flash distance")]
		protected float weakDuration  = 6f;

		[SerializeField, ShowIf(nameof(stunsPlayers))] protected float stunExtraTime = 3f;
		[SerializeField] protected float flashCooldown = 24f;
		[SerializeField] protected AddressableAudioSource flashSound;
		[SerializeField] protected bool stunsPlayers = true;
		[SyncVar] private bool onCooldown = false;
		public bool OnCooldown => onCooldown;
		private LayerMask pLayerMask;
		private LayerMask layerMask;

		private void Start()
		{
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

				var result = MatrixManager.Linecast(
					gameObject.AssumedWorldPosServer(), LayerTypeSelection.Walls, null,
					target.gameObject.AssumedWorldPosServer(), false);
				if (result.ItHit) continue;
				var duration = result.Distance < maximumDistance ? flashTime : weakDuration;

				if (stunsPlayers)
				{
					PerformFlash(target.gameObject, duration , duration + stunExtraTime);
				}
				else
				{
					PerformFlash(target.gameObject, duration , 0);
				}

			}
			StartCoroutine(Cooldown());
			Chat.AddActionMsgToChat(gameObject, $"The {gameObject.ExpensiveName()} flashes everyone in its radius.");
		}

		[Server]
		public void FlashTarget(GameObject target, float time, float stunnedtime, bool checkForProtectiveGear = true)
		{
			if (onCooldown) return;
			if (flashSound != null) _ = SoundManager.PlayNetworkedAtPosAsync(flashSound, gameObject.AssumedWorldPosServer());
			StartCoroutine(Cooldown());
			PerformFlash(target, time, stunnedtime, checkForProtectiveGear);
		}

		[Server]
		private void PerformFlash(GameObject target, float time, float stunnedTime, bool checkForProtectiveGear = true)
		{
			if (target.gameObject.TryGetComponentCustom<LivingHealthMasterBase>(out var livingHealthMasterBase) == false) return;
			if (stunnedTime > 0 && livingHealthMasterBase.TryFlash(time, checkForProtectiveGear))
			{
				livingHealthMasterBase.GetComponent<RegisterPlayer>()?.ServerStun(stunnedTime);
			}
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