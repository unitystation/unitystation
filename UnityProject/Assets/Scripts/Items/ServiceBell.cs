using UnityEngine;
using AddressableReferences;

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
			if (ShouldRing(interaction))
			{
				SoundManager.PlayNetworkedAtPos(RingSound, interaction.TargetObject.WorldPosServer());
				// ok, we ringed and we don't need to pick up the bell
				return;
			}

			base.ServerPerformInteraction(interaction);
		}

		public override void ClientPredictInteraction(HandApply interaction)
		{
			if (ShouldRing(interaction))
			{
				// we shouldn't pick up the item, so there is no need to predicate something
				return;
			}

			base.ClientPredictInteraction(interaction);
		}

		/// <summary>
		/// 	Should the bell ring when a player is trying to pick up the bell?
		/// </summary>
		/// <returns>False if a player's intent is set to Grab, true otherwise.</returns>
		private bool ShouldRing(Interaction interaction)
		{
			return interaction.Intent != Intent.Grab;
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
