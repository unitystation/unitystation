using System.Collections;
using UnityEngine;
using Messages.Server;
using Newtonsoft.Json;

namespace Messages.Client
{
	/// <summary>
	/// Used for requesting a job at round start.
	/// Assigns the occupation to the player and spawns them on the station.
	/// Fails if no more slots for that occupation are available.
	/// </summary>
	public class ClientRequestJobMessage : ClientMessage
	{
		public string PlayerID;
		public JobType JobType;
		public string JsonCharSettings;

		public override void Process()
		{
			// Serverside: check that message sent from client is good, and then validate request (round started, has job space etc)
			if (ValidateMessage() && ValidateRequest())
			{
				AcceptRequest();
			}
		}

		public static ClientRequestJobMessage Send(JobType jobType, string jsonCharSettings, string playerID)
		{
			ClientRequestJobMessage msg = new ClientRequestJobMessage
			{
				JobType = jobType,
				JsonCharSettings = jsonCharSettings,
				PlayerID = playerID
			};
			msg.Send();
			return msg;
		}

		private bool ValidateMessage()
		{
			if (SentByPlayer == null || SentByPlayer.Equals(ConnectedPlayer.Invalid))
			{
				Logger.LogError($"Cannot process {nameof(ClientRequestJobMessage)}: {nameof(SentByPlayer)} is null!");
				return false;
			}

			if (SentByPlayer.ViewerScript == null)
			{
				NotifyError(JobRequestError.InvalidScript, $"{nameof(SentByPlayer.ViewerScript)} is null");
				return false;
			}

			if (SentByPlayer.UserId == null)
			{
				NotifyError(JobRequestError.InvalidUserID, $"{nameof(SentByPlayer.UserId)} is null");
				return false;
			}

			if (SentByPlayer.UserId != PlayerID)
			{
				NotifyError(JobRequestError.InvalidPlayerID, $"{nameof(PlayerID)} does not match {nameof(SentByPlayer.UserId)}");
				return false;
			}

			return true;
		}

		private bool ValidateRequest()
		{
			if (GameManager.Instance.CurrentRoundState != RoundState.Started)
			{
				NotifyRequestRejected(JobRequestError.RoundNotReady, "round hasn't started yet");
				return false;
			}

			if (PlayerList.Instance.FindPlayerJobBanEntryServer(PlayerID, JobType, true) != null)
			{
				NotifyRequestRejected(JobRequestError.JobBanned, $"player was job-banned from {JobType}");
				return false;
			}

			int slotsTaken = GameManager.Instance.GetOccupationsCount(JobType);
			int slotsMax = GameManager.Instance.GetOccupationMaxCount(JobType);
			if (slotsTaken >= slotsMax)
			{
				NotifyRequestRejected(JobRequestError.PositionsFilled, $"no empty positions for {JobType}");
				return false;
			}

			return true;
		}

		private void AcceptRequest()
		{
			var characterSettings = JsonConvert.DeserializeObject<CharacterSettings>(JsonCharSettings);
			var spawnRequest = PlayerSpawnRequest.RequestOccupation(
					SentByPlayer.ViewerScript, GameManager.Instance.GetRandomFreeOccupation(JobType), characterSettings, SentByPlayer.UserId);

			GameManager.Instance.SpawnPlayerRequestQueue.Enqueue(spawnRequest);
			GameManager.Instance.ProcessSpawnPlayerQueue();
		}

		private void NotifyError(JobRequestError error, string message)
		{
			Logger.LogError($"Cannot process {SentByPlayer}'s {nameof(ClientRequestJobMessage)}: {message}.");
			JobRequestFailedMessage.SendTo(SentByPlayer, error);
		}

		private void NotifyRequestRejected(JobRequestError error, string message)
		{
			Logger.Log($"Job request from {SentByPlayer} rejected: {message}.", Category.Jobs);
			JobRequestFailedMessage.SendTo(SentByPlayer, error);
		}
	}
}
