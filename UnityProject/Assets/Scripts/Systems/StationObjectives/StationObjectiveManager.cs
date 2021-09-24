using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;
using DiscordWebhook;
using DatabaseAPI;
using Messages.Server.LocalGuiMessages;
using Strings;
using Managers;
using Random = UnityEngine.Random;

namespace StationObjectives
{
	public class StationObjectiveManager : MonoBehaviour
	{

		[SerializeField]
		[Tooltip("Stores all station objective data.")]
		private StationObjectiveData stationObjectiveData = null;

		public static StationObjectiveManager Instance;

		private StationObjective activeObjective;

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(gameObject);
			}
		}
		void OnEnable()
		{
			EventManager.AddHandler(Event.RoundEnded, ResetObjectives);
		}
		void OnDisable()
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
			StringBuilder statusSB = new StringBuilder($"<color=white><size=60><b>End of Round Report</b></size></color>\n\n", 200);

			var message = $"End of Round Report on {ServerData.ServerConfig.ServerName}\n";

			statusSB.AppendLine(GetObjectiveStatus());
			message += $"\n{GetObjectiveStatusNonRich()}";

			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAnnouncementURL, message, "");

			// Send the message
			Chat.AddGameWideSystemMsgToChat(statusSB.ToString());
		}

		private string GetObjectiveStatus()
		{
			var stringBuilder = new StringBuilder($"<color=blue>Objective of <b>{MatrixManager.MainStationMatrix.GameObject.scene.name}</b>:</color>\n", 200);
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
			var message = $"Objective of {MatrixManager.MainStationMatrix.GameObject.scene.name}:\n";
			var complete = activeObjective.CheckCompletion();
			message += $"{activeObjective.GetRoundEndReport()}";
			message += complete ? "Completed\n" : "Failed\n";
			return message;
		}

		private void ResetObjectives()
		{
			activeObjective = null;
		}
	}
}