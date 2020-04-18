using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Grpc.Core;
using Mirror;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ConveyorBelt : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	[SerializeField]
	private SpriteHandler spriteHandler;

	private RegisterTile registerTile;
	private Vector3 position;
	private ConveyorBelt prevBelt;
	private ConveyorBelt nextBelt;
	private Matrix Matrix => registerTile.Matrix;

	[SyncVar(hook = nameof(SyncDirection))]
	private ConveyorDirection CurrentDirection;

	private ConveyorStatus CurrentStatus = ConveyorStatus.Off;

	Vector2Int[] searchDirs = {new Vector2Int(-1,0), new Vector2Int(0,1),
		new Vector2Int(1,0),new Vector2Int(0,-1)};

	private void OnEnable()
	{
		OnStart();
	}

	private void OnStart()
	{
		registerTile = GetComponent<RegisterTile>();
		spriteHandler.ChangeSprite((int)CurrentStatus);
		spriteHandler.ChangeSpriteVariant((int)CurrentDirection);
	}

	public override void OnStartServer()
	{
		CheckNeighbours();
	}

	public override void OnStartClient()
	{
		SyncDirection(CurrentDirection, CurrentDirection);
	}

	public void CheckNeighbours()
	{
		var nFound = 0;
		var inFound = false;
		var inPos = -1;
		var outPos = -1;
		for (int i = 0; i < searchDirs.Length; i++)
		{
			var conveyorBelt =
				registerTile.Matrix.GetFirst<ConveyorBelt>(registerTile.LocalPosition + searchDirs[i].To3Int(), true);

			//Default directions are Left To Right and Up to Down
			// [prev] ---> [next] first default direction
			//
			// [prev]
			// |    second default direction
			// V
			// [next]

			if (conveyorBelt != null && nFound < 2)
			{
				switch (i)
				{
					case 0: //First default IN pos:
						prevBelt = conveyorBelt;
						inFound = true;
						inPos = 0;
						break;
					case 1: //Second default IN pos:
						if (!inFound)
						{
							inPos = 1;
							prevBelt = conveyorBelt;
							inFound = true;
						}
						else
						{
							outPos = 1;
							nextBelt = conveyorBelt;
						}
						break;
					case 2: //Third default in:
						if (!inFound)
						{
							inPos = 2;
							prevBelt = conveyorBelt;
							inFound = true;
						}
						else
						{
							outPos = 2;
							nextBelt = conveyorBelt;
						}
						break;
					case 3:
						nextBelt = conveyorBelt;
						outPos = 3;
						break;
				}

				nFound++;
			}
		}

		DetermineDirection(inPos, outPos);
	}

	void DetermineDirection(int inPos, int outPos)
	{
		if (isServer)
		{
			CurrentDirection = ConveyorDirections.GetDirection(inPos, outPos);
			GetPositionOffset();
		}
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
		ChangeAnimation();
		if(isServer) DetectItems();
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

		ChangeAnimation();
	}

	private void ChangeAnimation()
	{
		spriteHandler.ChangeSprite((int)CurrentStatus);
		spriteHandler.ChangeSpriteVariant((int) CurrentDirection);
	}

	private void DetectItems()
	{
		if (CurrentStatus == ConveyorStatus.Off) return;
		GetPositionOffset();
		if (!Matrix.IsPassableAt(registerTile.LocalPositionServer, Vector3Int.RoundToInt(registerTile.LocalPositionServer + position), true)) return;

		foreach (var player in Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, ObjectType.Player, true))
		{
			TransportPlayers(player);
		}

		foreach (var items in Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, ObjectType.Item, true))
		{
			Transport(items);
		}

		foreach (var objects in Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, ObjectType.Object, true))
		{
			if (objects.gameObject != gameObject)//dont move itself, lol
			{
				Transport(objects);
			}
		}
	}

	[Server]
	public virtual void TransportPlayers(ObjectBehaviour player)
	{
		//teleports player to the next tile
		player.GetComponent<PlayerSync>().SetPosition(registerTile.WorldPosition + position);
	}

	[Server]
	public virtual void Transport(ObjectBehaviour toTransport)
	{
		toTransport.GetComponent<CustomNetTransform>().SetPosition(registerTile.WorldPosition + position);
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
		RightUp = 7
	}

	/* Construction stuff */
	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (!Validations.IsTarget(gameObject, interaction)) return false;

		return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar) || Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench) || Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver);// deconstruct(crowbar) and turn direction(wrench)
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


