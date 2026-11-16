using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using US13.Core;
using US13.Health.Objects;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Mobs.BrainAI.ContextExtensions;
using Util;

namespace US13.Mobs.BrainAI.Tasks
{
	public sealed class MedicalMobTask : MobTask, IAsyncMobTask
	{
		private TaskContextExtension selectedContext;
		private object treatmentOwner;

		private static bool Eligible(MobBehaviorContext context, LivingHealthMasterBase living)
		{
			return IsAllowedLiving(context, living) && !living.IsDead &&
			       (context.AI.IsEmagged ? living.GetBruteBurnTotal() <= -100f : living.GetBruteBurnTotal() > 0f);
		}

		public override bool FindTarget(MobBehaviorContext context, int radius)
		{
			Cancel();
			if (!context.TryGetExtension<TaskContextExtension>(out var tasks)) return false;
			tasks.TargetHealth = null;
			var targets = ComponentsTracker<LivingHealthMasterBase>.GetAllNearbyTypesToTarget(context.AI.Body.gameObject, radius, bypassInventories: false);
			if (targets == null) return false;
			var matrix = MatrixOf(context);
			foreach (var living in targets)
			{
				if (Eligible(context, living) && Select(context, living.gameObject.AssumedWorldPosServer().ToLocalInt(matrix), matrix, living.gameObject))
				{
					tasks.TargetHealth = living;
					selectedContext = tasks;
					return true;
				}
			}
			return false;
		}

		public override bool IsValid(MobBehaviorContext context)
		{
			return context.TryGetExtension<TaskContextExtension>(out var tasks) && tasks.TargetHealth != null &&
			       context.Target == tasks.TargetHealth.gameObject && IsAllowedLiving(context, tasks.TargetHealth) && !tasks.TargetHealth.IsDead;
		}

		public async UniTask ExecuteAsync(MobBehaviorContext context, CancellationToken cancellationToken)
		{
			if (!IsValid(context)) throw new InvalidOperationException("Medical task requires a cached living target.");
			context.TryGetExtension<TaskContextExtension>(out var tasks);
			var target = tasks.TargetHealth;
			var owner = new object();
			selectedContext = tasks;
			treatmentOwner = owner;
			int generation = context.Generation;
			try
			{
				while (true)
				{
					CheckCurrentTreatment();
					if (RecoveredOrMovedAway()) return;
					if (context.AI.IsEmagged)
						target.ApplyDamageToRandomBodyPart(context.AI.Body.gameObject, 5f, AttackType.Melee, DamageType.Brute);

					float deadline = Time.time + Mathf.Max(0.02f, context.AI.Settings.Task.Duration);
					await UniTask.WaitUntil(() => !IsCurrentTreatment() || RecoveredOrMovedAway() || Time.time >= deadline,
						cancellationToken: cancellationToken);
					CheckCurrentTreatment();
					if (RecoveredOrMovedAway()) return;
					if (context.AI.IsEmagged == false)
					{
						target.HealDamageOnAll(context.AI.Body.gameObject, 2f, DamageType.Brute);
						CheckCurrentTreatment();
						if (RecoveredOrMovedAway()) return;
						target.HealDamageOnAll(context.AI.Body.gameObject, 2f, DamageType.Burn);
					}
				}
			}
			finally
			{
				// Old continuations must not clear a patient selected by a newer run.
				if (ReferenceEquals(treatmentOwner, owner))
				{
					if (ReferenceEquals(tasks.TargetHealth, target)) tasks.TargetHealth = null;
					selectedContext = null;
					treatmentOwner = null;
				}
			}

			bool IsCurrentTreatment() => ReferenceEquals(treatmentOwner, owner) && context.Generation == generation &&
				ReferenceEquals(tasks.TargetHealth, target) && context.AI.Body != null && context.AI.Enabled &&
				!context.AI.IsControlledByPlayer && context.Health?.IsDead != true && context.Health?.IsCrit != true &&
				context.Health?.IsSoftCrit != true && IsValid(context);
			void CheckCurrentTreatment()
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (!IsCurrentTreatment()) throw new OperationCanceledException("Medical target or brain execution changed.", cancellationToken);
			}
			bool RecoveredOrMovedAway() => (!context.AI.IsEmagged && target.GetBruteBurnTotal() <= 0f) ||
				Vector3.Distance(target.gameObject.AssumedWorldPosServer(), context.AI.Body.gameObject.AssumedWorldPosServer()) > Range;
		}

		public override void Complete(MobBehaviorContext context)
		{
			if (context.TryGetExtension<TaskContextExtension>(out var tasks)) tasks.TargetHealth = null;
		}

		public override void Cancel()
		{
			if (selectedContext != null) selectedContext.TargetHealth = null;
			selectedContext = null;
			treatmentOwner = null;
		}
	}
}
