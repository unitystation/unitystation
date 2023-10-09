using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class HUDItemBase : NetworkBehaviour,  IItemInOutMovedPlayer, IClientSynchronisedEffect
{
	protected Pickupable pickupable;

	private void Awake()
	{
		pickupable =  GetComponent<Pickupable>();
	}

	#region InventoryMove

	public RegisterPlayer CurrentlyOn { get; set; }
	bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

	void IItemInOutMovedPlayer.ChangingPlayer(RegisterPlayer HideForPlayer, RegisterPlayer ShowForPlayer)
	{
		if (ShowForPlayer != null)
		{
			OnBodyID = ShowForPlayer.netId;
		}
		else
		{
			OnBodyID = NetId.Empty;
		}
	}

	#endregion

	private IClientSynchronisedEffect Preimplemented => (IClientSynchronisedEffect) this;

	[SyncVar(hook = nameof(SyncOnPlayer))] public uint OnBodyID;

	public uint OnPlayerID => OnBodyID;



	public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
	{
		OnBodyID = CurrentlyOn;
		Preimplemented.ImplementationSyncOnPlayer(PreviouslyOn, CurrentlyOn);
	}

	public void ApplyDefaultOrCurrentValues(bool Default)
	{
		ApplyEffects(Default ? false : true);
	}

	public virtual bool IsValidSetup(RegisterPlayer player)
	{
		if (player == null) return false;
		if (player != null && player.PlayerScript.RegisterPlayer == pickupable.ItemSlot.Player && pickupable.ItemSlot is {NamedSlot: NamedSlot.eyes}) // Checks if it's not null and checks if NamedSlot == NamedSlot.eyes
		{
			return true;
		}

		return false;
	}


	public virtual void ApplyEffects(bool State)
	{
		//hummm UI = Objects on player
		//How is it synchronised??
		//hud Script for each player?
		//Information on glasses nah



		//Ideal world
		//Observe static manager, why One object= observing, Serialise is once
		//Remove and add players when they put on the glasses from observing that object
		//Just need to add and remove information from that static thing
		//Wish it was per object but not too much I can do
		//the data class lives on the player but adds  and removes itself as needed
		//TODO Setup template for this


		//so, The script that actually track = on object that is related to it
		//so  traitor= script on mind , that specifies play ID  blah blah blah = as what to put the HUD on? What about brains idk it's not living it shouldn't have a HUD
		//Aileen What about no egg shiititttttttttttttttttttttttttttttttttttt,
		//hummm,
		//so Periodic update That updates the state??
		//Object HUD <-> State change
		//has gun Puts away gun , = remove Update
		//No egg HUD  no, Player spawns in with no egg ?!? what do?
		//let's say we don't support HUDs that don't have Code on the player In some way e.g must be tied to something at the players holding/Has/is associated with,
		//it can't be something ephemeral like a new player that has normal items,
		//has a hud lack of special item, Worst-case scenario if you need that just check if they're not on the list every Often and add the they don't have this thing To their HUD


		//Handling HUD removing and adding,
		//It affects so disable Client side, remove with interface, add with interface


		//so, synchronise var on each item unregistered and registered to the list of all the HUDs  managers in game( one for each player )
	}
}
