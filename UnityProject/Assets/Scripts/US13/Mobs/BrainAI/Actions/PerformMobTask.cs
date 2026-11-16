using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using US13.Mobs.BrainAI.ContextExtensions;
using US13.Mobs.BrainAI.SettingsExtensions;

namespace US13.Mobs.BrainAI.Actions
{
	[MovedFrom(true, sourceNamespace: "US13.Mobs.BrainAI", sourceAssembly: "Assets", sourceClassName: "PerformMobTask")]
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Perform Mob Task", story: "Perform the selected task", category: "Mob/Tasks", id: "ad1dc1c1a2b34c5d8000000000000005")]
	[RequiresMobContextExtension(typeof(TaskContextExtension))]
	[RequiresMobSettings(typeof(BotTaskSettings))]
	public partial class PerformMobTask : BrainMobState
	{
		private MobTaskExecution execution;
		private bool active;
		protected override Status OnStart()
		{
			if (!CanAct || !AI.TryGetSettings<BotTaskSettings>(out _) || !Context.TryGetExtension<TaskContextExtension>(out var tasks) || tasks.Task == null || Context.Health.IsCrit || Context.Health.IsSoftCrit || !tasks.Task.IsValid(Context)) return Status.Failure;
			active = true;
			AI.SetPerformingTask(true);
			MobDialogue.Speak(AI, AI.GetSettings<BotTaskSettings>().EnterDialogue);
			MobDialogue.Sound(AI, AI.IsEmagged ? AI.GetSettings<BotTaskSettings>().EmaggedPerformSound : AI.GetSettings<BotTaskSettings>().PerformSound);
			execution = new MobTaskExecution(Context, Time.time);
			return execution.Status;
		}
		protected override Status OnUpdate()
		{
			if (!CanAct || Context.Health.IsCrit || Context.Health.IsSoftCrit) return Status.Failure;
			return execution.Tick(Time.time);
		}
		protected override void OnEnd()
		{
			if (!active) return;
			active = false;
			execution?.Cancel();
			execution = null;
			AI?.SetPerformingTask(false);
			if (CanAct) MobDialogue.Speak(AI, AI.GetSettings<BotTaskSettings>().ExitDialogue);
		}
	}
}
