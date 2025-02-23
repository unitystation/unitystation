using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HealthV2;
using InGameGizmos;
using Logs;
using Tilemaps.Behaviours.Pathfinding;
using UnityEngine;

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

		private const int TENTH_OF_A_SECOND = 135;


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
		}

		public bool QueueMovementGoal(Vector3Int newTarget,
			Action onTraversalFinalStep = null,
			Action onRetryMoveToDirection = null, List<ITraversalStrat> strategies = null, bool cancelOnSlip = false)
		{
			if (health.IsDead) return false;
			if (_targetQueue.Count >= MaxQueuedTargets) return false;

			path = matrix.MetaDataLayer.Pathfinder.AStarFromTo(matrix.MetaDataLayer.Nodes,
				gameObject.TileLocalPosition().To3Int(), newTarget);
			if (path == null || path.Count == 0)
			{
				if (DebugGizmos) Loggy.Info("Attempted to move to a location that is not reachable.");
				return false;
			}
			TraversalDetails newDetails = new TraversalDetails
			{
				TargetPosition = newTarget,
				OnTraversalFinalStep = onTraversalFinalStep,
				OnRetryMoveToDirection = onRetryMoveToDirection,
				Strats = strategies,
				CancelOnSlip = cancelOnSlip
			};

			if (_isMoving == false)
			{
				CleanSlate(false);
				_ = MoveToTarget(newDetails);
				return true;
			}
			_targetQueue.Enqueue(newDetails);
			return true;
		}

		private async UniTaskVoid MoveToTarget(TraversalDetails details)
		{
			_isMoving = true;
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
			foreach (var pos in path)
			{
				if (Movement.registerTile.LocalPosition == pos) continue;
				await ProcessMovement(details, pos);
			}

			if (localPosition == path[^1])
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
				waitTicks = 0;
				if (timeoutRequestTicks > 0)
				{
					await UniTask.Delay(timeoutRequestTicks);
					timeoutRequestTicks = 0;
				}
				if (health.IsDead) break; // Incase we died while moving.
				if (Movement.IsMoving)
				{
					await UniTask.Delay(TENTH_OF_A_SECOND + (i * 10)); // If we're already moving, wait until the next retry.
					continue;
                }
				var attemptMovement = Move(pos);
				if (attemptMovement.Item1 == false || (attemptMovement.Item2 && details.CancelOnSlip))
				{
					_movingFromFirstTile = false;
					if (i > 1 && details.Strats != null)
					{
						AttemptStrategies(details.Strats, pos);
						await UniTask.Delay(TENTH_OF_A_SECOND + (i * 10));
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
					await UniTask.Delay(TENTH_OF_A_SECOND + (i * 10)); //TODO: Calculate movement speed as a delay factor.
					if (_movingFromFirstTilePosition == registerPlayer.LocalPositionServer && waitTicks > 10)
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
			CleanSlate(true);
			if (_targetQueue.Count > 0)
			{
				MoveToTarget(_targetQueue.Dequeue()); // do not await this. Let it do its thing.
			}
		}

		private void CleanSlate(bool clearPath)
		{
			_isMoving = false;
			_movingFromFirstTile = false;
			waitTicks = 0;
			timeoutRequestTicks = 0;
			if (clearPath) path.Clear();
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
			public Vector3Int TargetPosition;
			public Action OnTraversalFinalStep;
			public Action OnRetryMoveToDirection;
			public List<ITraversalStrat> Strats;
			public bool CancelOnSlip;
		}
	}
}