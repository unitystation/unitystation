using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class DNAscanner : ClosetControl, ICheckedInteractable<MouseDrop>, IAPCPowered
{
	public LivingHealthBehaviour occupant;
	public string statusString;

	public bool Powered => powered;
	[SyncVar(hook = nameof(SyncPowered))] private bool powered;
	//tracks whether we've recieved our first power update from electriciy.
	//allows us to avoid  syncing power when it is unchanged
	private bool powerInit;

	public Sprite closedWithOccupant;
	public Sprite doorClosedPowerless;
	public Sprite doorOpenPowerless;


	public SpriteHandler spriteHandler;

	public override void OnStartServer()
	{
		base.OnStartServer();
		statusString = "Ready to scan.";
		SyncPowered(powered);
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		SyncPowered(powered);
	}

	protected override void ServerHandleContentsOnStatusChange()
	{
		base.ServerHandleContentsOnStatusChange();
		if(ServerHeldPlayers.Any())
		{
			var mob = ServerHeldPlayers.First();
			occupant = mob.GetComponent<LivingHealthBehaviour>();
		}
		else
		{
			occupant = null;
		}
	}

	public bool WillInteract(MouseDrop interaction, NetworkSide side)
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

	public void ServerPerformInteraction(MouseDrop drop)
	{
		var objectBehaviour = drop.DroppedObject.GetComponent<ObjectBehaviour>();
		if(objectBehaviour)
		{
			ServerStorePlayer(objectBehaviour);
			ServerToggleClosed(true);
		}
	}

	protected override void UpdateSpritesOnStatusChange()
	{
		//Logger.Log("TTTTTTTTTTTTT" + value.ToString());
		if (ClosetStatus == ClosetStatus.Open)
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
		else if (ClosetStatus == ClosetStatus.Closed)
		{
			spriteHandler.ChangeSprite(0);
		}
		else if(ClosetStatus == ClosetStatus.ClosedWithOccupant)
		{
			spriteHandler.ChangeSprite(2);
		}
	}

	private void SyncPowered(bool value)
	{
		//does nothing if power is unchanged and
		//we've already init'd
		if (powered == value && powerInit) return;

		powered = value;
		if(!powered)
		{
			if(IsLocked)
			{
				ServerToggleLocked(false);
			}
		}
		UpdateSpritesOnStatusChange();
	}

	public void PowerNetworkUpdate(float Voltage)
	{
	}

	public void StateUpdate(PowerStates State)
	{
		if (State == PowerStates.Off || State == PowerStates.LowVoltage)
		{
			SyncPowered(false);
		}
		else
		{
			SyncPowered(true);
		}

		if (!powerInit)
		{
			powerInit = true;
		}
	}

}
