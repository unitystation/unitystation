using Mirror;
using Player;
using Shuttles;
using UnityEngine;

namespace Messages.Client.Interaction
{
	/// <summary>
	/// Requests for the server to perform examine interaction
	/// </summary>
	public class RequestExamineMessage : ClientMessage<RequestExamineMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			//members
			// netid of target
			public uint examineTarget;
			public Vector3 mousePosition;
		}

		public override void Process(NetMessage netMsg)
		{
			//TODO: check break conditions
			if (SentByPlayer == null || SentByPlayer.Script == null)
			{
				return;
			}

			LoadNetworkObject(netMsg.examineTarget);

			if (NetworkObject == null) return;

			//If we are looking for matrix we use the matrix sync Id so we need to point back to the matrix
			if (NetworkObject.TryGetComponent<MatrixSync>(out var matrixSync))
			{
				NetworkObject = matrixSync.NetworkedMatrix.gameObject;
			}

			// Here we build the message to send, by looking at the target's components.
			// anyone extending IExaminable gets a say in it.
			// Look for examinables.
			var examinables = NetworkObject.GetComponents<IExaminable>();
			string msg = "";
			IExaminable examinable;

			for (int i = 0; i < examinables.Length; i++)
			{
				examinable = examinables[i];
				// don't send text message target is player - instead send PlayerExaminationMessage

				// Exception for player examine window.
				//TODO make this be based on a setting clients can turn off
				if (examinable is ExaminablePlayer examinablePlayer)
				{
					examinablePlayer.Examine(SentByPlayer.GameObject);
				}

				var examinableMsg = examinable.Examine(netMsg.mousePosition);
				if (string.IsNullOrEmpty(examinableMsg))
					continue;

				msg += examinableMsg;

				if (i != examinables.Length - 1)
				{
					msg += "\n";
				}
			}

			// Send the message.
			if (msg.Length > 0)
			{
				Chat.AddExamineMsgFromServer(SentByPlayer.GameObject, msg);
			}
		}

		public static void Send(uint targetNetId)
		{
			var msg = new NetMessage()
			{
				examineTarget = targetNetId
			};

			Send(msg);
		}

		public static void Send(uint targetNetId, Vector3 mousePos)
		{
			var msg = new NetMessage()
			{
				examineTarget = targetNetId,
				mousePosition = mousePos
			};

			Send(msg);
		}
	}
}
