using NaughtyAttributes;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers;
using US13.Objects;
using US13.Systems.Explosions;
using Util;

namespace US13.Items.Weapons
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
			SparkUtil.TrySpark(gameObject);
			if (despawnOnInvoke) _ = Despawn.ServerSingle(gameObject);
		}
	}
}