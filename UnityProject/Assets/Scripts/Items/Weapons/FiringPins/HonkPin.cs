using Messages.Server.SoundMessages;

namespace Weapons
{
	class HonkPin : PinBase
	{

		public override void ServerBehaviour(AimApply interaction, bool isSuicide)
		{
			AudioSourceParameters hornParameters = new AudioSourceParameters(pitch: UnityEngine.Random.Range(0.7f, 1.2f));
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.ClownHonk, interaction.Performer.AssumedWorldPosServer(),
			hornParameters, true, sourceObj: interaction.Performer);
		}

		public override void ClientBehaviour(AimApply interaction, bool isSuicide)
		{
		}
	}
}
