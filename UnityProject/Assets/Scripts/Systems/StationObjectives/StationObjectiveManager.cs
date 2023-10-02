using System.Collections.Generic;
using System.Text;
using DatabaseAPI;
using DiscordWebhook;
using Shared.Managers;
using Strings;
using UnityEngine;

namespace StationObjectives
{
	public class StationObjectiveManager : SingletonManager<StationObjectiveManager>
	{
		[SerializeField]
		[Tooltip("Stores all station objective data.")]
		private StationObjectiveData stationObjectiveData = null;

		public List<StationObjective> ActiveObjective = new ();

		private void OnEnable()
		{
			EventManager.AddHandler(Event.RoundEnded, ResetObjectives);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.RoundEnded, ResetObjectives);
		}

		public void ServerChooseObjective()
		{
			var obj = stationObjectiveData.GetRandomObjective();
			ActiveObjective.Add(obj);
			obj.DoSetupStationObjective();
		}

		public void RemoveStationObjective(StationObjective toRemove)
		{
			if (ActiveObjective.Contains(toRemove))
			{
				toRemove.OnCanceling();
				ActiveObjective.Remove(toRemove);
			}
		}

		public void AddObjective(StationObjective toAdd)
		{
			ActiveObjective.Add(toAdd);
			toAdd.DoSetupStationObjective();
		}

		public void ShowStationStatusReport()
		{
			StringBuilder statusSB = new StringBuilder(
					$"<color=white><size={ChatTemplates.ExtremelyLargeText}><b>End of Round Report</b></size></color>\n\n", 200);

			var message = $"End of Round Report on {ServerData.ServerConfig.ServerName}\n";

			statusSB.AppendLine(GetObjectiveStatus());
			message += $"\n{GetObjectiveStatusNonRich()}";

			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAnnouncementURL, message, "");

			// Send the message
			Chat.AddGameWideSystemMsgToChat(statusSB.ToString());
		}

		private string GetObjectiveStatus()
		{
			var stringBuilder = new StringBuilder(
					$"<color={ChatTemplates.Blue}><size={ChatTemplates.LargeText}>" +
					$"Objective of <b>{MatrixManager.MainStationMatrix.GameObject.scene.name}</b>:</size></color>\n",
					200);
			if (ActiveObjective == null)
			{
				return "Error: Status not found :S";
			}
			foreach (var obj in ActiveObjective)
			{
				var complete = obj.CheckStationObjectiveCompletion();
				stringBuilder.Append($"{obj.GetRoundEndReport()}\n");
				stringBuilder.AppendLine(complete ? "<color=green><b>Completed</b></color>" : "<color=red><b>Failed</b></color>");
			}
			return stringBuilder.ToString();
		}

		private string GetObjectiveStatusNonRich()
		{
			var builder = new StringBuilder($"Status of {MatrixManager.MainStationMatrix.GameObject.scene.name}:\n");
			if (ActiveObjective != null)
			{
				bool complete = false;

				foreach (var obj in ActiveObjective)
				{
					complete = obj.CheckStationObjectiveCompletion();
					builder.Append($"{obj.GetRoundEndReport()}");
					builder.Append(complete ? "Completed\n" : "Failed\n");
				}
				return builder.ToString();
			}
			return builder + " Did not have any active objectives ";
		}

		private void ResetObjectives()
		{
			ActiveObjective.Clear();
		}
	}
}
