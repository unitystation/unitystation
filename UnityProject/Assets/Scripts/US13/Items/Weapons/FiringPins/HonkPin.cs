using US13.Core.Addressables;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Managers;
using US13.Messages.Server.SoundMessages;
using Util;

namespace US13.Items.Weapons.FiringPins
{
	class HonkPin : PinBase
	{

		public override void ServerBehaviour(AimApply interaction, bool isSuicide)
		{
			AudioSourceParameters hornParameters = new AudioSourceParameters(pitch: UnityEngine.Random.Range(0.7f, 1.2f));
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.ClownHonk, interaction.Performer.AssumedWorldPosServer(),
			hornParameters, true, sourceObj: interaction.Performer);
		}
	}
}
