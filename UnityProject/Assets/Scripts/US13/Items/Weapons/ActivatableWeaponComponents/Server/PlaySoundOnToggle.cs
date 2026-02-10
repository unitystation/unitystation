using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Managers;
using Util;

namespace US13.Items.Weapons.ActivatableWeaponComponents.Server
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