using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IItemInOutMovedPlayer : IServerInventoryMove, IOnPlayerLeaveBody, IOnPlayerTransfer
{
	public Mind CurrentlyOn { get; set; }
	bool PreviousSetValid { get; set; }


	void IServerInventoryMove.OnInventoryMoveServer(InventoryMove info)
	{
		Mind ToPlayer = null;
		Mind FromPlayer = null;

		if (info.ToPlayer != null)
		{
			if (CurrentlyOn != null)
			{
				//removing and adding
				FromPlayer = CurrentlyOn;
				ToPlayer = info.ToRootPlayer.PlayerScript.mind;
				CurrentlyOn = info.ToRootPlayer.PlayerScript.mind;
			}

			if (CurrentlyOn == null)
			{
				//adding
				ToPlayer = info.ToRootPlayer.PlayerScript.mind;
				CurrentlyOn = info.ToRootPlayer.PlayerScript.mind;
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


	void IOnPlayerLeaveBody.OnPlayerLeaveBody(Mind mind)
	{
		InventoryInOutMovedPlayer(mind, null);
		CurrentlyOn = null;
	}

	void IOnPlayerTransfer.OnPlayerTransfer(Mind mind)
	{
		InventoryInOutMovedPlayer(CurrentlyOn, mind);
		CurrentlyOn = mind;
	}

	public bool IsValidSetup(Mind player);

	virtual void InventoryInOutMovedPlayer(Mind fromPlayer, Mind toPlayer)
	{
		Mind ShowForPlayer = null;
		Mind HideForPlayer = null;

		if (fromPlayer != null && toPlayer != fromPlayer)
		{
			PreviousSetValid = false; //Because different player now
			HideForPlayer = fromPlayer;
		}

		if (toPlayer != null && PreviousSetValid == false)
		{
			PreviousSetValid = IsValidSetup(toPlayer);

			if (PreviousSetValid)
			{
				ShowForPlayer = toPlayer;
			}
			else if (PreviousSetValid == false && toPlayer == fromPlayer)
			{
				HideForPlayer = fromPlayer;
			}
		}

		ChangingPlayer(HideForPlayer, ShowForPlayer);
	}


	void ChangingPlayer(Mind HideForPlayer, Mind ShowForPlayer);


}