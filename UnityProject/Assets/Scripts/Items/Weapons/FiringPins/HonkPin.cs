using Messages.Server.SoundMessages;

namespace Weapons
{
	class HonkPin : GenericPin
	{

		public override void ServerBehaviour(AimApply interaction)
		{
			AudioSourceParameters hornParameters = new AudioSourceParameters(pitch: UnityEngine.Random.Range(0.7f, 1.2f));
			SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.ClownHonk, interaction.Performer.AssumedWorldPosServer(),
			hornParameters, true, sourceObj: interaction.Performer);
		}

		public override void ClientBehaviour(AimApply interaction)
		{
			return;
		}
	}
}