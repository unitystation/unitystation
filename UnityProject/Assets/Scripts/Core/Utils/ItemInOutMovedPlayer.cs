using System.Collections;
using System.Collections.Generic;
using Player;
using UnityEngine;

public interface IItemInOutMovedPlayer : IServerInventoryMove
{
	//TODO Stuff that should trigger on a body that doesn't have a mind, For example clothing that sets you on fire, That should work even if you don't have a mind E.G you got cloned
	//Call it something like IItemBodyTransfer
	public RegisterPlayer CurrentlyOn { get; set; }
	bool PreviousSetValid { get; set; }


	void IServerInventoryMove.OnInventoryMoveServer(InventoryMove info)
	{
		RegisterPlayer ToPlayer = null;
		RegisterPlayer FromPlayer = null;

		if (info.ToRootPlayer != null)
		{
			if (CurrentlyOn != null)
			{
				//removing and adding
				FromPlayer = CurrentlyOn;
				ToPlayer = info.ToRootPlayer;
				CurrentlyOn = info.ToRootPlayer;
			}

			if (CurrentlyOn == null)
			{
				//adding
				ToPlayer = info.ToRootPlayer;
				CurrentlyOn = info.ToRootPlayer;
			}
		}
		else if (CurrentlyOn != null)
		{
			//Removing
			FromPlayer = CurrentlyOn;
			CurrentlyOn = null;
		}

		InventoryInOutMovedPlayer(FromPlayer, ToPlayer);
	}

	public bool IsValidSetup(RegisterPlayer player);

	virtual void InventoryInOutMovedPlayer(RegisterPlayer fromPlayer, RegisterPlayer toPlayer)
	{
		RegisterPlayer ShowForPlayer = null;
		RegisterPlayer HideForPlayer = null;

		if (fromPlayer != null)
		{
			if (toPlayer == null || toPlayer != fromPlayer)
			{
				PreviousSetValid = false;
				HideForPlayer = fromPlayer;
			}
		}


		bool Valid = IsValidSetup(toPlayer);

		if (PreviousSetValid != Valid || (fromPlayer != null && toPlayer != fromPlayer))
		{
			PreviousSetValid = Valid;
			if (PreviousSetValid)
			{
				ShowForPlayer = toPlayer;
			}
			else if (PreviousSetValid == false && toPlayer == fromPlayer)
			{
				HideForPlayer = fromPlayer;
			}
		}

		if (HideForPlayer == null && ShowForPlayer == null) return;
		ChangingPlayer(HideForPlayer, ShowForPlayer);
	}


	void ChangingPlayer(RegisterPlayer HideForPlayer, RegisterPlayer ShowForPlayer);
}