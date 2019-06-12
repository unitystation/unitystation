using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DNAscanner : ClosetControl
{
	public LivingHealthBehaviour occupant;
	public Sprite closedWithOccupant;


	public override void HandleItems()
	{
		base.HandleItems();
		if(heldPlayers.Count > 0)
		{
			var mob = heldPlayers[0];
			occupant = mob.GetComponent<LivingHealthBehaviour>();
		}
		else
		{
			occupant = null;
		}
	}

	public override void SyncSprite(ClosetStatus value)
	{
		if (value == ClosetStatus.Closed)
		{
			spriteRenderer.sprite = doorClosed;
		}
		else if(value == ClosetStatus.ClosedWithOccupant)
		{
			spriteRenderer.sprite = closedWithOccupant;
		}
		else
		{
			spriteRenderer.sprite = doorOpened;
		}
	}

}
