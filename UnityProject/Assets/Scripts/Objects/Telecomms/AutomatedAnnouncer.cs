using System;
using System.Collections.Generic;
using UnityEngine;

namespace Objects.Telecomms
{
	public class AutomatedAnnouncer : MonoBehaviour
	{
		private const string machineName = "Announcing Machine";

		private static readonly DateTime TIME_BEFORE_JOIN_ANNOUNCEMENTS = new DateTime().AddHours(12).AddSeconds(5);

		private static readonly Dictionary<JobType, ChatChannel> channelFromJob = new Dictionary<JobType, ChatChannel>()
	{
		{ JobType.CAPTAIN, ChatChannel.Command },
		{ JobType.HOP, ChatChannel.Service },
		{ JobType.RD, ChatChannel.Science },
		{ JobType.CHIEF_ENGINEER, ChatChannel.Engineering },
		{ JobType.CMO, ChatChannel.Medical },
		{ JobType.HOS, ChatChannel.Security }
	};

		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer)
			{
				PlayerSpawn.SpawnEvent += ServerOnPlayerSpawned;
			}
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer)
			{
				PlayerSpawn.SpawnEvent -= ServerOnPlayerSpawned;
			}
		}

		private void ServerOnPlayerSpawned(object sender, PlayerSpawn.SpawnEventArgs args)
		{
			if (GameManager.Instance.stationTime < TIME_BEFORE_JOIN_ANNOUNCEMENTS)
			{
				return;
			}

			AnnounceNewCrewmember(args.player);
		}

		private void AnnounceNewCrewmember(GameObject player)
		{
			PlayerScript playerScript = player.GetComponent<PlayerScript>();
			Occupation playerOccupation = playerScript.mind.occupation;
			string playerName = player.ExpensiveName();
			int annoucementImportance = GetAnnouncementImportance(playerOccupation);

			ChatChannel chatChannels = ChatChannel.Common;
			string commonMessage = $"{playerName} has signed up as {playerOccupation.DisplayName}.";
			string deptMessage = $"{playerName}, {playerOccupation.DisplayName}, is the department head.";

			// Get the channel of the newly joined head from their occupation.
			if (channelFromJob.ContainsKey(playerOccupation.JobType))
			{
				BroadcastCommMsg(channelFromJob[playerOccupation.JobType], deptMessage, annoucementImportance);
			}

			// Announce the arrival on the CentComm channel if is a CentComm occupation.
			if (JobCategories.CentCommJobs.Contains(playerOccupation.JobType))
			{
				chatChannels = ChatChannel.CentComm;
			}

			if (playerOccupation.JobType == JobType.AI)
			{
				commonMessage = $"{player.ExpensiveName()} has been bluespace-beamed into the AI core!";
			}
			else if (playerOccupation.JobType == JobType.SYNDICATE)
			{
				chatChannels = ChatChannel.Syndicate;
			}
			else if (playerOccupation.IsCrewmember == false)
			{
				// Don't announce non-crewmembers like wizards, fugitives at all (they don't have their own chat channel).
				return;
			}

			BroadcastCommMsg(chatChannels, commonMessage, 1);
		}

		private int GetAnnouncementImportance(Occupation job)
		{
			if (job.JobType == JobType.AI || job.JobType == JobType.HOP || job.JobType == JobType.CAPTAIN ||
			    job.JobType == JobType.CMO)
			{
				return 2;
			}
			else
			{
				return 1;
			}
		}

		private void BroadcastCommMsg(ChatChannel chatChannels, string message, int importance)
		{
			Chat.AddCommMsgByMachineToChat(gameObject, message, chatChannels, ChatModifier.ColdlyState, importance,  machineName);
		}
	}
}
