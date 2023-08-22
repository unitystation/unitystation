using AddressableReferences;
using NaughtyAttributes;
using Player;
using UnityEngine;

namespace Items.Weapons
{
	[RequireComponent(typeof(Grenade))]
	public class FlashbangGrenadeLogic : MonoBehaviour
	{
		[SerializeField] private bool despawnOnInvoke = true;
		[SerializeField] private float radius = 20f;
		[SerializeField] private LayerMask layersToUse;
		[SerializeField] private float maximumDistance = 20;
		[SerializeField] private float fullDuration = 12f;
		[SerializeField] private float weakDuration = 4f;
		[SerializeField] private float extraStunDuration = 4f;
		[SerializeField] private AddressableAudioSource flashSound;

		[Button("Flash!")]
		public void OnExpload()
		{
			var s = Physics2D.OverlapCircleAll(gameObject.AssumedWorldPosServer(), radius, layersToUse);
			foreach (Collider2D obj in s)
			{
				var result = MatrixManager.Linecast(
					gameObject.AssumedWorldPosServer(), LayerTypeSelection.Walls, null,
					obj.gameObject.AssumedWorldPosServer(), true);
				if (result.ItHit) continue;
				if (obj.gameObject.TryGetComponent<PlayerFlashEffects>(out var flashEffector) == false) continue;
				var duration = result.Distance < maximumDistance ? fullDuration : weakDuration;
				flashEffector.ServerSendMessageToClient(obj.gameObject, duration, true, true, duration + extraStunDuration);
			}

			if (despawnOnInvoke) _ = Despawn.ServerSingle(gameObject);
		}
	}
}