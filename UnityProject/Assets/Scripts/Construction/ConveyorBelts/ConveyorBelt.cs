using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[SelectionBase]
public class ConveyorBelt : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	[SerializeField] private SpriteHandler spriteHandler;

	private RegisterTile registerTile;
	private Vector3 position;
	private ConveyorBelt prevBelt;
	private ConveyorBelt nextBelt;

	public ConveyorBelt PrevBelt => prevBelt;
	public ConveyorBelt NextBelt => nextBelt;
	public bool ActivePrevBelt => ValidBelt(prevBelt);
	public bool ActiveNextBelt => ValidBelt(nextBelt);

	private Matrix Matrix => registerTile.Matrix;

	[SyncVar(hook = nameof(SyncDirection))]
	public ConveyorDirection CurrentDirection;

	private ConveyorStatus CurrentStatus = ConveyorStatus.Off;

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
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		OnStart();
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	//Avoiding the use of coroutines as this could run on many conveyor belts
	//This is needed so items and players are getting yeeted from being pushed by belts
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
		RefreshSprites();
	}

	public override void OnStartClient()
	{
		SyncDirection(CurrentDirection, CurrentDirection);
	}

	bool ValidBelt(ConveyorBelt belt)
	{
		if (belt == null || !belt.gameObject.activeInHierarchy) return false;
		if (Vector3.Distance(belt.transform.localPosition, transform.localPosition) > 1.5f) return false;
		return true;
	}

	public void SyncDirection(ConveyorDirection oldValue, ConveyorDirection newValue)
	{
		CurrentDirection = newValue;
		GetPositionOffset();
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

	public void MoveBelt()
	{
		RefreshSprites();
		if (isServer) DetectItems();
	}

	/// <summary>
	/// This method is called from the connected Conveyor Switch when its state changes.
	/// It will update the belt current state and its sprite.
	/// </summary>
	/// <param name="switchState"></param>
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

	private enum ConveyorStatus
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
		if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar))
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

//		else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench))//change direction
//		{
//			int count = (int)CurrentDirection + 1;
//
//			if (count > 7)
//			{
//				count = 0;
//			}
//
//			ToolUtils.ServerUseToolWithActionMessages(interaction, 1f,
//			"You start redirecting the conveyor belt...",
//			$"{interaction.Performer.ExpensiveName()} starts redirecting the conveyor belt...",
//			"You redirect the conveyor belt.",
//			$"{interaction.Performer.ExpensiveName()} redirects the conveyor belt.",
//			() =>
//			{
//				CurrentDirection = (ConveyorDirection)count;
//
//				UpdateDirection(CurrentDirection);
//
//				spriteHandler.ChangeSpriteVariant(count);
//			});
//		}
//		else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver))
//		{
//			ToolUtils.ServerUseToolWithActionMessages(interaction, 1f,
//			"You start inverting the conveyor belt...",
//			$"{interaction.Performer.ExpensiveName()} starts inverting the conveyor belt...",
//			"You invert the conveyor belt.",
//			$"{interaction.Performer.ExpensiveName()} invert the conveyor belt.",
//			() =>
//			{
//				switch (SyncInverted)
//				{
//					case true:
//						SyncInverted = false;
//						break;
//					case false:
//						SyncInverted = true;
//						break;
//				}
//			});
//		}
	}
}