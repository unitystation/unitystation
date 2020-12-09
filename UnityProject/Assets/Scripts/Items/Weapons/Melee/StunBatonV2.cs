using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Pickupable))]
public class StunBatonV2 : NetworkBehaviour, ICheckedInteractable<HandActivate>, IServerSpawn
{
	public SpriteHandler spriteHandler;

    public MeleeStun meleeStun;
	
	// Sound played when turning this baton on/off.
	public AddressableAudioSource ToggleSound;

	///Both used as states for the baton and for the sub-catalogue in the sprite handler.
	private enum BatonState
	{
		Off,
		On,
		NoCell
	}

	private BatonState batonState;

    private void Awake()
    {
        meleeStun = GetComponent<MeleeStun>();
    }

	// Calls TurnOff() when baton is spawned, see below.
	public void OnSpawnServer(SpawnInfo info)
	{
		TurnOff();
	}

	// Enables melee stun component and sets baton state to on.
    private void TurnOn()
        {
        meleeStun.enabled = true;
		batonState = BatonState.On;
		spriteHandler.ChangeSprite((int) BatonState.On);
        }

	// Disables melee stun component and sets baton state to off.
    private void TurnOff()
    {
        //logic to turn the baton off.
        meleeStun.enabled = false;
		batonState = BatonState.Off;
		spriteHandler.ChangeSprite((int) BatonState.Off);
    }

	//For making sure the user is actually conscious.
	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	//Activating the baton in-hand turns it off, on, or does nothing depending on its state.
	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (batonState == BatonState.Off)
		{
			TurnOn();
			SoundManager.PlayNetworkedAtPos(ToggleSound, interaction.Performer.AssumedWorldPosServer(), sourceObj: interaction.Performer);
		}
		else
		{
			TurnOff();
			SoundManager.PlayNetworkedAtPos(ToggleSound, interaction.Performer.AssumedWorldPosServer(), sourceObj: interaction.Performer);
		}
	}
}
