using AddressableReferences;
using NaughtyAttributes;
using Objects;
using Player;
using UnityEngine;

namespace Items.Weapons
{
	[RequireComponent(typeof(Grenade))]
	public class FlashbangGrenadeLogic : FlasherBase
	{
		[SerializeField] private bool despawnOnInvoke = true;

		[Button("Flash!")]
		public void OnExpload()
		{
			FlashInRadius();

			if (flashSound != null) SoundManager.PlayNetworkedAtPos(flashSound, gameObject.AssumedWorldPosServer());
			if (despawnOnInvoke) _ = Despawn.ServerSingle(gameObject);
		}
	}
}