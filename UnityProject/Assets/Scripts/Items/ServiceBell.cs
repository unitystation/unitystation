using Mirror;
using UnityEngine;
using AddressableReferences;
using UnityEngine.Serialization;

namespace Objects
{
	public class ServiceBell : Pickupable
	{

		[Tooltip("The sound the bell makes when it rings.")]
		[SerializeField] private AddressableAudioSource RingSound = null;

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
	}
}
