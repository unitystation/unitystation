using System.Collections;
using UnityEngine.Networking;

/// <summary>
///     A message that is supposed to be sent from client when he's ready to accept sync data(transforms, etc.)
///     Should probably be sent only once
/// </summary>
public class RequestSyncMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RequestSyncMessage;

	public override IEnumerator Process()
	{
//		Logger.Log("Processed " + ToString());
		Logger.Log($"{SentByPlayer} requested sync", Category.Connections);

		//not sending out sync data for players not ingame
		if ( SentByPlayer.Job != JobType.NULL && !SentByPlayer.Synced ) {
			CustomNetworkManager.Instance.SyncPlayerData(SentByPlayer.GameObject);

			//marking player as synced to avoid sending that data pile again
			SentByPlayer.Synced = true;

			AnnounceNewPlayer( SentByPlayer );
		}
		yield return null;
	}

	private static void AnnounceNewPlayer( ConnectedPlayer newPlayer ) {
		if ( newPlayer.Job == JobType.SYNDICATE ) {
			return;
		}
		var chatEvent = new ChatEvent( $"{newPlayer.Job.JobString()} {newPlayer.Name} has arrived at the station. " +
		                               $"Have a pleasant day! Try not to die...", ChatChannel.System, true );
		AnnouncementMessage.SendToAll( chatEvent.message );
		ChatRelay.Instance.AddToChatLogServer( chatEvent );
	}

	public override string ToString()
	{
		return $"[RequestSyncMessage Type={MessageType} SentBy={SentByPlayer}]";
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
	}
}