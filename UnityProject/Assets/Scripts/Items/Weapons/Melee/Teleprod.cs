using AddressableReferences;
using Mirror;
using UnityEngine;

namespace Weapons
{
	/// <summary>
	/// Logic for toggling a teleprod on and off, same as the logic for StunBatonV2 but uses MeleeTeleport instead.
	/// Both of these could probably be compressed into one class, but they are seperate for now so its less finicky.
	/// </summary>
	[RequireComponent(typeof(Pickupable))]
	[RequireComponent(typeof(MeleeTeleport))]
	public class Teleprod : NetworkBehaviour, ICheckedInteractable<HandActivate>, IServerSpawn
	{
		private SpriteHandler spriteHandler;

		private MeleeTeleport meleeTeleport;

		// Sound played when turning this prod on/off.
		public AddressableAudioSource ToggleSound;

		///Both used as states for the teleprod and for the sub-catalogue in the sprite handler.
		private enum ProdState
		{
			Off,
			On,
			NoCell
		}

		private ProdState prodState;

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			meleeTeleport = GetComponent<MeleeTeleport>();
		}

		// Calls TurnOff() when teleprod is spawned, see below.
		public void OnSpawnServer(SpawnInfo info)
		{
			TurnOff();
		}

		private void TurnOn()
		{
			meleeTeleport.enabled = true;
			prodState = ProdState.On;
			spriteHandler.ChangeSprite((int)ProdState.On);
		}

		private void TurnOff()
		{
			//logic to turn the teleprod off.
			meleeTeleport.enabled = false;
			prodState = ProdState.Off;
			spriteHandler.ChangeSprite((int)ProdState.Off);
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
			if (prodState == ProdState.Off)
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
