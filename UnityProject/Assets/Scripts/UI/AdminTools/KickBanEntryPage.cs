using System;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Mirror;
using Newtonsoft.Json;
using System.Globalization;

namespace AdminTools
{
	public class KickBanEntryPage : MonoBehaviour
	{
		private static KickBanEntryPage instance;

		public static KickBanEntryPage Instance => instance;

		[SerializeField] private GameObject kickPage = null;
		[SerializeField] private GameObject banPage = null;
		[SerializeField] private GameObject jobBanPage = null;

		[SerializeField] private GameObject jobBanJobTemplate = null;

		[SerializeField] private Text kickTitle = null;
		[SerializeField] private InputField kickReasonField = null;

		[SerializeField] private Text banTitle = null;
		[SerializeField] private InputField banReasonField = null;
		[SerializeField] private InputField minutesField = null;

		[SerializeField] private Toggle kickAnnounceToggle = null;
		[SerializeField] private Toggle banAnnounceToggle = null;

		[SerializeField] private Text jobBanTitle = null;
		[SerializeField] private InputField jobBanReasonField = null;
		[SerializeField] private InputField jobBanMinutesField = null;
		[SerializeField] private Toggle jobBanPermaBanToggle = null;
		[SerializeField] private Dropdown jobBanActionAfterDropDown = null;

		private List<JobBanListItem> jobBanJobTypeListObjects = new List<JobBanListItem>();

		private AdminPlayerEntryData playerToKickCache;

		public void SetPage(bool isBan, AdminPlayerEntryData playerToKick, bool isJobBan)
		{
			playerToKickCache = playerToKick;
			UIManager.IsInputFocus = true;
			UIManager.PreventChatInput = true;
			if (!isBan && !isJobBan)
			{
				kickPage.SetActive(true);
				kickTitle.text = $"Kick Player: {playerToKick.name}";
				kickReasonField.text = "";
				kickReasonField.ActivateInputField();
			}
			else if(!isJobBan)
			{
				banPage.SetActive(true);
				banTitle.text = $"Ban Player: {playerToKick.name}";
				banReasonField.text = "";
				banReasonField.ActivateInputField();
				minutesField.text = "";
			}
			else
			{
				jobBanPage.SetActive(true);
				jobBanTitle.text = $"Job Ban Player: {playerToKick.name}";
				jobBanReasonField.text = "";
				jobBanReasonField.ActivateInputField();
				jobBanMinutesField.text = "";
				jobBanPermaBanToggle.isOn = false;

				ClientJobBanDataAdminMessage.Send(DatabaseAPI.ServerData.UserID, PlayerList.Instance.AdminToken, playerToKick.uid);

				jobBanActionAfterDropDown.value = 0;
			}

			gameObject.SetActive(true);
		}

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

		private void Start()
		{
			//generate job list

			var jobs = Enum.GetNames(typeof(JobType)).ToList();

			foreach (var jobType in jobs)
			{
				if (jobType == "NULL") continue;

				GameObject jobEntry = Instantiate(jobBanJobTemplate);//creates new button
				jobEntry.SetActive(true);
				var c = jobEntry.GetComponent<JobBanListItem>();
				c.jobName.text = jobType;
				jobBanJobTypeListObjects.Add(c);

				jobEntry.transform.SetParent(jobBanJobTemplate.transform.parent, false);
			}
		}

		public void OnDoKick()
		{
			if (string.IsNullOrEmpty(kickReasonField.text))
			{
				Logger.LogError("Kick reason field needs to be completed!", Category.Admin);
				return;
			}

			RequestKickMessage.Send(ServerData.UserID, PlayerList.Instance.AdminToken, playerToKickCache.uid,
				kickReasonField.text, announceBan: kickAnnounceToggle.isOn);

			ClosePage();
		}

		public void OnDoBan()
		{
			if (string.IsNullOrEmpty(banReasonField.text))
			{
				Logger.LogError("Ban reason field needs to be completed!", Category.Admin);
				return;
			}

			if (string.IsNullOrEmpty(minutesField.text))
			{
				Logger.LogError("Duration field needs to be completed!", Category.Admin);
				return;
			}

			int minutes;
			int.TryParse(minutesField.text, out minutes);
			RequestKickMessage.Send(ServerData.UserID, PlayerList.Instance.AdminToken, playerToKickCache.uid,
				banReasonField.text, true, minutes, announceBan: banAnnounceToggle.isOn);
			ClosePage();
		}

		public void OnDoJobBan()
		{
			if (string.IsNullOrEmpty(jobBanReasonField.text))
			{
				Logger.LogError("Job Ban reason field needs to be completed!", Category.Admin);
				return;
			}

			if (string.IsNullOrEmpty(jobBanMinutesField.text) && jobBanPermaBanToggle.isOn == false)
			{
				Logger.LogError("Duration field needs to be completed or Perma toggled!", Category.Admin);
				return;
			}

			var value = jobBanActionAfterDropDown.value;

			var ghost = value == 2;
			var kick = value == 3;

			int minutes;
			var outSuccess = int.TryParse(jobBanMinutesField.text, out minutes);

			if (!outSuccess && jobBanPermaBanToggle.isOn == false)
			{
				Logger.LogError("Minutes Field incorrectly configured", Category.Admin);
				return;
			}

			if (jobBanPermaBanToggle.isOn == true)
			{
				minutes = 0;
			}

			foreach (var jobs in jobBanJobTypeListObjects)
			{
				if(jobs.toBeBanned.isOn == false) continue;

				var jobTypeBool = Enum.TryParse(jobs.jobName.text, out JobType jobType);

				if(!jobTypeBool) continue;

				PlayerList.RequestJobBan.Send(ServerData.UserID, PlayerList.Instance.AdminToken, playerToKickCache.uid,
					jobBanReasonField.text, jobBanPermaBanToggle.isOn, minutes, jobType, ghost, kick);
			}

			ClosePage();
		}
		public void ClosePage()
		{
			gameObject.SetActive(false);
			kickPage.SetActive(false);
			banPage.SetActive(false);
			jobBanPage.SetActive(false);
			UIManager.IsInputFocus = false;
			var manager = FindObjectOfType<PlayerManagePage>();
			manager.RefreshPage();
			UIManager.PreventChatInput = false;
		}

		public class ClientJobBanDataAdminMessage : ClientMessage
		{
			public string AdminID;
			public string AdminToken;
			public string PlayerID;

			public override void Process()
			{
				var admin = PlayerList.Instance.GetAdmin(AdminID, AdminToken);
				if (admin == null) return;

				//Server Stuff here

				var jobBanEntries = PlayerList.Instance.ListOfBanEntries(PlayerID);

				ServerSendsJobBanDataAdminMessage.Send(SentByPlayer.Connection, jobBanEntries);
			}

			public static ClientJobBanDataAdminMessage Send(string adminID, string adminToken, string playerID)
			{
				ClientJobBanDataAdminMessage msg = new ClientJobBanDataAdminMessage
				{
					AdminID = adminID,
					AdminToken = adminToken,
					PlayerID = playerID
				};
				msg.Send();
				return msg;
			}
		}

		public class ServerSendsJobBanDataAdminMessage : ServerMessage
		{
			public string JobBanEntries;

			public override void Process()
			{
				//client Stuff here

				var bans = JsonConvert.DeserializeObject<List<JobBanEntry>>(JobBanEntries);

				foreach (var jobObject in KickBanEntryPage.instance.jobBanJobTypeListObjects)
				{
					jobObject.toBeBanned.isOn = false;

					if (bans == null || bans.Count == 0)
					{
						jobObject.unbannedStatus.SetActive(true);
						jobObject.bannedStatus.SetActive(false);
						continue;
					}

					foreach (var jobsBanned in bans)
					{
						if (jobObject.jobName.text == jobsBanned.job.ToString())
						{
							jobObject.bannedStatus.SetActive(true);

							var msg = "";

							if (jobsBanned.isPerma)
							{
								msg = "Perma Banned";
							}
							else
							{
								var entryTime = DateTime.ParseExact(jobsBanned.dateTimeOfBan,"O",CultureInfo.InvariantCulture);
								var totalMins = Mathf.Abs((float)(entryTime - DateTime.Now).TotalMinutes);

								msg = $"{Mathf.RoundToInt((float)jobsBanned.minutes - totalMins)} minutes left";
							}

							jobObject.banTime.text = msg;
							jobObject.unbannedStatus.SetActive(false);
							break;
						}

						jobObject.unbannedStatus.SetActive(true);
						jobObject.bannedStatus.SetActive(false);
					}
				}
			}

			public static ServerSendsJobBanDataAdminMessage Send(NetworkConnection requestee, List<JobBanEntry> jobBanEntries)
			{
				ServerSendsJobBanDataAdminMessage msg = new ServerSendsJobBanDataAdminMessage
				{
					JobBanEntries = JsonConvert.SerializeObject(jobBanEntries)
				};
				msg.SendTo(requestee);
				return msg;
			}
		}
	}
}