using Mirror;
using UnityEngine;
using UI;

namespace Messages.Server
{
	/// <summary>
	///Tells client to apply PlayerState (update his position, flight direction etc) to the given player
	/// </summary>
	public class PlayerMoveMessage : ServerMessage<PlayerMoveMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public PlayerState State;
			/// Player to be moved
			public uint SubjectPlayer;

			public override string ToString()
			{
				return $"[PlayerMoveMessage State={State} Subject={SubjectPlayer}]";
			}
		}

		/// To be run on client
		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.SubjectPlayer);

			if ( NetworkObject == null )
			{
				return;
			}

			Logger.LogTraceFormat("Processed {1}'s state: {0}", Category.Movement, this, NetworkObject.name);
			// var playerSync = NetworkObject.GetComponent<PlayerSync>();
			// playerSync.UpdateClientState(msg.State);
			//
			// if ( NetworkObject == PlayerManager.LocalPlayer ) {
			// 	if (msg.State.ResetClientQueue)
			// 	{
			// 		playerSync.ClearQueueClient();
			// 		playerSync.RollbackPrediction();
			// 	}
			// 	if (msg.State.MoveNumber == 0 ) {
			// 		playerSync.ClearQueueClient();
			// 		playerSync.RollbackPrediction();
			// 	}
			//
			// 	ControlTabs.CheckTabClose();
			// }
		}

		public static NetMessage Send(NetworkConnection recipient, GameObject subjectPlayer, PlayerState state)
		{
			var msg = new NetMessage
			{
				SubjectPlayer = subjectPlayer != null ? subjectPlayer.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
				State = state,
			};

			SendTo(recipient, msg);
			return msg;
		}

		public static void SendToAll(GameObject subjectPlayer, PlayerState state)
		{
			if (PlayerUtils.IsGhost(subjectPlayer))
			{
				// Send ghost positions only to ghosts
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
				var msg = new NetMessage
				{
					SubjectPlayer = subjectPlayer != null ? subjectPlayer.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
					State = state,
				};

				SendToAll(msg);
			}
		}
	}
}
