using System.Linq;
using Logs;
using UnityEngine;
using Mirror;
using Systems.Disposals;

namespace Objects.Disposals
{
	public class DisposalPipeObject : NetworkBehaviour, IExaminable, ICheckedInteractable<HandApply>
	{
		[SerializeField]
		private float wrenchTime = 0;
		[SerializeField]
		private float weldTime = 3;

		private RegisterTile registerTile;
		private UniversalObjectPhysics behaviour;

		[SerializeField]
		[Tooltip("Tile to spawn when pipe is welded in the Up orientation.")]
		private DisposalPipe disposalPipeTileUp = null;

		[SerializeField]
		[Tooltip("Tile to spawn when pipe is welded in the Down orientation.")]
		private DisposalPipe disposalPipeTileDown = null;

		[SerializeField]
		[Tooltip("Tile to spawn when pipe is welded in the Left orientation.")]
		private DisposalPipe disposalPipeTileLeft = null;

		[SerializeField]
		[Tooltip("Tile to spawn when pipe is welded in the Right orientation.")]
		private DisposalPipe disposalPipeTileRight = null;

		private string objectName;
		private HandApply currentInteraction;

		public bool Anchored => behaviour.IsNotPushable;

		private void Awake()
		{
			registerTile = gameObject.RegisterTile();
			behaviour = GetComponent<UniversalObjectPhysics>();
		}

		public override void OnStartServer()
		{
			objectName = gameObject.ExpensiveName();
			if (gameObject.TryGetComponent<ObjectAttributes>(out var attributes))
			{
				objectName = attributes.InitialName;
			}
		}

		#region Interactions

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench)
				|| Validations.HasUsedActiveWelder(interaction);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			currentInteraction = interaction;

			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench))
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

		private void TryWrench()
		{
			if (Anchored == false)
			{
				// Try anchor
				if (VerboseFloorExists() == false) return;
				if (VerbosePlatingExposed() == false) return;
				if (VerbosePipeExists()) return;
			}

			Wrench();
		}

		private void TryWeld()
		{
			if (VerboseFloorExists() == false) return;
			if (VerbosePlatingExposed() == false) return;
			if (VerbosePipeExists()) return;
			if (VerboseSecured() == false) return;

			Weld();
		}

		private bool PipeExists()
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

		private bool VerboseFloorExists()
		{
			if (MatrixManager.IsConstructable(registerTile.WorldPositionServer, registerTile.Matrix.MatrixInfo))
			{
				return true;
			}
			Chat.AddExamineMsg(currentInteraction.Performer, $"A floor must be present to secure the {objectName}!");
			return false;
		}

		private bool VerbosePlatingExposed()
		{
			if (registerTile.TileChangeManager.MetaTileMap.HasTile(registerTile.LocalPositionServer, LayerType.Floors) == false) return true;

			Chat.AddExamineMsg(
					currentInteraction.Performer,
					$"The floor plating must be exposed before you can secure the {objectName} to the floor!");
			return false;
		}

		private bool VerbosePipeExists()
		{
			if (PipeExists() == false) return false;

			Chat.AddExamineMsgFromServer(currentInteraction.Performer, "A disposal pipe already exists here!");
			return true;
		}

		private bool VerboseSecured()
		{
			if (Anchored) return true;

			Chat.AddExamineMsgFromServer(currentInteraction.Performer, $"The {objectName} must be secured to the floor first!");
			return false;
		}

		private void Wrench()
		{
			if (Anchored)
			{
				Unsecure();
			}
			else
			{
				Secure();
			}
		}

		private void Secure()
		{
			ToolUtils.ServerUseToolWithActionMessages(currentInteraction, wrenchTime,
					wrenchTime == 0 ? "" : $"You start securing the {objectName} to the floor...",
					wrenchTime == 0 ? "" : $"{currentInteraction.Performer.ExpensiveName()} starts securing the {objectName} to the floor...",
					$"You secure the {objectName} to the floor.",
					$"{currentInteraction.Performer.ExpensiveName()} secures the {objectName} to the floor.",
					() => behaviour.ServerSetAnchored(true, currentInteraction.Performer)
			);
		}

		private void Unsecure()
		{
			ToolUtils.ServerUseToolWithActionMessages(currentInteraction, wrenchTime,
					wrenchTime == 0 ? "" : $"You start unsecuring the {objectName} from the floor...",
					wrenchTime == 0 ? "" : $"{currentInteraction.Performer.ExpensiveName()} starts unsecuring the {objectName} from the floor...",
					$"You unsecure the {objectName} from the floor.",
					$"{currentInteraction.Performer.ExpensiveName()} unsecures the {objectName} from the floor.",
					() => behaviour.ServerSetAnchored(false, currentInteraction.Performer)
			);
		}

		private void Weld()
		{
			ToolUtils.ServerUseToolWithActionMessages(currentInteraction, weldTime,
					$"You start welding the {objectName} to the floor...",
					$"{currentInteraction.Performer.ExpensiveName()} starts welding the {objectName} to the floor...",
					$"You weld the {objectName} to the floor.",
					$"{currentInteraction.Performer.ExpensiveName()} welds the {objectName} to the floor.",
					() => ChangePipeObjectToTile()
			);
		}

		private void ChangePipeObjectToTile()
		{
			var orientation = GetComponent<Rotatable>().CurrentDirection;

			// Spawn the correct disposal pipe tile, based on current orientation.
			DisposalPipe pipeTileToSpawn = GetPipeTileByOrientation(orientation);
			if (pipeTileToSpawn != null)
			{
				var matrixTransform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
				Color pipeColor = GetComponentInChildren<SpriteRenderer>().color;
				Vector3Int searchVec = registerTile.Matrix.TileChangeManager.MetaTileMap.SetTile(registerTile.LocalPositionServer, pipeTileToSpawn, matrixTransform, pipeColor);
				pipeTileToSpawn.InitialiseNode(searchVec, registerTile.Matrix);
				_ = Despawn.ServerSingle(gameObject);
			}
			else
			{
				Loggy.LogError($"Failed to spawn disposal pipe tile! Is {name} missing reference to tile asset for {orientation}?",
					Category.Pipes);
			}
		}

		private DisposalPipe GetPipeTileByOrientation(OrientationEnum orientation)
		{
			switch (orientation)
			{
				case OrientationEnum.Up_By0:
					return disposalPipeTileUp;
				case OrientationEnum.Down_By180:
					return disposalPipeTileDown;
				case OrientationEnum.Left_By90:
					return disposalPipeTileLeft;
				case OrientationEnum.Right_By270:
					return disposalPipeTileRight;
			}

			return default;
		}

		#endregion Construction
	}
}
