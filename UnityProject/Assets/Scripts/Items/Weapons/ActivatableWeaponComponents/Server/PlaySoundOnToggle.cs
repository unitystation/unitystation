using AddressableReferences;
using UnityEngine;

namespace Weapons.ActivatableWeapons
{
	public class PlaySoundOnToggle : ServerActivatableWeaponComponent
	{
		public AddressableAudioSource activateSound;
		public AddressableAudioSource deactivateSound;

		public override void ServerActivateBehaviour(GameObject performer)
		{
			SoundManager.PlayNetworkedAtPos(activateSound, gameObject.AssumedWorldPosServer());
		}

		public override void ServerDeactivateBehaviour(GameObject performer)
		{
			SoundManager.PlayNetworkedAtPos(deactivateSound, gameObject.AssumedWorldPosServer());
		}
	}
}