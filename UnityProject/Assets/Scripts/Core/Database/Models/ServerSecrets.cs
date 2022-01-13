using System;

namespace Core.Database.Models
{
	[Serializable]
	public class ServerSecrets
	{
		public string RconPass;
		//CertKey needed in the future for SSL Rcon
		public string certKey;
		public string HubUser;
		public string HubPass;

		//Discord Webhook URL//

		//OOC chat
		public string DiscordWebhookOOCURL;

		//ID that can be pinged in OOC chat
		public string DiscordWebhookOOCMentionsID;

		//Webhook where Ahelps are sent
		public string DiscordWebhookAdminURL;

		//Announcements for round start/end, also public Ban/Kick if enabled
		public string DiscordWebhookAnnouncementURL;
		public bool DiscordWebhookEnableBanKickAnnouncement;

		//Sends all chat messages from each channel, also OOC if enabled
		public string DiscordWebhookAllChatURL;
		public bool DiscordWebhookSendOOCToAllChat;

		//Sends Admin actions to a webhook
		public string DiscordWebhookAdminLogURL;

		//Sends errors to a webhook
		public string DiscordWebhookErrorLogURL;

	}
}