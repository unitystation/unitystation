using Mirror;
using UnityEngine;
using AddressableReferences;
using UnityEngine.Serialization;

namespace Objects
{
	public class ServiceBell : Pickupable, IServerSpawn
	{

		[Tooltip("The sound the bell makes when it rings.")]
		[SerializeField] private AddressableAudioSource RingSound = null;

		[Tooltip("The additional sound for when the bell spawns as a large bell.")]
		[SerializeField]
		private AddressableAudioSource BigBellRingSound = null;

		[SerializeField] private SpriteHandler BellSpriteRenderer;


		public override void ServerPerformInteraction(HandApply interaction)
		{
			// yes, we can pick up the service bell!
			if (interaction.Intent == Intent.Grab)
			{
				base.ServerPerformInteraction(interaction);
				return;
			}
			SoundManager.PlayNetworkedAtPos(RingSound, interaction.TargetObject.WorldPosServer());
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			// Roll for the big bell
			if (Random.value <= 0.005)
			{
				RingSound = BigBellRingSound;
				BellSpriteRenderer.ChangeSpriteVariant(1);
			}
		}
	}
}
