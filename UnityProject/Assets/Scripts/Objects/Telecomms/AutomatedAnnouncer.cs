using System;
using System.Collections.Generic;
using Systems.Electricity;
using UnityEngine;
using Communications;
using Objects.Machines.ServerMachines.Communications;
using ScriptableObjects.Communications;
using Systems.Communications;
using InGameEvents;
using Logs;

namespace Objects.Telecomms
{
	public class AutomatedAnnouncer : SignalEmitter, IChatInfluencer
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

		private UniversalObjectPhysics objectPhysics;
		private APCPoweredDevice poweredDevice;
		private Integrity integrity;

		[SerializeField] private SignalDataSO radioSO;

		private void Start()
		{
			objectPhysics = GetComponent<UniversalObjectPhysics>();
			poweredDevice = GetComponent<APCPoweredDevice>();
			integrity = GetComponent<Integrity>();
		}

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

		private void ServerOnPlayerSpawned(Mind player)
		{
			if (GameManager.Instance.RoundTime < TIME_BEFORE_JOIN_ANNOUNCEMENTS)
			{
				return;
			}
			AnnounceNewCrewmember(player);
		}

		protected override bool SendSignalLogic()
		{
			if (GameManager.Instance.CommsServers.Count == 0) return false;
			return true;
		}

		public override void SignalFailed() { }


		private void AnnounceNewCrewmember(Mind player)
		{
			if (player.occupation == null) return;
			string playerName = player.CurrentCharacterSettings.Name;
			Loudness annoucementImportance = GetAnnouncementImportance(player.occupation);

			ChatChannel chatChannels = ChatChannel.Common;
			string commonMessage = $"{playerName} has signed up as {player.occupation?.DisplayName}.";
			string deptMessage = $"{playerName}, {player.occupation.DisplayName}, is the department head.";

			// Get the channel of the newly joined head from their occupation.
			if (channelFromJob.ContainsKey(player.occupation.JobType))
			{
				BroadcastCommMsg(channelFromJob[player.occupation.JobType], deptMessage, annoucementImportance);
			}

			// Announce the arrival on the CentComm channel if is a CentComm occupation.
			if (JobCategories.CentCommJobs.Contains(player.occupation.JobType))
			{
				chatChannels = ChatChannel.CentComm;
			}

			if (player.occupation.JobType == JobType.AI)
			{
				commonMessage = $"{player.CurrentCharacterSettings.AiName} has been bluespace-beamed into the AI core!";
			}
			else if (player.occupation.JobType == JobType.SYNDICATE)
			{
				chatChannels = ChatChannel.Syndicate;
			}
			else if (player.occupation.IsCrewmember == false)
			{
				// Don't announce non-crewmembers like wizards, fugitives at all (they don't have their own chat channel).
				return;
			}

			BroadcastCommMsg(chatChannels, commonMessage, GetAnnouncementImportance(player.occupation));
		}

		private Loudness GetAnnouncementImportance(Occupation job)
		{
			if (job == null) return Loudness.NORMAL;
			if (job.JobType == JobType.AI || job.JobType == JobType.HOP || job.JobType == JobType.CAPTAIN ||
			    job.JobType == JobType.CMO || job.JobType == JobType.CENTCOMM_COMMANDER || job.JobType == JobType.RD
			    || job.JobType == JobType.HOS || job.JobType == JobType.CHIEF_ENGINEER || job.JobType == JobType.CARGOTECH)
			{
				return Loudness.LOUD;
			}
			else
			{
				return Loudness.NORMAL;
			}
		}

		private void BroadcastCommMsg(ChatChannel chatChannels, string message, Loudness importance)
		{
			ChatEvent chatEvent = new ChatEvent();
			chatEvent.message = message;
			chatEvent.channels = chatChannels;
			chatEvent.VoiceLevel = importance;
			chatEvent.position = objectPhysics.OfficialPosition;
			chatEvent.originator = gameObject;
			InfluenceChat(chatEvent);
		}

		public bool WillInfluenceChat()
		{
			if (poweredDevice == null || integrity == null)
			{
				Loggy.LogError("[Telecomms/AutomatedAnnouncer] - Missing components detected on a terminal.");
				return false;
			}
			// Don't send anything if this terminal has no power
			return poweredDevice.State != PowerState.Off;
		}


		public ChatEvent InfluenceChat(ChatEvent chatToManipulate)
		{
			CommsServer.RadioMessageData msg = new CommsServer.RadioMessageData();
			// If the integrity of this terminal is so low, start scrambling text.
			if (integrity.integrity > minimumDamageBeforeObfuscation)
			{
				msg.ChatEvent = chatToManipulate;
				TrySendSignal(radioSO, msg);
				return chatToManipulate;
			}

			var scrambledText = chatToManipulate;
			scrambledText.message = EventProcessorOverload.ProcessMessage(scrambledText.message);
			msg.ChatEvent = scrambledText;
			TrySendSignal(radioSO, msg);
			return scrambledText;
		}
	}
}
