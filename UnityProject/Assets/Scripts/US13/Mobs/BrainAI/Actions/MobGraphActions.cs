using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using Util;
using US13.Mobs.BrainAI.ContextExtensions;
using US13.Mobs.BrainAI.SettingsExtensions;

namespace US13.Mobs.BrainAI.Actions
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "FindMobTaskTarget")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Find Mob Task Target", story: "Find a task target", category: "Mob/Targeting", id: "ad1dc1c1a2b34c5d8000000000000003")]
	[RequiresMobContextExtension(typeof(TaskContextExtension))]
	[RequiresMobSettings(typeof(BotTaskSettings))]
	public partial class FindMobTaskTarget : BrainMobState
	{
		protected override Status OnStart()
		{
			if (!CanAct || !AI.TryGetSettings<BotTaskSettings>(out _) || !Context.TryGetExtension<TaskContextExtension>(out var tasks) || tasks.Task == null || Time.time < tasks.NextSearchTime || Context.Health.IsCrit || Context.Health.IsSoftCrit) return Status.Failure;
			Context.ClearTarget();
			Context.TargetRange = tasks.Task.Range;
			int radius = tasks.NearbySearch ? (AI.GetSettings<BotTaskSettings>().Kind == MobTaskKind.Security ? 2 : 1) : tasks.Task.SearchRadius;
			tasks.NearbySearch = false;
			if (tasks.Task.FindTarget(Context, radius))
			{
				MobDialogue.Speak(AI, AI.GetSettings<BotTaskSettings>().FoundDialogue);
				return Status.Success;
			}
			tasks.NextSearchTime = Time.time + AI.Settings.SearchInterval;
			if (DMMath.Prob(AI.GetSettings<BotTaskSettings>().DialogueChancePercent)) MobDialogue.Speak(AI, AI.IsEmagged ? AI.GetSettings<BotTaskSettings>().IdleEmaggedDialogue : AI.GetSettings<BotTaskSettings>().IdleDialogue);
			return Status.Failure;
		}
	}
}
