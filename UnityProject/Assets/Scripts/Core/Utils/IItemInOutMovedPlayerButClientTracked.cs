using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IItemInOutMovedPlayerButClientTracked : IItemInOutMovedPlayer, IOnPlayerRejoin
{
	void IOnPlayerRejoin.OnPlayerRejoin(Mind mind)
	{
		InventoryInOutMovedPlayer(null, mind);
		CurrentlyOn = mind;
	}
}
