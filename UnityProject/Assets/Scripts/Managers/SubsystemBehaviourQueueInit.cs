using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Shared.Managers;

namespace Managers
{
	public class SubsystemBehaviourQueueInit : SingletonManager<SubsystemBehaviourQueueInit>
	{
		private List<SubsystemBehaviour> behaviours = new List<SubsystemBehaviour>();

		public static bool InitializedAll = false;

		public static void Queue(SubsystemBehaviour behaviour)
		{
			Instance.behaviours.Add(behaviour);
		}

		public static void InitAllSystems()
		{
			var watch = new Stopwatch();
			watch.Start();
			Chat.AddGameWideSystemMsgToChat($"<color=blue>Initialising {Instance.behaviours.Count} subsystems..</color>");
			Instance.behaviours = Instance.behaviours.OrderByDescending(s => s.Priority).ToList();
			try
			{
				foreach (var behaviour in Instance.behaviours)
				{
					behaviour.Initialize();
				}
			}
			catch (Exception e)
			{
				Logger.LogError($"[SubsystemBehaviourQueueInit] - INIT FAIL! {e}");
				watch.Stop();
				Chat.AddGameWideSystemMsgToChat($"<color=red>Encountered an error! " +
				                                $"Subsystems failed after {watch.Elapsed.Seconds} seconds. " +
				                                $"Game will not function properly.</color>");
				InitializedAll = true;
				return;
			}
			watch.Stop();
			Chat.AddGameWideSystemMsgToChat($"<color=green>Subsystems loaded! Only took {watch.Elapsed.Seconds} seconds.</color>");
			InitializedAll = true;
		}
	}
}