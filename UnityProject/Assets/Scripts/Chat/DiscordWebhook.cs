using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;
using DatabaseAPI;


namespace DiscordWebhook
{
	public static class DiscordWebhookMessage
	{
		public static byte[] Post(string url, NameValueCollection pairs)
		{
			using (WebClient webClient = new WebClient())
				return webClient.UploadValues(url, pairs);
		}

		public static void SendWebHookMessage(Urls urlToUse, string msg, string username, string mentionID = null)
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

			if (mentionID != null)
			{
				//Replaces the @ServerAdmin (non case sensitive), with the discord role ID, so it pings.
				newmsg = Regex.Replace(newmsg, "(?i)@ServerAdmin", mentionID);
			}

			return newmsg;
		}

		public static string GetUrl(Urls url)
		{
			if (url == Urls.DiscordWebhookOOCURL)
			{
				return ServerData.ServerConfig.DiscordWebhookOOCURL;
			}
			else if (url == Urls.DiscordWebhookAdminURL)
			{
				return ServerData.ServerConfig.DiscordWebhookAdminURL;
			}
			else if (url == Urls.DiscordWebhookAnnouncementURL)
			{
				return ServerData.ServerConfig.DiscordWebhookAnnouncementURL;
			}
			else
			{
				return null;
			}
		}
	}

	public enum Urls
	{
		DiscordWebhookOOCURL,
		DiscordWebhookAdminURL,
		DiscordWebhookAnnouncementURL
	}
}