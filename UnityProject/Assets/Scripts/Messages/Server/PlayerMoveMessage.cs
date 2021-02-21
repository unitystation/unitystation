using System.Collections;
using UnityEngine;
using Mirror;

///   Tells client to apply PlayerState (update his position, flight direction etc) to the given player
public class PlayerMoveMessage : ServerMessage
{
	public struct PlayerMoveMessageNetMessage : NetworkMessage
	{
		public PlayerState State;
		/// Player to be moved
		public uint SubjectPlayer;

		public override string ToString()
		{
			return $"[PlayerMoveMessage State={State} Subject={SubjectPlayer}]";
		}
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public PlayerMoveMessageNetMessage IgnoreMe;

	///To be run on client
	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as PlayerMoveMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadNetworkObject(newMsg.SubjectPlayer);

		if ( NetworkObject == null )
		{
			return;
		}

		Logger.LogTraceFormat("Processed {1}'s state: {0}", Category.Movement, this, NetworkObject.name);
		var playerSync = NetworkObject.GetComponent<PlayerSync>();
		playerSync.UpdateClientState(newMsg.State);

		if ( NetworkObject == PlayerManager.LocalPlayer ) {
			if (newMsg.State.ResetClientQueue)
			{
				playerSync.ClearQueueClient();
				playerSync.RollbackPrediction();
			}
			if (newMsg.State.MoveNumber == 0 ) {
				//Logger.Log( "Zero step rollback" );
				playerSync.ClearQueueClient();
				playerSync.RollbackPrediction();
			}

			ControlTabs.CheckTabClose();
		}
	}

	public static PlayerMoveMessageNetMessage Send(NetworkConnection recipient, GameObject subjectPlayer, PlayerState state)
	{
		var msg = new PlayerMoveMessageNetMessage
		{
			SubjectPlayer = subjectPlayer != null ? subjectPlayer.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
			State = state,
		};
		new PlayerMoveMessage().SendTo(recipient, msg);
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
			var msg = new PlayerMoveMessageNetMessage
			{
				SubjectPlayer = subjectPlayer != null ? subjectPlayer.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
				State = state,
			};
			new PlayerMoveMessage().SendToAll(msg);
		}

	}
}