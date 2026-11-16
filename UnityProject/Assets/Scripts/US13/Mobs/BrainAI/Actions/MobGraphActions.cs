using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using Util;
using US13.Mobs.BrainAI.ContextExtensions;

namespace US13.Mobs.BrainAI.Actions
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "FindMobTaskTarget")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Find Mob Task Target", story: "Find a task target", category: "Mob/Targeting", id: "ad1dc1c1a2b34c5d8000000000000003")]
	[RequiresMobContextExtension(typeof(TaskContextExtension))]
	public partial class FindMobTaskTarget : BrainMobState
	{
		protected override Status OnStart()
		{
			if (!CanAct || !Context.TryGetExtension<TaskContextExtension>(out var tasks) || tasks.Task == null || Time.time < tasks.NextSearchTime || Context.Health.IsCrit || Context.Health.IsSoftCrit) return Status.Failure;
			Context.ClearTarget();
			Context.TargetRange = tasks.Task.Range;
			int radius = tasks.NearbySearch ? (AI.Settings.Task.Kind == MobTaskKind.Security ? 2 : 1) : tasks.Task.SearchRadius;
			tasks.NearbySearch = false;
			if (tasks.Task.FindTarget(Context, radius))
			{
				MobDialogue.Speak(AI, AI.Settings.Task.FoundDialogue);
				return Status.Success;
			}
			tasks.NextSearchTime = Time.time + AI.Settings.SearchInterval;
			if (DMMath.Prob(AI.Settings.Task.DialogueChancePercent)) MobDialogue.Speak(AI, AI.IsEmagged ? AI.Settings.Task.IdleEmaggedDialogue : AI.Settings.Task.IdleDialogue);
			return Status.Failure;
		}
	}
}
