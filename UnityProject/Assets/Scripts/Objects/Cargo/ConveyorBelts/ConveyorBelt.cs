using System;
using System.Collections.Generic;
using Core;
using UnityEngine;
using Mirror;
using ScriptableObjects;
using SecureStuff;
using Shared.Systems.ObjectConnection;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Construction.Conveyors
{
	[SelectionBase]
	[ExecuteInEditMode]
	public class ConveyorBelt : MonoBehaviour, ICheckedInteractable<HandApply>, IMultitoolSlaveable
	{
		private readonly Vector2Int[] searchDirs =
		{
			new Vector2Int(-1, 0), new Vector2Int(0, 1),
			new Vector2Int(1, 0), new Vector2Int(0, -1)
		};

		[Tooltip("Set this conveyor belt's initial direction.")]
		[SerializeField]
		public ConveyorDirection CurrentDirection = default;

		[Tooltip("Set this conveyor belt's initial status.")]
		[SerializeField]
		public ConveyorStatus CurrentStatus = default;

		[SerializeField] private SpriteHandler spriteHandler = null;
		private RegisterTile registerTile;

		private Vector3 PushDirectionPosition;
		private Matrix Matrix => registerTile.Matrix;

		public ConveyorBeltSwitch AssignedSwitch { get; private set; }

		private Queue<UniversalObjectPhysics> objectPhyicsCache = new Queue<UniversalObjectPhysics>();

		private Matrix _lastUpdateMatrix;
		private Vector3Int _lastLocalUpdatePosition;
		private float _LastSpeed = 0;
		[field: SerializeField] public bool CanRelink { get; set; } = true;
		[field: SerializeField] public bool IgnoreMaxDistanceMapper { get; set; } = false;

		private List<UniversalObjectPhysics> GCfreeList = new List<UniversalObjectPhysics>();

		#region Lifecycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
		}

		private void OnValidate()
		{
			if (Application.isPlaying) return;
#if UNITY_EDITOR
			if (Selection.activeGameObject != this.gameObject) return;
#endif
#if UNITY_EDITOR
			EditorRefreshSprites();
#endif

		}

		#endregion Lifecycle

		#region Belt Operation

		[Server]
		public void MoveBelt(float ConveyorBeltSpeed)
		{
			DetectItems();
			MoveEntities(ConveyorBeltSpeed);
		}


		private void DetectItems()
		{
			if (CurrentStatus == ConveyorStatus.Off) return;

			GetPositionOffset();
			if (!Matrix.IsPassableAtOneMatrix(registerTile.LocalPositionServer,
				Vector3Int.RoundToInt(registerTile.LocalPositionServer + PushDirectionPosition), true)) return;
			GCfreeList.Clear();
			foreach (var item in Matrix.GetNoGC(registerTile.LocalPositionServer, true, GCfreeList))
			{
				if (item.gameObject == gameObject || item.IsNotPushable || item.Intangible || item.IsMoving)  continue;
				objectPhyicsCache.Enqueue(item);
			}
		}

		private void MoveEntities(float ConveyorBeltSpeed)
		{
			while (objectPhyicsCache.Count > 0)
			{
				Transport(objectPhyicsCache.Dequeue(), ConveyorBeltSpeed);
			}
		}


		[Server]
		private void Transport(UniversalObjectPhysics item, float ConveyorBeltSpeed)
		{
			if (item == null) return;
			if (item.NewtonianMovement.magnitude > ConveyorBeltSpeed) return;
			item.Pushing.Clear();

			item.TryTilePush(PushDirectionPosition.RoundTo2Int(), null , ConveyorBeltSpeed);
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
						conveyorBelt.AssignedSwitch.AddConveyorBelt(this);
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
			UpdateState();
		}

		/// <summary>
		/// Updates the state of this conveyor based on the state of its assigned switch.
		/// </summary>
		[Server]
		public void UpdateState()
		{
			if (AssignedSwitch == null)
			{
				SetState(ConveyorStatus.Off);
				return;
			}
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

		private void EditorRefreshSprites()
		{
			if (Application.isPlaying) return;
			if (this == null) return;
#if UNITY_EDITOR
			if (Selection.activeGameObject != this.gameObject) return;
#endif
			spriteHandler.SetCatalogueIndexSprite((int)CurrentStatus);
			var variant = (int)CurrentDirection;

			spriteHandler.SetSpriteVariant(variant);
		}

		private void RefreshSprites()
		{
			if (this == null) return;
			spriteHandler.SetCatalogueIndexSprite((int)CurrentStatus);
			var variant = (int)CurrentDirection;

			spriteHandler.SetSpriteVariant(variant);
		}

		private void GetPositionOffset()
		{
			switch (CurrentStatus)
			{
				case ConveyorStatus.Forward:
					PushDirectionPosition = ConveyorDirections.directionsForward[CurrentDirection];
					break;
				case ConveyorStatus.Backward:
					PushDirectionPosition = ConveyorDirections.directionsBackward[CurrentDirection];
					break;
				default:
					PushDirectionPosition = Vector3.up;
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
			Down = 0,
			Up = 1,
			Right = 2,
			Left = 3,
			LeftDown = 4,
			UpLeft = 5,
			DownRight = 6,
			RightUp = 7,
			DownLeft = 8,
			LeftUp = 9,
			RightDown = 10,
			UpRight = 11
		}

		#region Interaction

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (!Validations.IsTarget(gameObject, interaction)) return false;

			// Deconstruct (crowbar) and change direction (screwdriver)
			return Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar) ||
					Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench))
			{
				//deconsruct
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start deconstructing the conveyor belt...",
					$"{interaction.Performer.ExpensiveName()} starts deconstructing the conveyor belt...",
					"You deconstruct the conveyor belt.",
					$"{interaction.Performer.ExpensiveName()} deconstructs the conveyor belt.",
					DeconstructBelt);
			}

			else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver)) //change direction
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
			_ = Despawn.ServerSingle(gameObject);
		}

		[VVNote(VVHighlight.SafeToModify100)]
		private void ChangeDirection()
		{
			int count = (int)CurrentDirection + 1;

			if (count > 11)
			{
				count = 0;
			}

			CurrentDirection = (ConveyorDirection)count;

			spriteHandler.SetSpriteVariant(count);
		}

		#endregion Interaction

		#region Multitool Interaction

		public MultitoolConnectionType ConType => MultitoolConnectionType.Conveyor;

		public IMultitoolMasterable Master { get; set; }

		[field: SerializeField] public bool RequireLink { get; set; } = true;

		public bool TrySetMaster(GameObject performer, IMultitoolMasterable Inmaster)
		{
			var SwitchOLd = (Master as ConveyorBeltSwitch);
			SwitchOLd?.RemoveConveyorBelt(this);

			Master = Inmaster;


			var SwitchNew = (Master as ConveyorBeltSwitch);
			SwitchNew.AddConveyorBelt(this);
			return true;
		}

		public void SetMasterEditor(IMultitoolMasterable master)
		{
			TrySetMaster(null, master);
		}


		#endregion Multitool Interaction
	}
}
