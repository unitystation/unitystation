using System;
using System.Collections.Generic;
using UnityEngine;

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

		ChatChannel chatChannels = ChatChannel.Common;
		string commonMessage = $"{playerName} has signed up as {playerOccupation.DisplayName}.";
		string deptMessage = $"{playerName}, {playerOccupation.DisplayName}, is the department head.";

		if (channelFromJob.ContainsKey(playerOccupation.JobType))
		{
			BroadcastCommMsg(channelFromJob[playerOccupation.JobType], deptMessage);
		}

		if (JobCategories.CentCommJobs.Contains(playerOccupation.JobType))
		{
			chatChannels = ChatChannel.CentComm;
		}

		switch (playerOccupation.JobType)
		{
			case JobType.AI:
				commonMessage = $"{player.ExpensiveName()} has been bluespace-beamed into the AI core!";
				break;
			case JobType.SYNDICATE:
				chatChannels = ChatChannel.Syndicate;
				break;
		}

		BroadcastCommMsg(chatChannels, commonMessage);
	}

	private void BroadcastCommMsg(ChatChannel chatChannels, string message)
	{
		Chat.AddCommMsgByMachineToChat(gameObject, message, chatChannels, ChatModifier.ColdlyState, machineName);
	}
}
