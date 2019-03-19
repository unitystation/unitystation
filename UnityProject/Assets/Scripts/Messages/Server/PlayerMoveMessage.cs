using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

///   Tells client to apply PlayerState (update his position, flight direction etc) to the given player
public class PlayerMoveMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.PlayerMoveMessage;
	public PlayerState State;
	/// Player to be moved
	public NetworkInstanceId SubjectPlayer;

	///To be run on client
	public override IEnumerator Process()
	{
//		Logger.Log("Processed " + ToString());
		yield return WaitFor(SubjectPlayer);

		if ( NetworkObject == null ) {
			yield break;
		}

		var playerSync = NetworkObject.GetComponent<PlayerSync>();
		playerSync.UpdateClientState(State);

		if ( NetworkObject == PlayerManager.LocalPlayer ) {
			if (State.ResetClientQueue)
			{
				playerSync.ClearQueueClient();
			}
			if ( State.MoveNumber == 0 ) {
	//			Logger.Log( "Zero step rollback" );
				playerSync.ClearQueueClient();
				playerSync.RollbackPrediction();
			}

			ControlTabs.CheckTabClose();
		}

	}

	public static PlayerMoveMessage Send(GameObject recipient, GameObject subjectPlayer, PlayerState state)
	{
		var msg = new PlayerMoveMessage
		{
			SubjectPlayer = subjectPlayer != null ? subjectPlayer.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
			State = state,
		};
		msg.SendTo(recipient);
		return msg;
	}

	public static void SendToAll(GameObject subjectPlayer, PlayerState state)
	{


		if (PlayerUtils.IsGhost(subjectPlayer))
		{
			//Send ghost positions only to ghosts
			foreach (var connectedPlayer in PlayerList.Instance.InGamePlayers)
			{
				if (PlayerUtils.IsGhost(connectedPlayer.GameObject))
				{
					Send(connectedPlayer.GameObject, subjectPlayer, state);
				}
			}
		}
		else
		{
			var msg = new PlayerMoveMessage
			{
				SubjectPlayer = subjectPlayer != null ? subjectPlayer.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid,
				State = state,
			};
			msg.SendToAll();
		}

	}

	public override string ToString()
	{
		return $"[PlayerMoveMessage State={State} Subject={SubjectPlayer}]";
	}
}