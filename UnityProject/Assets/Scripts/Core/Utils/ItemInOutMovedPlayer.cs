using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IItemInOutMovedPlayer : IServerInventoryMove
{
	public Mind CurrentlyOn { get; set; }

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


	void InventoryInOutMovedPlayer(Mind fromPlayer, Mind toPlayer);
}