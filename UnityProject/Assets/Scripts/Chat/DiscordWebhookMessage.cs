using System.Collections.Specialized;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Text.RegularExpressions;
using DatabaseAPI;
using System.Collections;
using Newtonsoft.Json;

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
		private Queue<string> ErrorLogMessageQueue = new Queue<string>();
		private HashSet<string> ErrorMessageHashSet = new HashSet<string>();

		private Dictionary<Queue<string>, string> discordWebhookURLQueueDict = null;
		private float spamPreventionTimer = 0f;
		private bool spamPrevention = false;
		private float sendingTimer = 0;
		private const float SpamTimeLimit = 150f;
		private const float MessageTimeDelay = 1.5f;

		private bool messageSendingInProgress = false;

		IList<string> RoleList = new List<string>();

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

			sendingTimer += Time.deltaTime;
			if (sendingTimer > MessageTimeDelay)
			{
				if (discordWebhookURLQueueDict == null)
				{
					InitDict();
				}

				if (!messageSendingInProgress)
				{
					messageSendingInProgress = true;

					_ = StartCoroutine(SendQueuedMessagesToWebhooks());
				}

				sendingTimer = 0;
			}

			if (!spamPrevention) return;

			spamPreventionTimer += Time.deltaTime;
			if (spamPreventionTimer > SpamTimeLimit)
			{
				spamPrevention = false;

				spamPreventionTimer = 0f;
			}
		}

		void OnEnable()
		{
			Application.logMessageReceived += HandleLog;
			EventManager.AddHandler(EVENT.PreRoundStarted, ResetHashSet);
		}

		void OnDisable()
		{
			Application.logMessageReceived -= HandleLog;
			EventManager.RemoveHandler(EVENT.PreRoundStarted, ResetHashSet);
		}

		void ResetHashSet()
		{
			ErrorMessageHashSet.Clear();
		}

		private IEnumerator SendQueuedMessagesToWebhooks()
		{
			foreach (var entry in discordWebhookURLQueueDict)
			{
				FormatAndSendMessage(entry.Value, entry.Key);
			}

			messageSendingInProgress = false;

			yield break;
		}

		private void InitDict()
		{
			discordWebhookURLQueueDict = new Dictionary<Queue<string>, string>
			{
				{OOCMessageQueue, ServerData.ServerConfig.DiscordWebhookOOCURL},
				{AdminAhelpMessageQueue, ServerData.ServerConfig.DiscordWebhookAdminURL},
				{AnnouncementMessageQueue, ServerData.ServerConfig.DiscordWebhookAnnouncementURL},
				{AllChatMessageQueue, ServerData.ServerConfig.DiscordWebhookAllChatURL},
				{AdminLogMessageQueue, ServerData.ServerConfig.DiscordWebhookAdminLogURL},
				{ErrorLogMessageQueue, ServerData.ServerConfig.DiscordWebhookErrorLogURL}
			};
		}

		private void Post(string url, JsonPayloadContent playload)
		{
			using (WebClient webClient = new WebClient())
			{
				var dataString = JsonConvert.SerializeObject(playload);
				webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json");
				webClient.UploadString(url, dataString);
			}
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
				//Discord character limit is 2000
				if (msg.Length > 1970)
				{
					msg = msg.Substring(0, 1970);
					break;
				}

				if (msg.Length + queue.Peek().Length > 1970)
				{
					break;
				}

				msg += queue.Dequeue() + "\n";
			}

			var payLoad = new JsonPayloadContent()
			{
				content = msg,

				allowed_mentions =
				{
					roles = RoleList
				}
			};

			Post(url, payLoad);
		}

		private string MsgMentionProcess(string msg, string mentionID = null)
		{
			var newmsg = msg;

			//Disable \ and \n
			newmsg = Regex.Replace(newmsg, @"\\n?", " ");

			//Disable links
			newmsg = Regex.Replace(newmsg, "(?i)http", " ");

			if (!string.IsNullOrEmpty(mentionID) && !spamPrevention)
			{
				if (!RoleList.Contains(mentionID))
				{
					RoleList.Add(mentionID);
				}

				mentionID = $"<@&{mentionID}>";

				//Replaces the @ServerAdmin (non case sensitive), with the discord role ID, so it pings.
				newmsg = Regex.Replace(newmsg, "(?i)@ServerAdmin", mentionID);

				spamPrevention = true;
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
				case DiscordWebhookURLs.DiscordWebhookErrorLogURL:
					return (ServerData.ServerConfig.DiscordWebhookErrorLogURL, ErrorLogMessageQueue);
				default:
					return (null, null);
			}
		}

		void HandleLog(string logString, string stackTrace, LogType type)
		{
			if ((type == LogType.Exception || type == LogType.Error) && !ErrorMessageHashSet.Contains(stackTrace))
			{
				ErrorMessageHashSet.Add(stackTrace);

				if(logString.Contains("Can't get home directory!")) return;

				var logToSend = $"{logString}\n{stackTrace}";

				//Discord character limit is 2000
				if (logToSend.Length > 1950)
				{
					logToSend = logToSend.Substring(0, 1950);
				}

				logToSend = $"```\n{logToSend}\n```\n";

				AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookErrorLogURL, logToSend, "");
			}
		}
	}

	public enum DiscordWebhookURLs
	{
		DiscordWebhookOOCURL,
		DiscordWebhookAdminURL,
		DiscordWebhookAnnouncementURL,
		DiscordWebhookAllChatURL,
		DiscordWebhookAdminLogURL,
		DiscordWebhookErrorLogURL
	}

	public class AllowedMentions
	{
		public IList<string> roles { get; set; }
	}

	public class JsonPayloadContent
	{
		public string content { get; set; }

		public AllowedMentions allowed_mentions = new AllowedMentions();
	}
}