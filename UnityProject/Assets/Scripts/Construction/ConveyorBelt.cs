using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Grpc.Core;
using Mirror;
using UnityEngine;

public class ConveyorBelt : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	[SerializeField]
	private float ConveyorBeltSpeed = 1f; //does not change animation speed! Only detection and teleport speed
	[SerializeField]
	private SpriteHandler spriteHandler;
	[SerializeField]
	private ConveyorDirection MappedDirection;

	[SyncVar]
	private bool SyncInverted = false;

	public bool Inverted = false;// this is read only during runtime, but sets SyncInverted on Start.

	private float timeElapsedServer = 0;
	private float timeElapsedClient = 0;
	private RegisterTile registerTile;
	private Vector3 position;
	private Matrix Matrix => registerTile.Matrix;

	[SyncVar(hook = nameof(SyncDirection))]
	private ConveyorDirection CurrentDirection;

	private ConveyorStatus CurrentStatus = ConveyorStatus.Off;

	protected virtual void UpdateMe()
	{
		if (isServer)
		{
			timeElapsedServer += Time.deltaTime;
			if (timeElapsedServer > ConveyorBeltSpeed)
			{
				Inverted = SyncInverted;
				ChangeAnimation();
				DetectItems();
				timeElapsedServer = 0;
			}
		}
		else
		{
			timeElapsedClient += Time.deltaTime;
			if (timeElapsedClient > ConveyorBeltSpeed)
			{
				Inverted = SyncInverted;
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

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}
	private void OnStart()
	{
		registerTile = GetComponent<RegisterTile>();
		CurrentDirection = MappedDirection;

		SyncInverted = Inverted;

		if (isServer)
		{
			UpdateDirection(CurrentDirection);
		}

		spriteHandler.ChangeSprite((int)CurrentStatus);
		spriteHandler.ChangeSpriteVariant((int)CurrentDirection);
	}

	public void SyncDirection(ConveyorDirection oldValue, ConveyorDirection newValue)
	{
		CurrentDirection = newValue;
	}

	[Server]
	public void UpdateDirection(ConveyorDirection newValue)
	{
		CurrentDirection = newValue;
	}

	/* Make this object a subordinate object. Make the switch boss around the behavior of this thing*/

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

		ChangeAnimation();
	}

	private void ChangeAnimation()
	{
		var tempStatus = CurrentStatus;

		if (SyncInverted && CurrentStatus == ConveyorStatus.Forward)
		{
			CurrentStatus = ConveyorStatus.Backward;
		}
		else if (SyncInverted && CurrentStatus == ConveyorStatus.Backward)
		{
			CurrentStatus = ConveyorStatus.Forward;
		}

		spriteHandler.ChangeSprite((int)CurrentStatus);
		spriteHandler.ChangeSpriteVariant((int)CurrentDirection);

		switch (CurrentStatus)
		{
			case ConveyorStatus.Forward:
				position = directionsForward[CurrentDirection];
				break;
			case ConveyorStatus.Backward:
				position = directionsBackward[CurrentDirection];
				break;
			default:
				position = Vector3.up;
				break;
		}

		CurrentStatus = tempStatus;
	}

	private void DetectItems()
	{
		if (CurrentStatus == ConveyorStatus.Off) return;

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

	private readonly Dictionary<ConveyorDirection, Vector3> directionsForward = new Dictionary<ConveyorDirection, Vector3>()
	{
		{ConveyorDirection.Up, Vector3.up},
		{ConveyorDirection.Right, Vector3.right},
		{ConveyorDirection.Down, Vector3.down},
		{ConveyorDirection.Left, Vector3.left},
		{ConveyorDirection.LeftDown, Vector3.left},
		{ConveyorDirection.UpLeft, Vector3.up},
		{ConveyorDirection.DownRight, Vector3.down},
		{ConveyorDirection.RightUp, Vector3.right}
	};

	private readonly Dictionary<ConveyorDirection, Vector3> directionsBackward = new Dictionary<ConveyorDirection, Vector3>()
	{
		{ConveyorDirection.Up, Vector3.down},
		{ConveyorDirection.Right, Vector3.left},
		{ConveyorDirection.Down, Vector3.up},
		{ConveyorDirection.Left, Vector3.right},
		{ConveyorDirection.LeftDown, Vector3.down},
		{ConveyorDirection.UpLeft, Vector3.left},
		{ConveyorDirection.DownRight, Vector3.right},
		{ConveyorDirection.RightUp, Vector3.up}
	};

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
		UpLeft = 5,
		DownRight = 6,
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
		else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench))//change direction
		{
			int count = (int)CurrentDirection + 1;

			if (count > 7)
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
				CurrentDirection = (ConveyorDirection)count;

				UpdateDirection(CurrentDirection);

				spriteHandler.ChangeSpriteVariant(count);
			});
		}
		else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver))
		{
			ToolUtils.ServerUseToolWithActionMessages(interaction, 1f,
			"You start inverting the conveyor belt...",
			$"{interaction.Performer.ExpensiveName()} starts inverting the conveyor belt...",
			"You invert the conveyor belt.",
			$"{interaction.Performer.ExpensiveName()} invert the conveyor belt.",
			() =>
			{
				switch (SyncInverted)
				{
					case true:
						SyncInverted = false;
						break;
					case false:
						SyncInverted = true;
						break;
				}
			});
		}
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


