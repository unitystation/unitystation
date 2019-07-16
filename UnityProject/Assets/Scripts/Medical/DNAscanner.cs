using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DNAscanner : ClosetControl, IAPCPowered
{
	public LivingHealthBehaviour occupant;
	public string statusString;
	[SyncVar(hook = nameof(SyncPowered))] public bool powered;
	public Sprite closedWithOccupant;
	public Sprite doorClosedPowerless;
	public Sprite doorOpenPowerless;

	public override void OnStartServer()
	{
		statusString = "Ready to scan.";
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		SyncPowered(powered);
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
		if (side == NetworkSide.Server && IsClosed)
			return false;
		if (!Validations.CanInteract(interaction.Performer, side))
			return false;
		if (!Validations.IsAdjacent(interaction.Performer, interaction.DroppedObject))
			return false;
		if (!Validations.IsAdjacent(interaction.Performer, gameObject))
			return false;
		if (interaction.Performer == interaction.DroppedObject)
			return false;
		return true;
	}

	protected override void ServerPerformInteraction(MouseDrop drop)
	{
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
		if (value == ClosetStatus.Open)
		{
			if (!powered)
			{
				spriteRenderer.sprite = doorOpenPowerless;
			}
			else
			{
				spriteRenderer.sprite = doorOpened;
			}
		}
		else if (!powered)
		{
			spriteRenderer.sprite = doorClosedPowerless;
		}
		else if (value == ClosetStatus.Closed)
		{
			spriteRenderer.sprite = doorClosed;
		}
		else if(value == ClosetStatus.ClosedWithOccupant)
		{
			spriteRenderer.sprite = closedWithOccupant;
		}

	}

	public void SyncPowered(bool value)
	{
		powered = value;
		SyncSprite(statusSync);
	}

	private void SetPowered(bool value)
	{
		powered = value;
		if(!powered)
		{
			if(IsLocked)
			{
				IsLocked = false;
			}
		}
	}

	public void PowerNetworkUpdate(float Voltage)
	{
	}

	public void StateUpdate(PowerStates State)
	{
		if (State == PowerStates.Off || State == PowerStates.LowVoltage)
		{
			SetPowered(false);
		}
		else
		{
			SetPowered(true);
		}
	}

}
