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
		private bool _movingFromTile = false;
		private int waitTicks = 0;
		private List<Vector3Int> path = new List<Vector3Int>();

		private MovementSynchronisation.MoveData _moveData = new MovementSynchronisation.MoveData();

		private void Awake()
		{
			Mob ??= GetComponent<PlayerScript>();
			Movement ??= GetComponent<MovementSynchronisation>();
			Movement.OnLocalTileReached.AddListener(SetMovingToTileToFalse);
			if (MaxRetries < 2) MaxRetries = 2;
		}

		public bool QueueMovementGoal(Vector3Int newTarget,
			Action onTraversalFinalStep = null,
			Action onRetryMoveToDirection = null, List<TraversalStrat> strategies = null, bool allowIncompletePaths = false)
		{
			if (health.IsDead) return false;
			if (_targetQueue.Count >= MaxQueuedTargets) return false;
			path = matrix.MetaDataLayer.Pathfinder.AStarFromTo(matrix.MetaDataLayer.Nodes,
				gameObject.TileLocalPosition().To3Int(), newTarget, false);
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
				Strats = strategies
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
				for (int i = 0; i < MaxRetries; i++)
				{
					waitTicks = 0;
					if (health.IsDead) break;
					if (Movement.IsMoving) continue;
					if (i > 1 && details.Strats != null)
					{
						AttemptStrategies(details.Strats, pos);
						await UniTask.Delay(50);
					}
					//PathfindingUtils.ShoveMobToPosition(Mob, pos, 12f);
					_moveData = Movement.GenerateMoveData(
						Movement.registerTile.LocalPosition,
						MovementSynchronisation.VectorToPlayerMoveDirection(PathfindingUtils.GetDirectionToPosition(Mob, pos).To2Int()));
					Movement.TryMove(ref _moveData, gameObject, CustomNetworkManager.IsServer, out var slip);
					_movingFromTile = true;
					while (_movingFromTile && waitTicks <= 35)
					{
						waitTicks++;
						await UniTask.Delay(135);
					}
					if (registerPlayer.LocalPositionServer == pos)
					{
						_movingFromTile = false;
						break;
					}
					OnTraversalFailedAndRetrying?.Invoke(details.TargetPosition);
				}
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

		private void AttemptStrategies(List<TraversalStrat> strategies, Vector3Int target)
		{
			foreach (var strat in strategies)
			{
				var check = strat.ObsticalCheck(target, Mob);
				if (check.Item1)
				{
					strat.TraverseObstical(target, check.Item2, check.Item3, Mob);
					return;
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
			_movingFromTile = false;
			waitTicks = 0;
			if (clearPath) path.Clear();
		}

		private void SetMovingToTileToFalse(Vector3Int arg0, Vector3Int vector3Int)
		{
			_movingFromTile = false;
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
			public List<TraversalStrat> Strats;
		}
	}
}