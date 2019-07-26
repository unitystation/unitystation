using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DNAscanner : ClosetControl, IAPCPowered
{
	public LivingHealthBehaviour occupant;
	public string statusString;
	[SyncVar(hook = nameof(SyncPowered))] public bool powered;
	public SpriteHandler spriteHandler;

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
				spriteHandler.ChangeSprite(5);
			}
			else
			{
				spriteHandler.ChangeSprite(3);
			}
		}
		else if (!powered)
		{
			spriteHandler.ChangeSprite(6);
		}
		else if (value == ClosetStatus.Closed)
		{
			spriteHandler.ChangeSprite(0);
		}
		else if(value == ClosetStatus.ClosedWithOccupant)
		{
			spriteHandler.ChangeSprite(2);
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
