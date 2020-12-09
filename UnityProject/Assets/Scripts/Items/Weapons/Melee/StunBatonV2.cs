using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Pickupable))]
[RequireComponent(typeof(MeleeStun))]
public class StunBatonV2 : NetworkBehaviour, ICheckedInteractable<HandActivate>, IServerSpawn
{
	private SpriteHandler spriteHandler;

	private MeleeStun meleeStun;
	
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
		spriteHandler = GetComponentInChildren<SpriteHandler>();
		meleeStun = GetComponent<MeleeStun>();
	}

	// Calls TurnOff() when baton is spawned, see below.
	public void OnSpawnServer(SpawnInfo info)
	{
		TurnOff();
	}

	private void TurnOn()
	{
		meleeStun.enabled = true;
		batonState = BatonState.On;
		spriteHandler.ChangeSprite((int) BatonState.On);
	}

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

	//Activating the baton in-hand turns it off or off depending on its state.
	public void ServerPerformInteraction(HandActivate interaction)
	{
		SoundManager.PlayNetworkedAtPos(ToggleSound, interaction.Performer.AssumedWorldPosServer(), sourceObj: interaction.Performer);
		if (batonState == BatonState.Off)
		{
			TurnOn();
		}
		else
		{
			TurnOff();
		}
	}
}
