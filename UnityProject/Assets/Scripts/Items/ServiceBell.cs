using Mirror;

namespace Objects
{
	public class ServiceBell : Pickupable
	{
		public override void ServerPerformInteraction(HandApply interaction)
		{
			// yes, we can pick up the service bell!
			if (interaction.Intent == Intent.Grab)
			{
				base.ServerPerformInteraction(interaction);
				return;
			}
			SoundManager.PlayNetworkedAtPos("ServiceBell", interaction.TargetObject.WorldPosServer());
		}
	}
}