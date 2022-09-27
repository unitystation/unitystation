using System.Collections;
using System.Text;
using UnityEngine;
using Managers;
using Strings;

namespace InGameEvents
{
	public class EventProcessorOverload : EventScriptBase
	{
		// Duplicates to change the weighting on random pick
		private static readonly string garbledChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ01234567890123456789!@#$%^&*()!@#$%^&*!@#$%^&*()";

		public override void OnEventStart()
		{
			if (AnnounceEvent)
			{
				var text = "Exospheric bubble inbound Processor overload is likely. Please contact you*%xp25)`6cq-BZZT";

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);
			}

			if (FakeEvent) return;

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			var numberOfServersToAffect = Random.Range(0, GameManager.Instance.CommsServers.Count);
			GameManager.Instance.CommsServers.Shuffle();
			for (int i = 0; i < numberOfServersToAffect; i++)
			{
				GameManager.Instance.CommsServers[i].OnEmp(Random.Range(75, GameManager.Instance.CommsServers[i].EmpResistence * 2));
			}
		}

		public static string ProcessMessage(string message)
		{
			StringBuilder newMessage = new StringBuilder(message.Length);
			foreach (char c in message)
			{
				if (char.IsWhiteSpace(c) || DMMath.Prob(50))
				{
					newMessage.Append(c);
				}
				else
				{
					newMessage.Append(garbledChars.PickRandom());
				}
			}

			return newMessage.ToString();
		}
	}
}
