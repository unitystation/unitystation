using System.Linq;
using UnityEngine;
using Mirror;

namespace Disposals
{
	public class DisposalPipeObject : NetworkBehaviour, IExaminable, ICheckedInteractable<HandApply>
	{
		[SerializeField] float wrenchTime = 0;
		[SerializeField] float weldTime = 3;

		RegisterTile registerTile;
		TileChangeManager tileChangeManager;
		ObjectBehaviour behaviour;

		[SerializeField]
		[Tooltip("Tile to spawn when pipe is welded in the Up orientation.")]
		DisposalPipe disposalPipeTileUp = null;

		[SerializeField]
		[Tooltip("Tile to spawn when pipe is welded in the Down orientation.")]
		DisposalPipe disposalPipeTileDown = null;

		[SerializeField]
		[Tooltip("Tile to spawn when pipe is welded in the Left orientation.")]
		DisposalPipe disposalPipeTileLeft = null;

		[SerializeField]
		[Tooltip("Tile to spawn when pipe is welded in the Right orientation.")]
		DisposalPipe disposalPipeTileRight = null;

		string objectName;
		HandApply currentInteraction;

		public bool Anchored => !behaviour.IsPushable;

		void Awake()
		{
			registerTile = gameObject.RegisterTile();
			tileChangeManager = registerTile.TileChangeManager;
			behaviour = GetComponent<ObjectBehaviour>();
		}

		public override void OnStartServer()
		{
			objectName = gameObject.ExpensiveName();
			if (gameObject.TryGetComponent(out ObjectAttributes attributes))
			{
				objectName = attributes.InitialName;
			}
		}

		#region Interactions

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench)
				|| Validations.HasUsedActiveWelder(interaction);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			currentInteraction = interaction;

			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench))
			{
				TryWrench();
			}

			else if (Validations.HasUsedActiveWelder(interaction))
			{
				TryWeld();
			}
		}

		public string Examine(Vector3 worldPos = default)
		{
			return Anchored ? "It is secured in place, but not welded to the floor." : "It is currently unsecured.";
		}

		#endregion Interactions

		#region Construction

		void TryWrench()
		{
			if (!VerboseFloorExists()) return;
			if (!VerbosePlatingExposed()) return;
			if (VerbosePipeExists()) return;

			Wrench();
		}

		void TryWeld()
		{
			if (!VerboseFloorExists()) return;
			if (!VerbosePlatingExposed()) return;
			if (VerbosePipeExists()) return;
			if (!VerboseSecured()) return;

			Weld();
		}

		bool PipeExists()
		{
			// Check for pipe tile.
			if (registerTile.Matrix.GetDisposalPipesAt(registerTile.LocalPositionServer).Any()) return true;

			// Check for pipe objects.
			var pipeObjects = registerTile.Matrix.Get<DisposalPipeObject>(registerTile.LocalPositionServer, true);
			foreach (DisposalPipeObject pipeObject in pipeObjects)
			{
				if (pipeObject == this) continue;
				if (pipeObject.Anchored) return true;
			}

			return false;
		}

		bool VerboseFloorExists()
		{
			if (!MatrixManager.IsSpaceAt(registerTile.WorldPositionServer, true)) return true;

			Chat.AddExamineMsg(currentInteraction.Performer, $"A floor must be present to secure the {objectName}!");
			return false;
		}

		bool VerbosePlatingExposed()
		{
			if (!tileChangeManager.MetaTileMap.HasTile(registerTile.LocalPositionServer, LayerType.Floors, true)) return true;

			Chat.AddExamineMsg(
					currentInteraction.Performer,
					$"The floor plating must be exposed before you can secure the {objectName} to the floor!");
			return false;
		}

		bool VerbosePipeExists()
		{
			if (!PipeExists()) return false;

			Chat.AddExamineMsgFromServer(currentInteraction.Performer, "A disposal pipe already exists here!");
			return true;
		}

		bool VerboseSecured()
		{
			if (Anchored) return true;

			Chat.AddExamineMsgFromServer(currentInteraction.Performer, $"The {objectName} must be secured to the floor first!");
			return false;
		}

		void Wrench()
		{
			if (Anchored) Unsecure();
			else Secure();
		}

		void Secure()
		{
			ToolUtils.ServerUseToolWithActionMessages(currentInteraction, wrenchTime,
					wrenchTime == 0 ? "" : $"You start securing the {objectName} to the floor...",
					wrenchTime == 0 ? "" : $"{currentInteraction.Performer.ExpensiveName()} starts securing the {objectName} to the floor...",
					$"You secure the {objectName} to the floor.",
					$"{currentInteraction.Performer.ExpensiveName()} secures the {objectName} to the floor.",
					() => behaviour.ServerSetAnchored(true, currentInteraction.Performer)
			);
		}

		void Unsecure()
		{
			ToolUtils.ServerUseToolWithActionMessages(currentInteraction, wrenchTime,
					wrenchTime == 0 ? "" : $"You start unsecuring the {objectName} from the floor...",
					wrenchTime == 0 ? "" : $"{currentInteraction.Performer.ExpensiveName()} starts unsecuring the {objectName} from the floor...",
					$"You unsecure the {objectName} from the floor.",
					$"{currentInteraction.Performer.ExpensiveName()} unsecures the {objectName} from the floor.",
					() => behaviour.ServerSetAnchored(false, currentInteraction.Performer)
			);
		}

		void Weld()
		{
			ToolUtils.ServerUseToolWithActionMessages(currentInteraction, weldTime,
					$"You start welding the {objectName} to the floor...",
					$"{currentInteraction.Performer.ExpensiveName()} starts welding the {objectName} to the floor...",
					$"You weld the {objectName} to the floor.",
					$"{currentInteraction.Performer.ExpensiveName()} welds the {objectName} to the floor.",
					() => ChangePipeObjectToTile()
			);
		}

		void ChangePipeObjectToTile()
		{
			Orientation orientation = GetComponent<Directional>().CurrentDirection;

			// Spawn the correct disposal pipe tile, based on current orientation.
			DisposalPipe pipeTileToSpawn = GetPipeTileByOrientation(orientation);
			if (pipeTileToSpawn != null)
			{
				var matrixTransform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
				Color pipeColor = GetComponentInChildren<SpriteRenderer>().color;
				registerTile.Matrix.AddUnderFloorTile(registerTile.LocalPositionServer, pipeTileToSpawn, matrixTransform, pipeColor);
				tileChangeManager.UpdateTile(registerTile.LocalPositionServer, pipeTileToSpawn);
				Despawn.ServerSingle(gameObject);
			}
			else
			{
				Logger.LogError($"Failed to spawn disposal pipe tile! Is {name} missing reference to tile asset for {orientation}?");
			}
		}

		DisposalPipe GetPipeTileByOrientation(Orientation orientation)
		{
			switch (orientation.AsEnum())
			{
				case OrientationEnum.Up:
					return disposalPipeTileUp;
				case OrientationEnum.Down:
					return disposalPipeTileDown;
				case OrientationEnum.Left:
					return disposalPipeTileLeft;
				case OrientationEnum.Right:
					return disposalPipeTileRight;
			}

			return default;
		}

		#endregion Construction
	}
}
