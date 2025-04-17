using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using HealthV2;
using InGameGizmos;
using Logs;
using Tilemaps.Behaviours.Pathfinding;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mobs.Traversal
{
	/// <summary>
	/// The main component responsible for automatically pathfidnign and moving mobs around the map.
	/// </summary>
	public class MobTraversal : MonoBehaviour
	{
		public PlayerScript Mob;
		public MovementSynchronisation Movement;
		public int MaxQueuedTargets = 12;
		public int MaxRetries = 6;
		public int MaxTicksForCancellationWait = 6;
		public bool DebugGizmos = false;

		public Action<Vector3Int> OnDoneTraversalToLocation;
		public Action<Vector3Int> OnTraversalFailedCompletely;
		public Action<Vector3Int> OnTraversalFailedAndRetrying;

		public int QueuedTargets => _targetQueue.Count;

		private LivingHealthMasterBase health => Mob.playerHealth;
		private Matrix matrix => Mob.RegisterPlayer.Matrix;
		private RegisterPlayer registerPlayer => Mob.RegisterPlayer;
		private Vector3Int localPosition => registerPlayer.LocalPositionServer;

		private readonly Queue<TraversalDetails> _targetQueue = new Queue<TraversalDetails>();
		private bool _isMoving = false;
		private bool _movingFromFirstTile = false;
		private Vector3Int _movingFromFirstTilePosition = Vector3Int.zero;
		private int waitTicks = 0;
		private List<Vector3Int> path = new List<Vector3Int>();
		private MovementSynchronisation.MoveData _moveData = new MovementSynchronisation.MoveData();
		private int timeoutRequestTicks = 0;
		private CancellationTokenSource cancellationToken = new();

		private const int TENTH_OF_A_SECOND = 135;
		private const int A_SECOND = 1350;


		private void Awake()
		{
			Mob ??= GetComponent<PlayerScript>();
			Movement ??= GetComponent<MovementSynchronisation>();
			Movement.OnLocalTileReached.AddListener(SetMovingToTileToFalse);
			Movement.OnBumpedIntoSomething.AddListener(OnBumpedIntoSomething);
			if (MaxRetries < 2) MaxRetries = 2;
		}

		private void OnDestroy()
		{
			Movement.OnLocalTileReached.RemoveListener(SetMovingToTileToFalse);
			Movement.OnBumpedIntoSomething.RemoveListener(OnBumpedIntoSomething);
			CancelCurrentTraversalImmediately();
		}

		public static List<Vector3Int> GeneratePath(Vector3Int start, Vector3Int target, Matrix matrix)
		{
			return matrix.MetaDataLayer.Pathfinder.AStarFromTo(matrix.MetaDataLayer.Nodes,
				start, target, false,
				Vector3Int.Distance(start, target) > 2);
		}

		public bool QueueMovementGoal(Vector3Int newTarget,
			Action onTraversalFinalStep = null,
			Action onRetryMoveToDirection = null, List<ITraversalStrat> strategies = null, bool cancelOnSlip = false)
		{
			TraversalDetails newDetails = new TraversalDetails
			{
				TargetPosition = newTarget,
				OnTraversalFinalStep = onTraversalFinalStep,
				OnRetryMoveToDirection = onRetryMoveToDirection,
				Strats = strategies,
				CancelOnSlip = cancelOnSlip
			};
			return QueueMovementGoal(newDetails);
		}

		public bool QueueMovementGoal(TraversalDetails newTraversalDetails)
		{
			if (health.IsDead) return false;
			if (_targetQueue.Count >= MaxQueuedTargets) return false;

			//TODO: Add a check to switch between using BFS and A*.
			path = GeneratePath(gameObject.TileLocalPosition().To3Int(), newTraversalDetails.TargetPosition, matrix);
			if (path == null || path.Count == 0)
			{
				if (DebugGizmos) Loggy.Info("Attempted to move to a location that is not reachable.");
				return false;
			}

			if (_isMoving == false)
			{
				CleanSlate(false);
				_ = MoveToTarget(newTraversalDetails);
				return true;
			}
			_targetQueue.Enqueue(newTraversalDetails);
			return true;
		}

		public async UniTask CancelQueueAndGenerateNewPathToFollow(TraversalDetails newDetails)
		{
			var newPath = GeneratePath(gameObject.TileLocalPosition().To3Int(), newDetails.TargetPosition, matrix);
			if (newPath == null || newPath.Count == 0)
			{
				if (DebugGizmos) Loggy.Info("Attempted to move to a location that is not reachable.");
				return;
			}
			await CancelCurrentTraversal();
			path = newPath;
			_ = MoveToTarget(newDetails);
		}

		public void CancelCurrentTraversalImmediately()
		{
			Loggy.Warning($"Canceling traversal on {gameObject.name} without awaiting MoveToTarget() to finish processing. This may cause some errors, but can be ignored if doing it OnDestroy().");
			if (_isMoving)
			{
				cancellationToken.Cancel();
			}
			CleanSlate(true);
			_targetQueue.Clear();
		}

		public async UniTask CancelCurrentTraversal()
		{
			if (_isMoving)
			{
				cancellationToken.Cancel();
				path.Clear();
				var cancelWaitTicks = 0;
				while (_isMoving && cancelWaitTicks < MaxTicksForCancellationWait)
				{
					cancelWaitTicks++;
					await UniTask.WaitForEndOfFrame(); // wait for the cancellation to finish.
				}
				if (DebugGizmos) Loggy.Info($"Cancelled current traversal on {gameObject.name} after {cancelWaitTicks} wait ticks.");
			}
			CleanSlate(true);
			_targetQueue.Clear();
		}

		private async UniTask MoveToTarget(TraversalDetails details)
		{
			_isMoving = true;
			var stoppedNearTargetObject = false;
			DebugGizmos_HighlightTargetArea(details.TargetPosition);
			if (path == null || path.Count == 0)
			{
				if (DebugGizmos) Loggy.Error("Attempted to move mob without any directions.");
				OnTraversalFailedCompletely?.Invoke(details.TargetPosition);
				MoveOnToTheNextQueuedTarget();
				details.OnTraversalFinalStep?.Invoke();
				return;
			}
			DebugGizmos_DrawPath(path);
			try
			{
				for (int i = 0; i < path.Count; i++)
				{
					if (cancellationToken.IsCancellationRequested || path.Count == 0) break;
					if (Movement.registerTile.LocalPosition == path[i]) continue;
					if (health.IsDead || (registerPlayer.IsSlippingServer && details.CancelOnSlip)) break; // Incase we died or stunned while moving.
					if (details.TargetObject != null)
					{
						if (Vector3.Distance(localPosition, details.TargetObject.TileLocalPosition().To3()) < 1.25)
						{
							stoppedNearTargetObject = true;
							break;
						}
					}
					await ProcessMovement(details, path[i]);
				}
			}
			catch (Exception e)
			{
				Loggy.Error($"Error happened while traversing: {e}");
			}

			if ((path is { Count: > 0 } && localPosition == path[^1]) || stoppedNearTargetObject)
			{
				OnDoneTraversalToLocation?.Invoke(details.TargetPosition);
			}
			else
			{
				OnTraversalFailedCompletely?.Invoke(details.TargetPosition);
			}
			MoveOnToTheNextQueuedTarget();
			details.OnTraversalFinalStep?.Invoke();
		}

		private async UniTask ProcessMovement(TraversalDetails details, Vector3Int pos)
		{
			for (int i = 0; i < MaxRetries; i++)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					details.OnTraversalCancelled?.Invoke();
					return;
				}
				waitTicks = 0;
				if (timeoutRequestTicks > 0)
				{
					await UniTask.Delay(timeoutRequestTicks, cancellationToken: cancellationToken.Token);
					timeoutRequestTicks = 0;
				}
				if (health.IsDead || (registerPlayer.IsSlippingServer && details.CancelOnSlip)) break; // Incase we died or stunned while moving.
				if (Movement.IsMoving)
				{
					await UniTask.Delay(TENTH_OF_A_SECOND + (i * 10), cancellationToken: cancellationToken.Token); // If we're already moving, wait until the next retry.
					continue;
                }
				var attemptMovement = Move(pos);
				if (attemptMovement.Item1 == false || (attemptMovement.Item2 && details.CancelOnSlip))
				{
					_movingFromFirstTile = false;
					if (i > 1 && details.Strats != null)
					{
						AttemptStrategies(details.Strats, pos);
						await UniTask.Delay(TENTH_OF_A_SECOND + (i * 10), cancellationToken: cancellationToken.Token);
					}
					if (cancellationToken.IsCancellationRequested)
					{
						details.OnTraversalCancelled?.Invoke();
						return;
					}
					continue;
				}
				_movingFromFirstTile = true;
				_movingFromFirstTilePosition = registerPlayer.LocalPositionServer;
				while (_movingFromFirstTile && waitTicks <= 25)
				{
					waitTicks++;
					// 135ms + 10ms per retry.
					// We add 10ms to the delay for each retry incase there's something holding back a succesful tile move.
					await UniTask.Delay(TENTH_OF_A_SECOND + (i * 10), cancellationToken: cancellationToken.Token); //TODO: Calculate movement speed as a delay factor.
					if ((_movingFromFirstTilePosition == registerPlayer.LocalPositionServer && waitTicks > 10) || cancellationToken.IsCancellationRequested)
					{
						// We're def stuck if 10 ticks passed and we're still standing in the same place.
						break; // Maybe we hit a door?
					}
				}
				if (registerPlayer.LocalPositionServer == pos)
				{
					_movingFromFirstTile = false;
					break; // we reached the next tile.
				}
				OnTraversalFailedAndRetrying?.Invoke(details.TargetPosition);
			}
		}

		/// <summary>
		/// Moves the mob to the direction of a target position.
		/// </summary>
		/// <param name="targetPosition">Local Position used to determine the direction of the next step. World Positions will be buggy.</param>
		/// <returns>Item1 is if we are moving. Item2 is if we slipped.</returns>
		public Tuple<bool, bool> Move(Vector3Int targetPosition)
		{
			_moveData = Movement.GenerateMoveData(
				Movement.registerTile.LocalPosition,
				MovementSynchronisation.VectorToPlayerMoveDirection(PathfindingUtils.GetDirectionToPosition(Mob, targetPosition).To2Int()));
			Tuple<bool, bool> result = new Tuple<bool, bool>(Movement.TryMove(ref _moveData, gameObject, CustomNetworkManager.IsServer, out var slip), slip);
			return result;
		}

		private void AttemptStrategies(List<ITraversalStrat> strategies, Vector3Int target)
		{
			foreach (var strat in strategies)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}
				try
				{
					var check = strat.ObsticalCheck(target, Mob);
					if (check.Item1)
					{
						timeoutRequestTicks += strat.TraverseObstical(target, check.Item2, check.Item3, Mob);
						return;
					}
				}
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
				}
			}
		}

		private void MoveOnToTheNextQueuedTarget()
		{
			if (cancellationToken.IsCancellationRequested)
			{
				CleanSlate(true);
				return;
			}
			else
			{
				CleanSlate(true);
				if (_targetQueue.Count > 0)
				{
					_ = MoveToTarget(_targetQueue.Dequeue());
				}
			}
		}

		private void CleanSlate(bool clearPath)
		{
			_isMoving = false;
			_movingFromFirstTile = false;
			waitTicks = 0;
			timeoutRequestTicks = 0;
			if (clearPath && path != null) path.Clear();
			cancellationToken = new CancellationTokenSource();
		}

		private void SetMovingToTileToFalse(Vector3Int arg0, Vector3Int vector3Int)
		{
			_movingFromFirstTile = false;
		}

		private void OnBumpedIntoSomething()
		{
			_movingFromFirstTile = false;
		}

		private void DebugGizmos_HighlightTargetArea(Vector3Int pos)
		{
			if (DebugGizmos == false) return;
			GameGizmomanager.AddNewBoxStaticClient(gameObject, pos, Color.red);
		}

		private void DebugGizmos_DrawPath(List<Vector3Int> path)
		{
			if (DebugGizmos == false) return;
			StartCoroutine(PathfindingUtils.Visualize(path, gameObject.TileLocalPosition().To3Int()));
		}

		public struct TraversalDetails
		{
			public bool CancelOnSlip;
			public Vector3Int TargetPosition;
			public Action OnTraversalFinalStep;
			public Action OnRetryMoveToDirection;
			public Action OnTraversalCancelled;
			public GameObject TargetObject;
			public List<ITraversalStrat> Strats;
		}
	}

	public enum PathfinderType
	{
		AStar = 0,
		BFS = 1,
	}
}