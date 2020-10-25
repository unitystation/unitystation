using UnityEngine;
using WebSocketSharp;

namespace Objects.Disposals
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
		private float seconds = 3;

		#region Interactions

		public override bool WillInteract(TileApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			return Validations.HasUsedActiveWelder(interaction);
		}

		public override void ServerPerformInteraction(TileApply interaction)
		{
			if (VerboseDisposalMachineExists(interaction)) return;

			Weld(interaction);
		}

		#endregion Interactions

		#region Deconstruction

		private bool VerboseDisposalMachineExists(TileApply interaction)
		{
			Matrix matrix = interaction.TileChangeManager.MetaTileMap.Layers[LayerType.Underfloor].matrix;

			if ((interaction.BasicTile as DisposalPipe).PipeType == DisposalPipeType.Terminal)
			{
				DisposalMachine disposalMachine = matrix.GetFirst<DisposalMachine>(interaction.TargetCellPos, true);
				if (disposalMachine != null && disposalMachine.MachineAttachedOrGreater)
				{
					string machineName = disposalMachine.name;
					if (disposalMachine.TryGetComponent(out ObjectAttributes attributes) &&
						!attributes.InitialName.IsNullOrEmpty())
					{
						machineName = attributes.InitialName;
					}

					Chat.AddExamineMsgFromServer(
							interaction.Performer,
							$"The {machineName} must be removed before you can cut the disposal pipe welds!");
					return true;
				}
			}

			return false;
		}

		private void Weld(TileApply interaction)
		{
			ToolUtils.ServerUseToolWithActionMessages(
					interaction, seconds,
					"You start slicing the welds that secure the disposal pipe...",
					$"{interaction.Performer.ExpensiveName()} starts cutting the disposal pipe welds...",
					"You cut the disposal pipe welds.",
					$"{interaction.Performer.ExpensiveName()} cuts the disposal pipe welds.",
					() => DeconstructPipe(interaction)
			);
		}

		private void DeconstructPipe(TileApply interaction)
		{
			DisposalPipe pipe = interaction.BasicTile as DisposalPipe;

			// Despawn pipe tile
			var matrix = MatrixManager.AtPoint(interaction.WorldPositionTarget.NormalizeTo3Int(), true).Matrix;
			matrix.RemoveUnderFloorTile(interaction.TargetCellPos, pipe);

			// Spawn pipe GameObject
			if (interaction.BasicTile.SpawnOnDeconstruct == null) return;

			var spawn = Spawn.ServerPrefab(interaction.BasicTile.SpawnOnDeconstruct, interaction.WorldPositionTarget);
			if (!spawn.Successful) return;

			if (!spawn.GameObject.TryGetComponent(out Directional directional)) return;
			directional.FaceDirection(Orientation.FromEnum(pipe.DisposalPipeObjectOrientation));

			if (!spawn.GameObject.TryGetComponent(out ObjectBehaviour behaviour)) return;
			behaviour.ServerSetPushable(false);
		}

		#endregion Deconstruction
	}
}
