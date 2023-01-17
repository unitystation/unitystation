using System.Collections.Generic;
using System.Diagnostics;
using Shared.Managers;

namespace Managers
{
	public class SubsystemBehaviourQueueInit : SingletonManager<SubsystemBehaviourQueueInit>
	{
		private readonly List<SubsystemBehaviour> behaviours = new List<SubsystemBehaviour>();

		public static void Queue(SubsystemBehaviour behaviour)
		{
			Instance.behaviours.Add(behaviour);
		}

		public static void InitAllSystems()
		{
			var watch = new Stopwatch();
			watch.Start();
			Chat.AddGameWideSystemMsgToChat($"<color=blue>Initialising {Instance.behaviours.Count} subsystems..</color>");
			foreach (var behaviour in Instance.behaviours)
			{
				behaviour.Initialize();
			}
			watch.Stop();
			Chat.AddGameWideSystemMsgToChat($"<color=green>Subsystems loaded! Only took {watch.Elapsed.Seconds} seconds.</color>");
		}
	}
}