using UnityEngine;
using WebSocketSharp;

namespace Disposals
{
	/// <summary>
	/// Deconstruction logic for disposal pipes
	/// </summary>
	[CreateAssetMenu(fileName = "DeconstructDisposalPipe",
		menuName = "Interaction/TileInteraction/DeconstructDisposalPipe")]
	public class DeconstructDisposalPipe : TileInteraction
	{
		[SerializeField]
		[Tooltip("Seconds required to cut the welds of the disposal pipe.")]
		float seconds = 3;

		TileApply deconstructInteraction;

		#region Interactions

		public override bool WillInteract(TileApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			return Validations.HasUsedActiveWelder(interaction);
		}

		public override void ServerPerformInteraction(TileApply interaction)
		{
			deconstructInteraction = interaction;

			TryWeld();
		}

		#endregion Interactions

		#region Deconstruction

		bool VerboseDisposalMachineExists()
		{
			Matrix matrix = deconstructInteraction.TileChangeManager.MetaTileMap.Layers[LayerType.Underfloor].matrix;

			if ((deconstructInteraction.BasicTile as DisposalPipe).PipeType == DisposalPipeType.Terminal)
			{
				DisposalMachine disposalMachine = matrix.GetFirst<DisposalMachine>(deconstructInteraction.TargetCellPos, true);
				if (disposalMachine != null && disposalMachine.MachineAttachedOrGreater)
				{
					string machineName = disposalMachine.name;
					if (disposalMachine.TryGetComponent(out ObjectAttributes attributes) &&
						!attributes.InitialName.IsNullOrEmpty())
					{
						machineName = attributes.InitialName;
					}

					Chat.AddExamineMsgFromServer(
							deconstructInteraction.Performer,
							$"The {machineName} must be removed before you can cut the disposal pipe welds!");
					return true;
				}
			}

			return false;
		}

		void TryWeld()
		{
			if (VerboseDisposalMachineExists()) return;

			Weld();
		}

		void Weld()
		{
			ToolUtils.ServerUseToolWithActionMessages(
					deconstructInteraction, seconds,
					"You start slicing the welds that secure the disposal pipe...",
					$"{deconstructInteraction.Performer.ExpensiveName()} starts cutting the disposal pipe welds...",
					"You cut the disposal pipe welds.",
					$"{deconstructInteraction.Performer.ExpensiveName()} cuts the disposal pipe welds.",
					() => DeconstructPipe()
			);
		}

		void DeconstructPipe()
		{
			DisposalPipe pipe = deconstructInteraction.BasicTile as DisposalPipe;

			// Despawn pipe tile
			var matrix = MatrixManager.AtPoint(deconstructInteraction.WorldPositionTarget.NormalizeTo3Int(), true).Matrix;
			matrix.RemoveUnderFloorTile(deconstructInteraction.TargetCellPos, pipe);

			// Spawn pipe GameObject
			if (deconstructInteraction.BasicTile.SpawnOnDeconstruct == null) return;

			var spawn = Spawn.ServerPrefab(deconstructInteraction.BasicTile.SpawnOnDeconstruct, deconstructInteraction.WorldPositionTarget);
			if (!spawn.Successful) return;

			if (!spawn.GameObject.TryGetComponent(out Directional directional)) return;
			directional.FaceDirection(Orientation.FromEnum(pipe.DisposalPipeObjectOrientation));

			if (!spawn.GameObject.TryGetComponent(out ObjectBehaviour behaviour)) return;
			behaviour.ServerSetPushable(false);
		}

		#endregion Deconstruction
	}
}
