using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ConveyorBelt : NetworkBehaviour
{
	public float AnimationSpeed = 1f;

	private float timeElapsedServer = 0;
	private float timeElapsedClient = 0;

	private GameObject ConnectedSwitch;

	public SpriteHandler spriteHandler;

	private RegisterTile registerTile;

	private Matrix Matrix => registerTile.Matrix;

	public ConveyorDirection MappedDirection;
	public ConveyorStatus MappedStatus;

	private ConveyorDirection LastDirection;
	private ConveyorStatus LastStatus;

	[SyncVar(hook = nameof(SyncStatus))]
	private ConveyorDirection CurrentDirection;

	[SyncVar(hook = nameof(SyncStatus))]
	private ConveyorStatus CurrentStatus;

	private void SyncStatus(ConveyorStatus newStatus, ConveyorStatus oldStatus, ConveyorDirection newDirection, ConveyorDirection oldDirection)
	{
		CurrentStatus = newStatus;
		CurrentDirection = newDirection;
		//do your thing
		//all clients will be updated with this
	}

	[Server]
	public void ServerChangeState(ConveyorStatus newStatus, ConveyorDirection newDirection)
	{
		CurrentStatus = newStatus;
		CurrentDirection = newDirection;
	}

	protected virtual void UpdateMe()
	{
		if (isServer)
		{
			timeElapsedServer += Time.deltaTime;
			if (timeElapsedServer > AnimationSpeed && CurrentStatus != ConveyorStatus.Off)
			{
				DetectItems();
				ChangeAnimation();
				timeElapsedServer = 0;
			}
		}
		else
		{
			timeElapsedClient += Time.deltaTime;
			if (timeElapsedClient > AnimationSpeed)
			{
				ChangeAnimation();
				timeElapsedClient = 0;
			}
		}
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}
	void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void Start()
	{
		if (!isServer) return;

		registerTile = GetComponent<RegisterTile>();

		ServerChangeState(MappedStatus, MappedDirection);
	}

	private void ChangeAnimation()
	{
		if (CurrentDirection == LastDirection || CurrentStatus == LastStatus) return;

		if (CurrentStatus == ConveyorStatus.Off)
		{
			spriteHandler.SetSprite(spriteHandler.Sprites[(int)CurrentDirection]);
		}
		else if (CurrentStatus == ConveyorStatus.Forward)
		{
			spriteHandler.ChangeSpriteVariant((int)CurrentDirection);
		}
		else
		{
			spriteHandler.ChangeSpriteVariant((int)CurrentDirection + 6);
		}
	}

	private void DetectItems()
	{
		foreach (var items in Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, ObjectType.Item, true))
		{
			Transport(items);
		}
	}

	[Server]
	public virtual void Transport(ObjectBehaviour toTransport)
	{
		//teleports player to the front of the new gateway
		//toTransport.GetComponent<PlayerSync>().SetPosition();
	}
}
public enum ConveyorStatus
{
	Forward = 0,
	Backward = 1,
	Off = 2
}

public enum ConveyorDirection
{
	Up = 0,
	Sideways = 1,
	TopToRight = 2,
	BottomToRight = 3,
	TopToLeft = 4,
	BottomToLeft = 5
}
