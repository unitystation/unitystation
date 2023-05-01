using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Objects.Disposals;
using AddressableReferences;
using Random = UnityEngine.Random;

namespace Systems.Disposals
{
	/// <summary>
	/// Represents an instance of a disposal. It takes a virtual container,
	/// which contains the entities to be disposed of. Traverse() advances the
	/// container one tile along the disposal pipe network.
	/// </summary>
	public class DisposalTraversal
	{
		readonly Matrix matrix;
		public readonly DisposalVirtualContainer virtualContainer;
		readonly UniversalObjectPhysics ObjectPhysics;

		public bool ReadyToTraverse = false;
		public bool CurrentlyDelayed = false;
		public bool TraversalFinished = false;

		private bool justStarted;
		private DisposalPipe currentPipe;
		private Vector3Int currentPipeLocalPos;
		private Orientation currentPipeOutputSide;

		private Vector3Int NextPipeVector => currentPipeOutputSide.LocalVectorInt.To3Int();
		private Vector3Int NextPipeLocalPosition => currentPipeLocalPos + NextPipeVector;

		private OrientationEnum nextPipeRequiredSide = OrientationEnum.Default;

		private Vector2 pipeEjectionFloorDamage = new Vector2(30, 55);

		/// <summary>
		/// Create a new disposal instance.
		/// Note: start a disposal instance by using DisposalsManager.Instance.NewDisposal(),
		/// as it will handle instance updates.
		/// </summary>
		/// <param name="container">The virtual container holding the entities to be disposed of.</param>
		public DisposalTraversal(DisposalVirtualContainer container)
		{
			RegisterTile registerTile = container.GetComponent<RegisterTile>();
			matrix = registerTile.Matrix;
			currentPipeLocalPos = registerTile.LocalPositionServer;
			virtualContainer = container;
			ObjectPhysics = container.GetComponent<UniversalObjectPhysics>();

			currentPipe = GetPipeAt(currentPipeLocalPos, DisposalPipeType.Terminal);
			if (currentPipe == null)
			{
				virtualContainer.EjectContents();
				DespawnContainerAndFinish();
				return;
			}
			// First() assumes initial pipe is of type: Terminal (as it has just one connectable side)
			// Not an issue unless new traversals do not start from disposal machines.
			// Consider refactoring and using GetPipeLeavingSide(currentPipe, someSide); if this becomes an issue.
			currentPipeOutputSide = Orientation.FromEnum(currentPipe.ConnectablePoints.First().Key);

			justStarted = true;
			ReadyToTraverse = true;
		}

		public void ChangeMovementTrajectory(MoveAction moveAction)
		{
			switch (moveAction)
			{
				case MoveAction.MoveUp:
					currentPipeOutputSide = Orientation.Up;
					nextPipeRequiredSide = GetConnectedSide(currentPipeOutputSide, true).AsEnum();
					break;
				case MoveAction.MoveLeft:
					currentPipeOutputSide = Orientation.Left;
					nextPipeRequiredSide = GetConnectedSide(currentPipeOutputSide, true).AsEnum();
					break;
				case MoveAction.MoveDown:
					currentPipeOutputSide = Orientation.Down;
					nextPipeRequiredSide = GetConnectedSide(currentPipeOutputSide, true).AsEnum();
					break;
				case MoveAction.MoveRight:
					currentPipeOutputSide = Orientation.Right;
					nextPipeRequiredSide = GetConnectedSide(currentPipeOutputSide, true).AsEnum();
					break;
				case MoveAction.NoMove:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(moveAction), moveAction, null);
			}
			Traverse();
		}

		/// <summary>
		/// Advances the disposal traversal by one tile.
		/// </summary>
		public void Traverse()
		{
			ReadyToTraverse = false;

			// Check if just started so we don't end the traversal at the disposal machine we started from.
			if (justStarted == false && currentPipe.PipeType == DisposalPipeType.Terminal && BlocksForPipeCrawling() == false)
			{
				EjectViaDisposalPipeTerminal();
				return;
			}

			// Advance to next pipe
			justStarted = false;
			if (virtualContainer.SelfControlled == false) nextPipeRequiredSide = GetConnectedSide(currentPipeOutputSide).AsEnum();
			DisposalPipe nextPipe = GetPipeAt(NextPipeLocalPosition, requiredSide: nextPipeRequiredSide);

			if (nextPipe == null)
			{
				if (virtualContainer.SelfControlled)
				{
					var directionsToCheck = new List<Vector3Int>
						{
							new Vector3Int(-1, 0, 0),
							new Vector3Int(1, 0, 0),
							new Vector3Int(0, -1, 0),
							new Vector3Int(0, 1, 0)
						};

					foreach (var direction in directionsToCheck)
					{
						if (GetPipeAt(out var pipe, currentPipeLocalPos + direction,
							    requiredSide: nextPipeRequiredSide) == null) continue;
						if (pipe.PipeType != DisposalPipeType.Terminal) continue;
						EjectViaPipeEnd();
						break;
					}
					return;
				}
				EjectViaPipeEnd();
				return;
			}

			TransferContainerToVector(NextPipeVector);
			currentPipeLocalPos = NextPipeLocalPosition;
			currentPipeOutputSide = Orientation.FromEnum(GetPipeLeavingSide(nextPipe, nextPipeRequiredSide));
			currentPipe = nextPipe;

			ReadyToTraverse = true;
		}

		private bool BlocksForPipeCrawling()
		{
			if (virtualContainer.SelfControlled == false) return false;
			var disposalMachine = matrix.GetFirst<DisposalMachine>(currentPipeLocalPos, true);
			if (disposalMachine == null) return false;
			return disposalMachine is DisposalBin == true;
		}

		private DisposalPipe GetPipeAt(Vector3Int localPosition, DisposalPipeType? type = null, OrientationEnum? requiredSide = null)
		{
			// Gets the first disposal pipe that meets the criteria.
			foreach (DisposalPipe pipe in matrix.GetDisposalPipesAt(localPosition))
			{
				if (type != null && (pipe.PipeType != type.Value && virtualContainer.SelfControlled == false)) continue;
				if (requiredSide != null && pipe.ConnectablePoints.ContainsKey(requiredSide.Value) == false && virtualContainer.SelfControlled == false) continue;

				return pipe;
			}

			return default;
		}

		private DisposalPipe GetPipeAt(out DisposalPipe result, Vector3Int localPosition, DisposalPipeType? type = null, OrientationEnum? requiredSide = null)
		{
			result = GetPipeAt(localPosition, type, requiredSide);
			if (result == default)
			{
				result = null;
			}
			return default;
		}

		private Orientation GetConnectedSide(Orientation side, bool opposite = false)
		{
			if (opposite)
			{
				return side.AsEnum() switch
				{
					OrientationEnum.Up_By0 => Orientation.Up,
					OrientationEnum.Down_By180 => Orientation.Down,
					OrientationEnum.Left_By90 => Orientation.Left,
					OrientationEnum.Right_By270 => Orientation.Right,
					_ => throw new ArgumentOutOfRangeException(nameof(side), side, "Invalid OrientationEnum value."),
				};
			}
			return side.AsEnum() switch
			{
				OrientationEnum.Up_By0 => Orientation.Down,
				OrientationEnum.Down_By180 => Orientation.Up,
				OrientationEnum.Left_By90 => Orientation.Right,
				OrientationEnum.Right_By270 => Orientation.Left,
				_ => throw new ArgumentOutOfRangeException(nameof(side), side, "Invalid OrientationEnum value."),
			};
		}

		private OrientationEnum GetPipeLeavingSide(DisposalPipe pipe, OrientationEnum sideEntered)
		{
			switch (pipe.PipeType)
			{
				// Basic pipes should only have two connectable points, so return the first that is not the entered side.
				case DisposalPipeType.Basic:
					return virtualContainer.SelfControlled ?
						pipe.ConnectablePoints.First().Key : //If self controlled, ignore restriction.
						pipe.ConnectablePoints.First(x => x.Key != sideEntered).Key;

				// Terminals (pipes that connect to disposal machines) should only have one connectable point.
				case DisposalPipeType.Terminal:
					return pipe.ConnectablePoints.First().Key;

				case DisposalPipeType.Merger:
					OrientationEnum tryOutputSide =
						pipe.ConnectablePoints.First
							(x => x.Value == DisposalPipeConnType.Output).Key;
					if (tryOutputSide == sideEntered)
					{
						// If a disposals instance enters a merger pipe from the output, just choose an input to exit from.
						return pipe.ConnectablePoints.First(x => x.Value != DisposalPipeConnType.Output).Key;
					}
					else
					{
						return tryOutputSide;
					}

				// TODO: Implement disposal instance destination to auto-select appropriate path to take.
				// Feature: If no destination, split incoming packets alternately.
				case DisposalPipeType.Splitter:
					return pipe.ConnectablePoints.First(x => x.Value != DisposalPipeConnType.Input).Key;
			}

			return default;
		}

		private void TransferContainerToVector(Vector3Int nextPipePosition)
		{
			ObjectPhysics.Pushing.Clear();
			ObjectPhysics.ForceTilePush(nextPipePosition.To2Int(), ObjectPhysics.Pushing, null);
		}

		private void EjectViaPipeEnd()
		{
			TryDamageTileFromEjection(NextPipeLocalPosition);
			var worldPos = MatrixManager.LocalToWorld(NextPipeLocalPosition, matrix);
			SoundManager.PlayNetworkedAtPos(DisposalsManager.Instance.DisposalEjectionHiss, worldPos);
			TransferContainerToVector(NextPipeVector);
			virtualContainer.EjectContentsWithVector(currentPipeOutputSide.LocalVector);
			DespawnContainerAndFinish();
		}

		private void EjectViaDisposalPipeTerminal()
		{
			var disposalMachine = matrix.GetFirst<DisposalMachine>(currentPipeLocalPos, true);
			if (disposalMachine is not null && disposalMachine.MachineSecured)
			{
				EjectViaDisposalMachine(disposalMachine);
			}
			else
			{
				// Ended at a pipe terminal, but no disposal machinery was detected at its location. Ejecting upwards...
				TryDamageTileFromEjection(currentPipeLocalPos);
				// Eject contents with zero vector to give spin on contents.
				virtualContainer.EjectContentsWithVector(Vector3.zero);
				DespawnContainerAndFinish();
			}
		}

		private void EjectViaDisposalMachine(DisposalMachine machine)
		{
			virtualContainer.SelfControlled = false;
			if (machine is DisposalOutlet)
			{
				(machine as DisposalOutlet).ServerReceiveAndEjectContainer(virtualContainer);
				// Do not call <see cref="DespawnContainerAndFinish"/> as the
				// disposal outlet will take over responsibility of virtualContainer.
				TraversalFinished = true;
			}
			else if (machine is DisposalIntake)
			{
				DespawnContainerAndFinish();
			}
			else if (machine is DisposalBin)
			{
				DespawnContainerAndFinish();
			}
		}

		private void TryDamageTileFromEjection(Vector3Int localPosition)
		{
			if (matrix.TileChangeManager.MetaTileMap.HasTile(localPosition, LayerType.Floors) == false) return;
			matrix.TileChangeManager.MetaTileMap.ApplyDamage(
				localPosition,
				Random.Range(pipeEjectionFloorDamage.x, pipeEjectionFloorDamage.y),
				virtualContainer.gameObject.AssumedWorldPosServer().CutToInt());
		}

		/// <summary>
		/// If container still has contents, they will be ejected with no direction.
		/// </summary>
		private void DespawnContainerAndFinish()
		{
			virtualContainer.SelfControlled = false;
			TraversalFinished = true;
			_ = Despawn.ServerSingle(virtualContainer.gameObject);
		}
	}
}
