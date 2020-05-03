using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[SelectionBase]
public class ConveyorBelt : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	[SerializeField] private SpriteHandler spriteHandler = null;

	private RegisterTile registerTile;
	private Vector3 position;

	public ConveyorBeltSwitch AssignedSwitch { get; private set; }

	private Matrix Matrix => registerTile.Matrix;

	[SyncVar(hook = nameof(SyncDirection))]
	public ConveyorDirection CurrentDirection;

	[SyncVar(hook = nameof(SyncStatus))]
	public ConveyorStatus CurrentStatus;

	Vector2Int[] searchDirs =
	{
		new Vector2Int(-1, 0), new Vector2Int(0, 1),
		new Vector2Int(1, 0), new Vector2Int(0, -1)
	};

	private bool processMoves = false;
	private float waitToMove = 0f;
	private Queue<PlayerSync> playerCache = new Queue<PlayerSync>();
	private Queue<CustomNetTransform> cntCache = new Queue<CustomNetTransform>();

	private void OnEnable()
	{
		OnStart();
	}

	private void OnDisable()
	{
		if(isServer) UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	//Avoiding the use of coroutines as this could run on many conveyor belts
	//This is needed so items and players aren't getting yeeted from being pushed by belts
	//each frame
	void UpdateMe()
	{
		if (!processMoves) return;

		waitToMove += Time.deltaTime;
		if (waitToMove > 0.1f)
		{
			processMoves = false;
			waitToMove = 0f;

			while (playerCache.Count > 0)
			{
				TransportPlayer(playerCache.Dequeue());
			}

			while (cntCache.Count > 0)
			{
				Transport(cntCache.Dequeue());
			}
		}
	}

	private void OnStart()
	{
		registerTile = GetComponent<RegisterTile>();
		RefreshSprites();
	}

	public override void OnStartServer()
	{
		CurrentStatus = ConveyorStatus.Off;
		RefreshSprites();
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	public override void OnStartClient()
	{
		RefreshSprites();
	}

	public void SyncDirection(ConveyorDirection oldValue, ConveyorDirection newValue)
	{
		CurrentDirection = newValue;
		GetPositionOffset();
		RefreshSprites();
	}

	void GetPositionOffset()
	{
		switch (CurrentStatus)
		{
			case ConveyorStatus.Forward:
				position = ConveyorDirections.directionsForward[CurrentDirection];
				break;
			case ConveyorStatus.Backward:
				position = ConveyorDirections.directionsBackward[CurrentDirection];
				break;
			default:
				position = Vector3.up;
				break;
		}
	}

	[Server]
	public void SetBeltFromBuildMenu(ConveyorDirection direction)
	{
		CurrentDirection = direction;
		//Discover any neighbours:
		for (int i = 0; i < searchDirs.Length; i++)
		{
			var conveyorBelt =
				registerTile.Matrix.GetFirst<ConveyorBelt>(registerTile.LocalPosition + searchDirs[i].To3Int(), true);

			if (conveyorBelt != null)
			{
				if (conveyorBelt.AssignedSwitch != null)
				{
					conveyorBelt.AssignedSwitch.AddConveyorBelt(new List<ConveyorBelt>{this});
					CurrentStatus = conveyorBelt.CurrentStatus;
					break;
				}
			}
		}
	}

	public void MoveBelt()
	{
		RefreshSprites();
		if (isServer) DetectItems();
	}

	public void SetSwitchRef(ConveyorBeltSwitch switchRef)
	{
		AssignedSwitch = switchRef;
	}

	/// <summary>
	/// This method is called from the connected Conveyor Switch when its state changes.
	/// It will update the belt current state and its sprite.
	/// </summary>
	/// <param name="switchState"></param>
	[Server]
	public void UpdateStatus(ConveyorBeltSwitch.State switchState)
	{
		switch (switchState)
		{
			case ConveyorBeltSwitch.State.Off:
				CurrentStatus = ConveyorStatus.Off;
				break;
			case ConveyorBeltSwitch.State.Forward:
				CurrentStatus = ConveyorStatus.Forward;
				break;
			case ConveyorBeltSwitch.State.Backward:
				CurrentStatus = ConveyorStatus.Backward;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(switchState), switchState, null);
		}

		GetPositionOffset();
		RefreshSprites();
	}

	private void SyncStatus(ConveyorStatus oldStatus, ConveyorStatus newStatus)
	{
		CurrentStatus = newStatus;
		GetPositionOffset();
		RefreshSprites();
	}

	private void RefreshSprites()
	{
		spriteHandler.ChangeSprite((int) CurrentStatus);
		var variant = (int) CurrentDirection;
		switch (variant)
		{
			case 8:
				variant = 4;
				break;
			case 9:
				variant = 5;
				break;
			case 10:
				variant = 6;
				break;
			case 11:
				variant = 7;
				break;
		}

		spriteHandler.ChangeSpriteVariant(variant);
	}

	private void DetectItems()
	{
		if (CurrentStatus == ConveyorStatus.Off) return;
		GetPositionOffset();
		if (!Matrix.IsPassableAt(registerTile.LocalPositionServer,
			Vector3Int.RoundToInt(registerTile.LocalPositionServer + position), true)) return;

		foreach (var player in Matrix.Get<PlayerSync>(registerTile.LocalPositionServer, ObjectType.Player, true))
		{
			playerCache.Enqueue(player);
		}

		foreach (var item in Matrix.Get<CustomNetTransform>(registerTile.LocalPositionServer, true))
		{
			if (item.gameObject == gameObject || item.PushPull == null || !item.PushPull.IsPushable) continue;

			cntCache.Enqueue(item);
		}

		waitToMove = 0f;
		processMoves = true;
	}

	[Server]
	public virtual void TransportPlayer(PlayerSync player)
	{
		//push player to the next tile
		player?.Push(position.To2Int());
	}

	[Server]
	public virtual void Transport(CustomNetTransform item)
	{
		item?.Push(position.To2Int());
	}

	public enum ConveyorStatus
	{
		Forward = 0,
		Off = 1,
		Backward = 2,
	}

	public enum ConveyorDirection
	{
		Up = 0,
		Down = 1,
		Left = 2,
		Right = 3,
		LeftDown = 4,
		LeftUp = 5,
		RightDown = 6,
		RightUp = 7,
		DownLeft = 8,
		UpLeft = 9,
		DownRight = 10,
		UpRight = 11
	}

	/* Construction stuff */
	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (!Validations.IsTarget(gameObject, interaction)) return false;

		return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar) ||
		       Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench) ||
		       Validations.HasUsedItemTrait(interaction,
			       CommonTraits.Instance.Screwdriver); // deconstruct(crowbar) and turn direction(wrench)
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench))
		{
			//deconsruct
			ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
				"You start deconstructing the conveyor belt...",
				$"{interaction.Performer.ExpensiveName()} starts deconstructing the conveyor belt...",
				"You deconstruct the conveyor belt.",
				$"{interaction.Performer.ExpensiveName()} deconstructs the conveyor belt.",
				() =>
				{
					Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, SpawnDestination.At(gameObject), 5);
					Despawn.ServerSingle(gameObject);
				});
		}

		else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver)) //change direction
		{
			int count = (int) CurrentDirection + 1;

			if (count > 11)
			{
				count = 0;
			}

			ToolUtils.ServerUseToolWithActionMessages(interaction, 1f,
				"You start redirecting the conveyor belt...",
				$"{interaction.Performer.ExpensiveName()} starts redirecting the conveyor belt...",
				"You redirect the conveyor belt.",
				$"{interaction.Performer.ExpensiveName()} redirects the conveyor belt.",
				() =>
				{
					CurrentDirection = (ConveyorDirection) count;

					spriteHandler.ChangeSpriteVariant(count);
				});
		}
	}
}