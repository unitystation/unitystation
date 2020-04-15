using Mirror;
using UnityEngine;

public class ConveyorBelt : NetworkBehaviour
{
	public float AnimationSpeed = 1f;

	private float timeElapsedServer = 0;
	private float timeElapsedClient = 0;

	public ConveyorBeltSwitch ConnectedSwitch;

	public SpriteHandler spriteHandler;

	private RegisterTile registerTile;

	private int count = 0;

	private Vector3 position;

	private Matrix Matrix => registerTile.Matrix;

	public ConveyorDirection MappedDirection;

	private ConveyorDirection LastDirection;
	private ConveyorStatus LastStatus;

	[SyncVar(hook = nameof(SyncDirection))]
	private ConveyorDirection CurrentDirection;

	[SyncVar(hook = nameof(SyncStatus))]
	private ConveyorStatus CurrentStatus;

	private void SyncStatus(ConveyorStatus newStatus, ConveyorStatus oldStatus)
	{
		CurrentStatus = newStatus;
		//do your thing
		//all clients will be updated with this
	}

	private void SyncDirection(ConveyorDirection newDirection, ConveyorDirection oldDirection)
	{
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
			if (timeElapsedServer > AnimationSpeed)
			{
				DetectItems();
				ChangeDirection();

				//ServerChangeState(CurrentStatus, CurrentDirection);

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
		OnStart();
	}

	void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void OnStart()
	{
		registerTile = GetComponent<RegisterTile>();

		CurrentDirection = MappedDirection;

		if (ConnectedSwitch == null) return;

		LastStatus = CurrentStatus;

		if (ConnectedSwitch.CurrentState == 0)
		{
			CurrentStatus = ConveyorStatus.Backward;
		}
		else if (ConnectedSwitch.CurrentState == 1)
		{
			CurrentStatus = ConveyorStatus.Off;
		}
		else
		{
			CurrentStatus = ConveyorStatus.Forward;
		}

		if (!isServer) return;

		ServerChangeState(CurrentStatus, MappedDirection);
	}

	//void Start()
	//{
	//	registerTile = GetComponent<RegisterTile>();

	//	CurrentDirection = MappedDirection;
	//	CurrentStatus = MappedStatus;
	//	LastDirection = CurrentDirection;
	//	LastStatus = CurrentStatus;

	//	if (!isServer) return;
	//	ServerChangeState(MappedStatus, MappedDirection);
	//}

	private void ChangeDirection()
	{
		if (ConnectedSwitch == null) return;

		Logger.Log("Connected switch not null");

		LastStatus = CurrentStatus;

		if (ConnectedSwitch.CurrentState == 0)
		{
			CurrentStatus = ConveyorStatus.Backward;
		}
		else if (ConnectedSwitch.CurrentState == 1)
		{
			CurrentStatus = ConveyorStatus.Off;
		}
		else
		{
			CurrentStatus = ConveyorStatus.Forward;
		}

		Logger.Log("Current status "+ CurrentStatus);
	}

	private void ChangeAnimation()
	{
		//if (CurrentDirection != LastDirection || CurrentStatus != LastStatus)
		//{
			if (CurrentStatus == ConveyorStatus.Off)
			{
				spriteHandler.ChangeSprite(1);
			}
			else if (CurrentStatus == ConveyorStatus.Forward)
			{
				spriteHandler.ChangeSprite(2);
			}
			else
			{
				spriteHandler.ChangeSprite(0);
			}

			if (CurrentDirection == ConveyorDirection.Up)
			{
				position = Vector3.up;

				if (CurrentStatus == ConveyorStatus.Backward)
				{
					position *= -1;
				}

				spriteHandler.ChangeSpriteVariant(0);
			}
			else if (CurrentDirection == ConveyorDirection.Down)
			{
				position = Vector3.down;

				if (CurrentStatus == ConveyorStatus.Backward)
				{
					position *= -1;
				}

				spriteHandler.ChangeSpriteVariant(1);
			}
			else if (CurrentDirection == ConveyorDirection.Left)
			{
				position = Vector3.left;

				if (CurrentStatus == ConveyorStatus.Backward)
				{
					position *= -1;
				}

				spriteHandler.ChangeSpriteVariant(2);
			}
			else if (CurrentDirection == ConveyorDirection.Right)
			{
				position = Vector3.right;

				if (CurrentStatus == ConveyorStatus.Backward)
				{
					position *= -1;
				}

				spriteHandler.ChangeSpriteVariant(3);
			}
			else if (CurrentDirection == ConveyorDirection.LeftDown)
			{
				position = Vector3.down;

				if (CurrentStatus == ConveyorStatus.Backward)
				{
					position = Vector3.left;
				}

				spriteHandler.ChangeSpriteVariant(4);
			}
			else if (CurrentDirection == ConveyorDirection.UpLeft)
			{
				position = Vector3.left;

				if (CurrentStatus == ConveyorStatus.Backward)
				{
					position = Vector3.up;
				}

				spriteHandler.ChangeSpriteVariant(5);
			}
			else if (CurrentDirection == ConveyorDirection.DownRight)
			{
				position = Vector3.right;

				if (CurrentStatus == ConveyorStatus.Backward)
				{
					position = Vector3.down;
				}

				spriteHandler.ChangeSpriteVariant(6);
			}
			else if (CurrentDirection == ConveyorDirection.RightUp)
			{
				position = Vector3.up;

				if (CurrentStatus == ConveyorStatus.Backward)
				{
					position = Vector3.right;
				}

				spriteHandler.ChangeSpriteVariant(7);
			}
		//}

		if (position == null)
		{
			position = Vector3.up;
		}
	}

	private void DetectItems()
	{
		if (CurrentStatus == ConveyorStatus.Off) return;

		foreach (var player in Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, ObjectType.Player, true))
		{
			TransportPlayers(player);
		}

		foreach (var items in Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, ObjectType.Item, true))
		{
			Transport(items);
		}
	}

	[Server]
	public virtual void TransportPlayers(ObjectBehaviour player)
	{
		//teleports player to the front of the new gateway
		player.GetComponent<PlayerSync>().SetPosition(registerTile.WorldPosition + position);
	}

	[Server]
	public virtual void Transport(ObjectBehaviour toTransport)
	{
		toTransport.GetComponent<CustomNetTransform>().SetPosition(registerTile.WorldPosition + position);
	}

#if UNITY_EDITOR//no idea how to get this to work, so you can see the correct conveyor direction in editor

	private void Update()
	{
		if (Application.isEditor && !Application.isPlaying)
		{
			spriteHandler.gameObject.GetComponent<SpriteRenderer>().sprite = spriteHandler.Sprites[(int)CurrentDirection].Sprites[0];
		}
	}

#endif
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
	Down = 1,
	Left = 2,
	Right = 3,
	LeftDown = 4,
	UpLeft = 5,
	DownRight = 6,
	RightUp = 7
}
