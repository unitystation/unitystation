using UnityEngine;
using US13.Core;
using US13.Health.Objects;
using US13.HealthV2;
using US13.HealthV2.Living;
using Util;

namespace US13.Mobs.BrainAI.Tasks
{
	public sealed class MedicalMobTask : MobTask
	{
		private static bool Eligible(MobBehaviorContext context, LivingHealthMasterBase living)
		{
			return IsAllowedLiving(context, living) && (context.AI.IsEmagged ? living.GetBruteBurnTotal() <= -100f : living.GetBruteBurnTotal() > 0f);
		}

		public override bool FindTarget(MobBehaviorContext context, int radius)
		{
			var targets = ComponentsTracker<LivingHealthMasterBase>.GetAllNearbyTypesToTarget(context.AI.Body.gameObject, radius, bypassInventories: false);
			if (targets == null) return false;
			var matrix = MatrixOf(context);
			foreach (var living in targets)
				if (Eligible(context, living) && Select(context, living.gameObject.AssumedWorldPosServer().ToLocalInt(matrix), matrix, living.gameObject)) return true;
			return false;
		}

		public override bool IsValid(MobBehaviorContext context)
		{
			return context.Target != null
			       &&
			       Eligible(context, context.Target.GetCachedComponent<LivingHealthMasterBase>())
			       &&
			       Vector3.Distance(context.Target.AssumedWorldPosServer(), context.AI.Body.gameObject.AssumedWorldPosServer()) <= Range;
		}

		public override float Begin(MobBehaviorContext context)
		{
			if (context.AI.IsEmagged)
			{
				context.Target.GetCachedComponent<LivingHealthMasterBase>()
					.ApplyDamageToRandomBodyPart(context.AI.Body.gameObject, 5f, AttackType.Melee, DamageType.Brute);
			}
			return base.Begin(context);
		}

		public override void Complete(MobBehaviorContext context)
		{
			if (context.AI.IsEmagged) return;
			var living = context.Target.GetComponent<LivingHealthMasterBase>();
			living.HealDamageOnAll(context.AI.Body.gameObject, 2f, DamageType.Brute);
			living.HealDamageOnAll(context.AI.Body.gameObject, 2f, DamageType.Burn);
		}
	}
}