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

		private StationObjective activeObjective;

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
			activeObjective = stationObjectiveData.GetRandomObjective();
			activeObjective.Setup();
			GameManager.Instance.CentComm.MakeCommandReport(activeObjective.description, false);
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
			if (activeObjective == null)
			{
				return "Error: Status not found :S";
			}
			var complete = activeObjective.CheckCompletion();
			stringBuilder.Append($"{activeObjective.GetRoundEndReport()}\n");
			stringBuilder.AppendLine(complete ? "<color=green><b>Completed</b></color>" : "<color=red><b>Failed</b></color>");
			return stringBuilder.ToString();
		}

		private string GetObjectiveStatusNonRich()
		{
			var message = $"Status of {MatrixManager.MainStationMatrix.GameObject.scene.name}:\n";
			if (activeObjective != null)
			{

				bool complete = false;

				complete = activeObjective.CheckCompletion();
				message += $"{activeObjective.GetRoundEndReport()}";
				message += complete ? "Completed\n" : "Failed\n";
				return message;
			}
			return message + " Did not have any active objectives ";
		}

		private void ResetObjectives()
		{
			activeObjective = null;
		}
	}
}
