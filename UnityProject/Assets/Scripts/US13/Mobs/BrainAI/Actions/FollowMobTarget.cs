using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using US13.HealthV2.Living;
using US13.Mobs.Traversal;
using Util;

namespace US13.Mobs.BrainAI.Actions
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "FollowMobTarget")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Follow Mob Target", story: "Follow the selected target", category: "Mob/Movement", id: "ad1dc1c1a2b34c5d8000000000000022")]
	public partial class FollowMobTarget : BrainMobState
	{
		private float nextRepath;
		private bool moving;
		private bool active;
		private GameObject target;
		private bool HasTarget => target != null && target.activeInHierarchy &&
		                          (!target.TryGetComponent<LivingHealthMasterBase>(out var health) || !health.IsDead);
		protected override Status OnStart()
		{
			moving = false;
			if (!CanAct) return Status.Failure;
			target = Context.DesiredTarget;
			if (!HasTarget || !Context.CanFollowTarget || Context.Health.IsCrit || Context.Health.IsSoftCrit) return Status.Failure;
			active = true;
			MobDialogue.Sound(AI, AI.Settings.FollowEnterSound);
			nextRepath = 0;
			Repath();
			return Status.Running;
		}
		private void Repath()
		{
			if (Time.time < nextRepath) return;
			nextRepath = Time.time + Mathf.Max(0.02f, AI.Settings.FollowInterval);
			if (moving) Context.ReleaseMovement(this);
			moving = false;
			if (AI.Traversal != null && Vector3.Distance(target.AssumedWorldPosServer(), AI.Body.gameObject.AssumedWorldPosServer()) > AI.Settings.FollowDistance)
			{
				if (!Context.TryAcquireMovement(this)) return;
				var matrix = AI.Body.RegisterTile.Matrix;
				moving = AI.Traversal.QueueMovementGoal(target.AssumedWorldPosServer().ToLocalInt(matrix), strategies: AI.Settings.TraversalStrategies, algorithm: PathfinderType.BFS, cancelOnSlip: true);
				if (!moving) Context.ReleaseMovement(this);
			}
		}
		protected override Status OnUpdate()
		{
			if (!CanAct || !HasTarget || Context.DesiredTarget != target || !Context.CanFollowTarget || Context.Health.IsCrit || Context.Health.IsSoftCrit) return Status.Failure;
			Repath();
			return Status.Running;
		}
		protected override void OnEnd()
		{
			if (!active) return;
			active = false;
			Context?.ReleaseMovement(this);
			moving = false;
			if (CanAct) MobDialogue.Sound(AI, AI.Settings.FollowExitSound);
		}
	}
}
