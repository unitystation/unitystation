using System.Collections;
using Mirror;

namespace Messages.Client
{
	/// <summary>
	///     Informs server of predicted movement action
	/// </summary>
	public class RequestMoveMessage : ClientMessage<RequestMoveMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public PlayerAction Action;
		}

		public override void Process(NetMessage msg)
		{
			SentByPlayer.Script.PlayerSync.ProcessAction(msg.Action);
		}

		public static NetMessage Send(PlayerAction action)
		{
			NetMessage msg = new NetMessage
			{
				Action = action
			};

			Send(msg);
			return msg;
		}
	}

	public static class CustomReadWriteFunctions
	{
		public static int[] ReadMoveAction(this NetworkReader reader)
		{
			var moveActions = new int[reader.ReadInt32()];
			for ( var i = 0; i < moveActions.Length; i++ )
			{
				moveActions[i] = reader.ReadInt32();
			}

			return moveActions;
		}

		public static void WriteMoveAction(this NetworkWriter writer, int[] value)
		{
			writer.WriteInt32(value.Length);
			for ( var i = 0; i < value.Length; i++ )
			{
				writer.WriteInt32(value[i]);
			}
		}
	}
}