using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DNAscanner : ClosetControl
{
	public LivingHealthBehaviour occupant;
	public Sprite closedWithOccupant;
	public string statusString;

	public override void OnStartServer()
	{
		statusString = "Ready to scan.";
	}

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

	protected override bool WillInteract(MouseDrop interaction, NetworkSide side)
	{
		return true;
	}

	protected override void ServerPerformInteraction(MouseDrop drop)
	{
		if(IsClosed)
		{
			return;
		}
		var objectBehaviour = drop.DroppedObject.GetComponent<ObjectBehaviour>();
		if(objectBehaviour)
		{
			IsClosed = true;
			StorePlayer(objectBehaviour);
			ChangeSprite();
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
