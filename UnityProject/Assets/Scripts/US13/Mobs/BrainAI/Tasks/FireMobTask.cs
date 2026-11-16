using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using US13.Core;
using US13.Health.Objects;
using US13.HealthV2.Living;
using US13.Managers.MatrixManager;
using US13.Tilemaps.Behaviours.Layers;
using Util;
using Effect = US13.Effects.Effect;

namespace US13.Mobs.BrainAI.Tasks
{
	public sealed class FireMobTask : MobTask
	{
		public override float Range => 3f;
		private readonly List<Vector3Int> sprayTiles = new();
		private int sprayed;
		private ReagentMix mix;

		public override bool FindTarget(MobBehaviorContext context, int radius)
		{
			Matrix matrix = MatrixOf(context);
			if (context.AI.IsEmagged)
			{
				List<LivingHealthMasterBase> targets =
					ComponentsTracker<LivingHealthMasterBase>.GetAllNearbyTypesToTarget(context.AI.Body.gameObject,
						radius, false);
				if (targets == null) return false;
				foreach (LivingHealthMasterBase living in targets)
				{
					if (IsAllowedLiving(context, living) && !(living.FireStacks > 10 && living.OverallHealth > -10) &&
					    Select(context, living.gameObject.AssumedWorldPosServer().ToLocalInt(matrix), matrix,
						    living.gameObject))
					{
						return true;
					}
				}
				return false;
			}

			List<Flammable> flammables = ComponentsTracker<Flammable>.GetAllNearbyTypesToTarget(context.AI.Body.gameObject, radius, false);
			if (flammables != null)
			{
				foreach (Flammable flammable in flammables)
				{
					if (flammable != null && flammable.IsOnFire && Select(context,
						    flammable.gameObject.AssumedWorldPosServer().ToLocalInt(matrix), matrix,
						    flammable.gameObject))
					{
						return true;
					}
				}
			}
			foreach (Vector3Int cell in Cells(context, radius))
			{
				if (matrix.ReactionManager != null && matrix.ReactionManager.HasHotspot(cell) && Select(context, cell, matrix))
				{
					return true;
				}
			}
			return false;
		}

		public override float Begin(MobBehaviorContext context)
		{
			Cancel();
			BotTaskSettings settings = context.AI.Settings.Task;
			mix = context.AI.IsEmagged ? settings.EmaggedChemicalMix : settings.FireRetardantMix;
			Vector3Int start = context.AI.Body.gameObject.AssumedWorldPosServer().RoundToInt();
			Vector3Int end = context.TargetCell.ToWorldInt(context.TargetMatrix);
			Vector2 relative = (end - start).To2().normalized;
			Effect.PlayParticleDirectional(context.AI.Body.gameObject, relative);
			Vector2 perpendicular = Vector2.Perpendicular(relative);
			for (int lane = -1; lane <= 1; lane++)
			{
				Vector3Int offset = (perpendicular * lane).RoundTo2Int().To3Int();
				foreach (Vector3Int tile in MatrixManager.GetTiles(start + offset, (end + offset).To2Int(),
					         Mathf.Max(1, settings.SprayDistance)))
				{
					if (!MatrixManager.IsAtmosPassableAt(tile, true)) break;
					sprayTiles.Add(tile);
				}
			}

			Tick(context, 0);
			return Mathf.Max(settings.Duration, (float)sprayTiles.Count / Mathf.Max(1, settings.SprayDistance));
		}

		public override void Tick(MobBehaviorContext context, float elapsed)
		{
			int due = Mathf.Min(sprayTiles.Count,
				1 + Mathf.FloorToInt(elapsed * Mathf.Max(1, context.AI.Settings.Task.SprayDistance)));
			while (sprayed < due)
			{
				if (mix != null && context.TargetMatrix != null)
					MatrixManager.ReagentReact(mix.Clone(), sprayTiles[sprayed], context.TargetMatrix.MatrixInfo);
				sprayed++;
			}
		}

		public override void Complete(MobBehaviorContext context)
		{
		}

		public override void Cancel()
		{
			sprayTiles.Clear();
			sprayed = 0;
			mix = null;
		}
	}
}