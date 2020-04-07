using System.Collections;
using Mirror;

/// <summary>
///     A message that is supposed to be sent from client when he's ready to accept sync data(transforms, etc.)
///     Should probably be sent only once
/// </summary>
public class RequestSyncMessage : ClientMessage
{
	public override void Process()
	{
//		Logger.Log("Processed " + ToString());
		Logger.Log($"{SentByPlayer} requested sync", Category.Connections);

		//not sending out sync data for players not ingame
		if (SentByPlayer.Job != JobType.NULL && !SentByPlayer.Synced)
		{
			CustomNetworkManager.Instance.SyncPlayerData(SentByPlayer.GameObject);

			//marking player as synced to avoid sending that data pile again
			SentByPlayer.Synced = true;

		}
	}
}
