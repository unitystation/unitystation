using Mirror;
using UnityEngine;
using AddressableReferences;
using UnityEngine.Serialization;

namespace Objects
{
	public class ServiceBell : Pickupable
	{

		[SerializeField] private AddressableAudioSource ServiceBellSFX = null;

		public override void ServerPerformInteraction(HandApply interaction)
		{
			// yes, we can pick up the service bell!
			if (interaction.Intent == Intent.Grab)
			{
				base.ServerPerformInteraction(interaction);
				return;
			}
			SoundManager.PlayNetworkedAtPos(ServiceBellSFX, interaction.TargetObject.WorldPosServer());
		}
	}
}