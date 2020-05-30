using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;


namespace DiscordWebhook
{
	public static class DiscordWebhookMessage
	{
		public static byte[] Post(string url, NameValueCollection pairs)
		{
			using (WebClient webClient = new WebClient())
				return webClient.UploadValues(url, pairs);
		}

		public static void SendWebHookMessage(string url, string msg, string username, string mentionID = null)
		{
			msg = MsgMentionProcess(msg, mentionID);

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

			newmsg = Regex.Replace(newmsg, "<@", " ");

			if (mentionID != null)
			{
				newmsg = Regex.Replace(newmsg, "(?i)@ServerAdmin", mentionID);
			}

			//<@&677800847391850496>
			return newmsg;
		}
	}
}