using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;
using DatabaseAPI;


namespace DiscordWebhook
{
	public static class DiscordWebhookMessage
	{
		public static void Post(string url, NameValueCollection pairs)
		{
			using (WebClient webClient = new WebClient())

			webClient.UploadValues(url, pairs);
		}

		public static void SendWebHookMessage(DiscordWebhookURLs urlToUse, string msg, string username, string mentionID = null)
		{
			var url = GetUrl(urlToUse);

			if (string.IsNullOrEmpty(url))
			{
				return;
			}

			msg = MsgMentionProcess(msg, mentionID);

			if (username == null)
			{
				username = "Spectator";
			}

			Post(url, new NameValueCollection()
			{
				{
					"username",
					username
				},
				{
					"content",
					msg
				}
			});
		}

		public static string MsgMentionProcess(string msg, string mentionID = null)
		{
			var newmsg = msg;

			//Removes <@ to stop unwanted pings
			newmsg = Regex.Replace(newmsg, "<@", " ");

			if (!string.IsNullOrEmpty(mentionID))
			{
				//Replaces the @ServerAdmin (non case sensitive), with the discord role ID, so it pings.
				newmsg = Regex.Replace(newmsg, "(?i)@ServerAdmin", mentionID);
			}

			return newmsg;
		}

		public static string GetUrl(DiscordWebhookURLs url)
		{
			switch(url)
			{
				case DiscordWebhookURLs.DiscordWebhookOOCURL:
					return ServerData.ServerConfig.DiscordWebhookOOCURL;
				case DiscordWebhookURLs.DiscordWebhookAdminURL:
					return ServerData.ServerConfig.DiscordWebhookAdminURL;
				case DiscordWebhookURLs.DiscordWebhookAnnouncementURL:
					return ServerData.ServerConfig.DiscordWebhookAnnouncementURL;
				default:
					return null;
			}
		}
	}

	public enum DiscordWebhookURLs
	{
		DiscordWebhookOOCURL,
		DiscordWebhookAdminURL,
		DiscordWebhookAnnouncementURL
	}
}