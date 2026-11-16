using System.Collections.Generic;
using UnityEngine;
using US13.HealthV2.Living;
using US13.Mobs.Traversal;
using US13.Tilemaps.Behaviours.Layers;
using Util;

namespace US13.Mobs.BrainAI.Tasks
{
	/// <summary>Task selection and effects, independent of graph flow, movement, dialogue and timing.</summary>
	public abstract class MobTask
	{
		public virtual int SearchRadius => 5;
		public virtual float Range => 1.5f;
		public abstract bool FindTarget(MobBehaviorContext context, int radius);
		public virtual bool IsValid(MobBehaviorContext context) => context.TargetMatrix != null && (!context.TargetWasObject || context.Target != null) &&
			Vector3.Distance(context.TargetCell.ToWorld(context.TargetMatrix), context.AI.Body.gameObject.AssumedWorldPosServer()) <= Range;
		public virtual float Begin(MobBehaviorContext context) => context.AI.Settings.Task.Duration;
		public virtual void Tick(MobBehaviorContext context, float elapsed) { }
		public abstract void Complete(MobBehaviorContext context);
		public virtual void Cancel() { }
		public static MobTask Create(MobTaskKind kind) => kind switch
		{
			MobTaskKind.Clean => new CleanMobTask(), MobTaskKind.Floor => new FloorMobTask(),
			MobTaskKind.Medical => new MedicalMobTask(), MobTaskKind.Fire => new FireMobTask(),
			MobTaskKind.Security => new SecurityMobTask(), _ => null
		};
		public static bool Select(MobBehaviorContext context, Vector3Int cell, Matrix matrix, GameObject target = null)
		{
			if (matrix == null) return false;
			var position = context.AI.Body.gameObject.AssumedWorldPosServer();
			bool inRange = Vector3.Distance(position, cell.ToWorld(matrix)) <= context.TargetRange;
			var path = inRange ? new List<Vector3Int>() : MobTraversal.GeneratePath(position.ToLocalInt(matrix), cell, matrix, PathfinderType.AStar);
			if (!inRange && (path == null || path.Count == 0)) return false;
			context.Target = target;
			context.TargetWasObject = target != null;
			context.TargetCell = cell;
			context.TargetMatrix = matrix;
			context.Path = path;
			return true;
		}
		protected static IEnumerable<Vector3Int> Cells(MobBehaviorContext context, int radius)
		{
			var position = context.AI.Body.RegisterTile.LocalPositionServer;
			for (int y = -radius; y <= radius; y++)
			for (int x = -radius; x <= radius; x++) yield return position + new Vector3Int(x, y, 0);
		}
		protected static Matrix MatrixOf(MobBehaviorContext context) => context.AI.Body.RegisterTile.Matrix;
		protected static bool IsAllowedLiving(MobBehaviorContext context, LivingHealthMasterBase living) =>
			living != null && living != context.Health && !context.AI.Settings.Task.BlacklistedSpecies.Contains(living.InitialSpecies);
	}
}
