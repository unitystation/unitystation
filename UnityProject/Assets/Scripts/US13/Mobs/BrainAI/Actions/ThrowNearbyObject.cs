using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using US13.Core;
using US13.Core.Physics;
using US13.Items;
using US13.Managers;
using Util;
using US13.Mobs.BrainAI.SettingsExtensions;

namespace US13.Mobs.BrainAI.Actions
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "ThrowNearbyObject")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Throw Nearby Object", story: "Hold and throw a nearby object at the target", category: "Mob/Tasks", id: "ad1dc1c1a2b34c5d8000000000000025")]
	[RequiresMobSettings(typeof(ArialBehaviorSettings))]
	public partial class ThrowNearbyObject : BrainMobState
	{
		private bool active;
		private UniversalObjectPhysics item;
		private float until;
		private float nextTick;
		private int generation;
		protected override Status OnStart()
		{
			if (!CanAct || !AI.TryGetSettings<ArialBehaviorSettings>(out _) || Context.Target == null) return Status.Failure;
			item = null;
			if (DMMath.Prob(1.25f)) item = PlayerList.Instance.GetAlivePlayers().PickRandom()?.Script?.playerMove;
			if (item == null)
			{
				var objects = ComponentsTracker<ItemAttributesV2>.GetAllNearbyTypesToTarget(AI.Body.gameObject, 9, false);
				if (objects != null && objects.Count > 0) item = objects.PickRandom()?.GetComponent<UniversalObjectPhysics>();
			}
			if (item == null || item.gameObject == AI.Body.gameObject) return Status.Failure;
			active = true;
			generation = Context.Generation;
			until = Time.time + 5;
			nextTick = 0;
			MobDialogue.Sound(AI, AI.GetSettings<ArialBehaviorSettings>().ThrowEnterSound);
			return Status.Running;
		}
		protected override Status OnUpdate()
		{
			if (!CanAct || Context.Generation != generation || Context.Target == null || item == null) return Status.Failure;
			if (Time.time < nextTick) return Status.Running;
			nextTick = Time.time + Mathf.Max(0.02f, AI.GetSettings<ArialBehaviorSettings>().ThrowInterval);
			item.SetTransform(AI.Body.gameObject.AssumedWorldPosServer(), true);
			if (Time.time < until) return Status.Running;
			item.NewtonianPush((Context.Target.AssumedWorldPosServer() - item.gameObject.AssumedWorldPosServer()).normalized, 35);
			return Status.Success;
		}
		protected override void OnEnd()
		{
			item = null;
			if (!active) return;
			active = false;
			if (CanAct) MobDialogue.Sound(AI, AI.GetSettings<ArialBehaviorSettings>().ThrowExitSound);
		}
	}
}
