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
	private float AnimationSpeed = 1f;
	[SerializeField]
	private SpriteHandler spriteHandler;
	[SerializeField]
	private ConveyorDirection MappedDirection;

	private float timeElapsedServer = 0;
	private float timeElapsedClient = 0;
	private RegisterTile registerTile;
	private Vector3 position;
	private Matrix Matrix => registerTile.Matrix;
	private ConveyorDirection CurrentDirection;
	private ConveyorStatus CurrentStatus = ConveyorStatus.Off;

	protected virtual void UpdateMe()
	{
		if (isServer)
		{
			timeElapsedServer += Time.deltaTime;
			if (timeElapsedServer > AnimationSpeed)
			{
				ChangeAnimation();
				DetectItems();
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

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}
	private void OnStart()
	{
		registerTile = GetComponent<RegisterTile>();
		CurrentDirection = MappedDirection;
		spriteHandler.ChangeSprite((int)CurrentStatus);
		spriteHandler.ChangeSpriteVariant((int)CurrentDirection);
	}

	/* Make this object a subordinate object. Make the switch boss around the behavior of this thing*/

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

		ChangeAnimation();
	}

	private void ChangeAnimation()
	{
		spriteHandler.ChangeSprite((int) CurrentStatus);

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

		spriteHandler.ChangeSpriteVariant((int) CurrentDirection);
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

	private enum ConveyorDirection
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

		return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar) || Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench);// deconstruct(crowbar) and turn direction(wrench)
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

			CurrentDirection = (ConveyorDirection)count;

			spriteHandler.ChangeSpriteVariant(count);
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


