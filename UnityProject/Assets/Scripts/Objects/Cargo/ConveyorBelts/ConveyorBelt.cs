using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ScriptableObjects;

namespace Construction.Conveyors
{
	[SelectionBase]
	[ExecuteInEditMode]
	public class ConveyorBelt : NetworkBehaviour, ICheckedInteractable<HandApply>, ISetMultitoolMaster
	{
		private readonly Vector2Int[] searchDirs =
		{
			new Vector2Int(-1, 0), new Vector2Int(0, 1),
			new Vector2Int(1, 0), new Vector2Int(0, -1)
		};

		[Tooltip("Set this conveyor belt's initial direction.")]
		[SerializeField]
		private ConveyorDirection CurrentDirection = default;

		[Tooltip("Set this conveyor belt's initial status.")]
		[SerializeField]
		private ConveyorStatus CurrentStatus = default;

		[SerializeField] private SpriteHandler spriteHandler = null;
		private RegisterTile registerTile;

		private Vector3 position;
		private Matrix Matrix => registerTile.Matrix;

		public ConveyorBeltSwitch AssignedSwitch { get; private set; }

		private Queue<PlayerSync> playerCache = new Queue<PlayerSync>();
		private Queue<CustomNetTransform> cntCache = new Queue<CustomNetTransform>();

		#region Lifecycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
		}

		private void OnValidate()
		{
			if (Application.isPlaying) return;
			RefreshSprites();
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			RefreshSprites();
		}

		#endregion Lifecycle

		#region Belt Operation

		[Server]
		public void MoveBelt()
		{
			DetectItems();
			MoveEntities();
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
		}

		private void MoveEntities()
		{
			while (playerCache.Count > 0)
			{
				TransportPlayer(playerCache.Dequeue());
			}

			while (cntCache.Count > 0)
			{
				Transport(cntCache.Dequeue());
			}
		}

		[Server]
		private void TransportPlayer(PlayerSync player)
		{
			//push player to the next tile
			player?.Push(position.To2Int());
		}

		[Server]
		private void Transport(CustomNetTransform item)
		{
			item?.Push(position.To2Int());
		}

		#endregion Belt Operation

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
						conveyorBelt.AssignedSwitch.AddConveyorBelt(new List<ConveyorBelt> { this });
						conveyorBelt.SetState(conveyorBelt.CurrentStatus);
						break;
					}
				}
			}
			RefreshSprites();
		}

		public void SetSwitchRef(ConveyorBeltSwitch switchRef)
		{
			AssignedSwitch = switchRef;
		}

		/// <summary>
		/// Updates the state of this conveyor based on the state of its assigned switch.
		/// </summary>
		[Server]
		public void UpdateState()
		{
			switch (AssignedSwitch.CurrentState)
			{
				case ConveyorBeltSwitch.SwitchState.Off:
					SetState(ConveyorStatus.Off);
					break;
				case ConveyorBeltSwitch.SwitchState.Forward:
					SetState(ConveyorStatus.Forward);
					break;
				case ConveyorBeltSwitch.SwitchState.Backward:
					SetState(ConveyorStatus.Backward);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void SetState(ConveyorStatus newStatus)
		{
			CurrentStatus = newStatus;
			GetPositionOffset();
			RefreshSprites();
		}

		private void RefreshSprites()
		{
			if (this == null) return;
			spriteHandler.ChangeSprite((int)CurrentStatus);
			var variant = (int)CurrentDirection;
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

		private void GetPositionOffset()
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

		public enum ConveyorStatus
		{
			Off = 0,
			Forward = 1,
			Backward = 2
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

		#region Interaction

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (!Validations.IsTarget(gameObject, interaction)) return false;

			// Deconstruct (crowbar) and change direction (screwdriver)
			return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar) ||
					Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver);
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
					DeconstructBelt);
			}

			else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver)) //change direction
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 1f,
					"You start redirecting the conveyor belt...",
					$"{interaction.Performer.ExpensiveName()} starts redirecting the conveyor belt...",
					"You redirect the conveyor belt.",
					$"{interaction.Performer.ExpensiveName()} redirects the conveyor belt.",
					ChangeDirection);
			}
		}

		private void DeconstructBelt()
		{
			Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, SpawnDestination.At(gameObject), 5);
			Despawn.ServerSingle(gameObject);
		}

		private void ChangeDirection()
		{
			int count = (int)CurrentDirection + 1;

			if (count > 11)
			{
				count = 0;
			}

			CurrentDirection = (ConveyorDirection)count;

			spriteHandler.ChangeSpriteVariant(count);
		}

		#endregion Interaction

		#region Multitool Interaction

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.Conveyor;
		public MultitoolConnectionType ConType => conType;

		private bool multiMaster = true;
		public bool MultiMaster => multiMaster;

		public void AddSlave(object SlaveObject)
		{
		}

		#endregion Multitool Interaction
	}
}
