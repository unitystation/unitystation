using System.Collections.Specialized;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Text.RegularExpressions;
using DatabaseAPI;


namespace DiscordWebhook
{
	/// <summary>
	/// Used to send messages to a discord webhook URLs, URLs need to be set up in the config.json file. Supports OOC, Ahelp, Announcements and All chat.
	/// </summary>
	public class DiscordWebhookMessage : MonoBehaviour
	{
		private static DiscordWebhookMessage instance;
		public static DiscordWebhookMessage Instance => instance;

		private Queue<string> OOCMessageQueue = new Queue<string>();
		private Queue<string> AdminAhelpMessageQueue = new Queue<string>();
		private Queue<string> AnnouncementMessageQueue = new Queue<string>();
		private Queue<string> AllChatMessageQueue = new Queue<string>();
		private Queue<string> AdminLogMessageQueue = new Queue<string>();

		private Dictionary<Queue<string>, string> DiscordWebhookURLQueueDict = null;
		private float SpamPreventionTimer = 0f;
		private bool SpamPrevention = false;
		private float SendingTimer = 0;
		private const float SpamTimeLimit = 150f;
		private const float MessageTimeDelay = 1f;

		private void Awake()
		{
			if (instance == null)
			{
				instance = this;
			}
			else
			{
				Destroy(this);
			}
		}

		private void Update()
		{
			if (!CustomNetworkManager.IsServer) return;

			SendingTimer += Time.deltaTime;
			if (SendingTimer > MessageTimeDelay)
			{
				if (DiscordWebhookURLQueueDict == null)
				{
					InitDict();
				}

				foreach (var entry in DiscordWebhookURLQueueDict)
				{
					FormatAndSendMessage(entry.Value, entry.Key);
				}

				SendingTimer --;
			}

			if (!SpamPrevention) return;

			SpamPreventionTimer += Time.deltaTime;
			if (SpamPreventionTimer > SpamTimeLimit)
			{
				SpamPrevention = false;

				SpamPreventionTimer = 0f;
			}
		}

		private void InitDict()
		{
			DiscordWebhookURLQueueDict = new Dictionary<Queue<string>, string>
			{
				{OOCMessageQueue, ServerData.ServerConfig.DiscordWebhookOOCURL},
				{AdminAhelpMessageQueue, ServerData.ServerConfig.DiscordWebhookAdminURL},
				{AnnouncementMessageQueue, ServerData.ServerConfig.DiscordWebhookAnnouncementURL},
				{AllChatMessageQueue, ServerData.ServerConfig.DiscordWebhookAllChatURL},
				{AdminLogMessageQueue, ServerData.ServerConfig.DiscordWebhookAdminLogURL}
			};
		}

		private void Post(string url, NameValueCollection pairs)
		{
			using (WebClient webClient = new WebClient())

			webClient.UploadValues(url, pairs);
		}

		public void AddWebHookMessageToQueue(DiscordWebhookURLs urlToUse, string msg, string username, string mentionID = null)
		{
			var urlAndQueue = GetUrl(urlToUse);

			if (string.IsNullOrEmpty(urlAndQueue.Item1))
			{
				return;
			}

			msg = MsgMentionProcess(msg, mentionID);

			if (username == null)
			{
				username = "Spectator";
			}

			if (username != "")
			{
				msg = $"{username}:  " + msg;
			}

			urlAndQueue.Item2.Enqueue(msg);
		}

		private void FormatAndSendMessage(string url, Queue<string> queue)
		{
			if (url == null) return;

			var count = queue.Count;

			if (count == 0) return;

			var msg = "";

			for (var i = 1; i <= count; i++)
			{
				msg += queue.Dequeue() + "\n";
			}

			Post(url, new NameValueCollection()
			{
				{
					"content",
					msg
				}
			});
		}

		private string MsgMentionProcess(string msg, string mentionID = null)
		{
			var newmsg = msg;

			//Removes <@ to stop unwanted pings
			newmsg = Regex.Replace(newmsg, "<@", " ");

			if (!string.IsNullOrEmpty(mentionID) && !SpamPrevention)
			{
				//Replaces the @ServerAdmin (non case sensitive), with the discord role ID, so it pings.
				newmsg = Regex.Replace(newmsg, "(?i)@ServerAdmin", mentionID);
				SpamPrevention = true;
			}

			return newmsg;
		}

		private (string, Queue<string>) GetUrl(DiscordWebhookURLs url)
		{
			switch(url)
			{
				case DiscordWebhookURLs.DiscordWebhookOOCURL:
					return (ServerData.ServerConfig.DiscordWebhookOOCURL, OOCMessageQueue);
				case DiscordWebhookURLs.DiscordWebhookAdminURL:
					return (ServerData.ServerConfig.DiscordWebhookAdminURL, AdminAhelpMessageQueue);
				case DiscordWebhookURLs.DiscordWebhookAnnouncementURL:
					return (ServerData.ServerConfig.DiscordWebhookAnnouncementURL, AnnouncementMessageQueue);
				case DiscordWebhookURLs.DiscordWebhookAllChatURL:
					return (ServerData.ServerConfig.DiscordWebhookAllChatURL, AllChatMessageQueue);
				case DiscordWebhookURLs.DiscordWebhookAdminLogURL:
					return (ServerData.ServerConfig.DiscordWebhookAdminLogURL, AdminLogMessageQueue);
				default:
					return (null, null);
			}
		}
	}

	public enum DiscordWebhookURLs
	{
		DiscordWebhookOOCURL,
		DiscordWebhookAdminURL,
		DiscordWebhookAnnouncementURL,
		DiscordWebhookAllChatURL,
		DiscordWebhookAdminLogURL
	}
}