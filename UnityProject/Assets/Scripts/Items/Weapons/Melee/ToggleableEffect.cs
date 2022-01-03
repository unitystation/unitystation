using AddressableReferences;
using Mirror;
using UnityEngine;

namespace Weapons
{
	/// <summary>
	/// Logic for toggling a weapon such as a stun baton or teleprod on or off
	/// </summary>
	[RequireComponent(typeof(Pickupable))]
	[RequireComponent(typeof(MeleeEffect))]
	public class ToggleableEffect : NetworkBehaviour, ICheckedInteractable<HandActivate>, IServerSpawn
	{
		private SpriteHandler spriteHandler;

		private MeleeEffect meleeEffect;

		// Sound played when turning this item on/off.
		public AddressableAudioSource ToggleSound;

		///Both used as states for the item and for the sub-catalogue in the sprite handler.
		private enum WeaponState
		{
			Off,
			On,
			NoCell
		}

		private WeaponState weaponState;

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			meleeEffect = GetComponent<MeleeEffect>();
		}

		// Calls TurnOff() when item is spawned, see below.
		public void OnSpawnServer(SpawnInfo info)
		{
			TurnOff();
		}

		private void TurnOn()
		{
			meleeEffect.enabled = true;
			weaponState = WeaponState.On;
			spriteHandler.ChangeSprite((int)WeaponState.On);
		}

		private void TurnOff()
		{
			//logic to turn the teleprod off.
			meleeEffect.enabled = false;
			weaponState = WeaponState.Off;
			spriteHandler.ChangeSprite((int)WeaponState.Off);
		}

		//For making sure the user is actually conscious.
		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		//Activating the teleprod in-hand turns it off or off depending on its state.
		public void ServerPerformInteraction(HandActivate interaction)
		{
			SoundManager.PlayNetworkedAtPos(ToggleSound, interaction.Performer.AssumedWorldPosServer(), sourceObj: interaction.Performer);
			if (weaponState == WeaponState.Off)
			{
				TurnOn();
			}
			else
			{
				TurnOff();
			}
		}
	}
}
