using System.Linq;
using Logs;
using Messages.Server;
using Mirror;
using Newtonsoft.Json;
using Player;
using Systems.Character;

namespace Messages.Client.NewPlayer
{
	/// <summary>
	/// Used for requesting a job at round start.
	/// Assigns the occupation to the player and spawns them on the station.
	/// Fails if no more slots for that occupation are available.
	/// </summary>
	public class ClientRequestJobMessage : ClientMessage<ClientRequestJobMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string PlayerID;
			public JobType JobType;
			public string JsonCharSettings;
		}

		public override void Process(NetMessage msg)
		{
			// Serverside: check that message sent from client is good, and then validate request (round started, has job space etc)
			if (ValidateMessage(msg) && ValidateRequest(msg))
			{
				AcceptRequest(msg);
			}
		}

		public static NetMessage Send(JobType jobType, string jsonCharSettings, string playerID)
		{
			NetMessage msg = new NetMessage
			{
				JobType = jobType,
				JsonCharSettings = jsonCharSettings,
				PlayerID = playerID
			};

			Send(msg);
			return msg;
		}

		private bool ValidateMessage(NetMessage msg)
		{
			if (SentByPlayer == null || SentByPlayer.Equals(PlayerInfo.Invalid))
			{
				Loggy.LogError($"Cannot process {nameof(ClientRequestJobMessage)}: {nameof(SentByPlayer)} is null!", Category.Jobs);
				return false;
			}

			if (SentByPlayer.UserId == null)
			{
				NotifyError(JobRequestError.InvalidUserID, $"{nameof(SentByPlayer.UserId)} is null");
				return false;
			}

			if (SentByPlayer.UserId != msg.PlayerID)
			{
				NotifyError(JobRequestError.InvalidPlayerID, $"{nameof(msg.PlayerID)} does not match {nameof(SentByPlayer.UserId)}");
				return false;
			}

			return true;
		}

		private bool ValidateRequest(NetMessage msg)
		{
			if (GameManager.Instance.CurrentRoundState != RoundState.Started)
			{
				NotifyRequestRejected(JobRequestError.RoundNotReady, "round hasn't started yet");
				return false;
			}

			if (PlayerList.Instance.FindPlayerJobBanEntryServer(msg.PlayerID, msg.JobType, true) != null)
			{
				NotifyRequestRejected(JobRequestError.JobBanned, $"player was job-banned from {msg.JobType}");
				return false;
			}

			if (msg.JobType != JobType.NULL)
			{
				int slotsTaken = GameManager.Instance.ServerGetOccupationsCount(msg.JobType);
				int slotsMax = GameManager.Instance.GetOccupationMaxCount(msg.JobType);
				if (slotsTaken >= slotsMax)
				{
					NotifyRequestRejected(JobRequestError.PositionsFilled, $"no empty positions for {msg.JobType}");
					return false;
				}
			}

			return true;
		}

		private void AcceptRequest(NetMessage msg)
		{
			if (msg.JobType == JobType.NULL)
			{
				var character = JsonConvert.DeserializeObject<CharacterSheet>(msg.JsonCharSettings);
				character.ValidateSpeciesCanBePlayerChosen();
				PlayerSpawn.NewSpawnPlayerV2(SentByPlayer, OccupationList.Instance.AllOcccupations.First(x => x.name == "Spectator") , character);
			}
			else
			{
				var character = JsonConvert.DeserializeObject<CharacterSheet>(msg.JsonCharSettings);
				var spawnRequest = new PlayerSpawnRequest(SentByPlayer, GameManager.Instance.GetRandomFreeOccupation(msg.JobType), character);
				character.ValidateSpeciesCanBePlayerChosen();
				if (GameManager.Instance.TrySpawnPlayer(spawnRequest) == false)
				{
					SendClientLogMessage.SendErrorToClient(SentByPlayer, "Server couldn't spawn you.");
				}
			}

		}

		private void NotifyError(JobRequestError error, string message)
		{
			Loggy.LogError($"Cannot process {SentByPlayer}'s {nameof(ClientRequestJobMessage)}: {message}.", Category.Jobs);
			JobRequestFailedMessage.SendTo(SentByPlayer, error);
		}

		private void NotifyRequestRejected(JobRequestError error, string message)
		{
			Loggy.Log($"Job request from {SentByPlayer} rejected: {message}.", Category.Jobs);
			JobRequestFailedMessage.SendTo(SentByPlayer, error);
		}
	}
}
