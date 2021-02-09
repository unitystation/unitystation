using System.Collections;
using UnityEngine;
using Mirror;

///   Tells client to apply PlayerState (update his position, flight direction etc) to the given player
public class PlayerMoveMessage : ServerMessage
{
	public PlayerState State;
	/// Player to be moved
	public uint SubjectPlayer;

	///To be run on client
	public override void Process()
	{
		LoadNetworkObject(SubjectPlayer);

		if ( NetworkObject == null )
		{
			return;
		}

		Logger.LogTraceFormat("Processed {1}'s state: {0}", Category.Movement, this, NetworkObject.name);
		var playerSync = NetworkObject.GetComponent<PlayerSync>();
		playerSync.UpdateClientState(State);

		if ( NetworkObject == PlayerManager.LocalPlayer ) {
			if (State.ResetClientQueue)
			{
				playerSync.ClearQueueClient();
				playerSync.RollbackPrediction();
			}
			if ( State.MoveNumber == 0 ) {
	//			Logger.Log( "Zero step rollback" );
				playerSync.ClearQueueClient();
				playerSync.RollbackPrediction();
			}

			ControlTabs.CheckTabClose();
		}
	}

	public static PlayerMoveMessage Send(NetworkConnection recipient, GameObject subjectPlayer, PlayerState state)
	{
		var msg = new PlayerMoveMessage
		{
			SubjectPlayer = subjectPlayer != null ? subjectPlayer.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
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
					Send(connectedPlayer.Connection, subjectPlayer, state);
				}
			}
		}
		else
		{
			var msg = new PlayerMoveMessage
			{
				SubjectPlayer = subjectPlayer != null ? subjectPlayer.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
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