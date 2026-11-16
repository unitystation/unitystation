using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using US13.Mobs.Traversal;
using Util;

namespace US13.Mobs.BrainAI.Actions
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "MoveToMobTarget")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Move To Mob Target", story: "Move into target range", category: "Mob/Movement", id: "ad1dc1c1a2b34c5d8000000000000004")]
	public partial class MoveToMobTarget : BrainMobState
	{
		private bool active;
		private bool failed;
		private bool arrived;
		private bool hadObjectTarget;
		private GameObject target;
		private Tilemaps.Behaviours.Layers.Matrix matrix;
		private Vector3Int cell;
		private float range;
		private int generation;
		private float deadline;
		private MobTraversal traversal;
		private bool InRange => matrix != null && (!hadObjectTarget || target != null) && Vector3.Distance(AI.Body.gameObject.AssumedWorldPosServer(),
			target != null ? target.AssumedWorldPosServer() : cell.ToWorld(matrix)) <= range;
		protected override Status OnStart()
		{
			if (!CanAct || Context.TargetMatrix == null || Context.Health.IsCrit || Context.Health.IsSoftCrit) return Status.Failure;
			target = Context.Target;
			hadObjectTarget = target != null;
			matrix = Context.TargetMatrix;
			cell = Context.TargetCell;
			range = Context.TargetRange;
			if (InRange) return Status.Success;
			traversal = AI.Traversal;
			if (traversal == null || !Context.TryAcquireMovement(this)) return Status.Failure;
			generation = Context.Generation;
			failed = false;
			arrived = false;
			active = true;
			deadline = Time.time + 60;
			traversal.OnTraversalFailedCompletely += Failed;
			traversal.OnDoneTraversalToLocation += Arrived;
			var path = Context.Path;
			if (path == null) path = MobTraversal.GeneratePath(AI.Body.gameObject.AssumedWorldPosServer().ToLocalInt(Context.TargetMatrix), Context.TargetCell, Context.TargetMatrix, PathfinderType.AStar);
			if (path is { Count: > 1 }) path.RemoveAt(path.Count - 1);
			var details = new MobTraversal.TraversalDetails { TargetPosition = Context.TargetCell, TargetObject = Context.Target, Strats = AI.Settings.TraversalStrategies, CancelOnSlip = true, Algorithm = PathfinderType.AStar };
			if (!traversal.QueueMovementGoalFromPath(details, path)) failed = true;
			return Status.Running;
		}
		private void Failed(Vector3Int cell) { if (active && Context != null && Context.Generation == generation) failed = true; }
		private void Arrived(Vector3Int cell) { if (active && Context != null && Context.Generation == generation) arrived = true; }
		protected override Status OnUpdate()
		{
			if (!CanAct || Context.Generation != generation || (hadObjectTarget && target == null) || Context.Health.IsCrit || Context.Health.IsSoftCrit) return Status.Failure;
			if (InRange) return Status.Success;
			if (!failed && !arrived && Time.time < deadline) return Status.Running;
			Context.NotifyMovementFailed();
			return Status.Failure;
		}
		protected override void OnEnd()
		{
			if (!active) return;
			active = false;
			if (traversal != null)
			{
				traversal.OnTraversalFailedCompletely -= Failed;
				traversal.OnDoneTraversalToLocation -= Arrived;
			}
			Context?.ReleaseMovement(this);
			traversal = null;
		}
	}
}
