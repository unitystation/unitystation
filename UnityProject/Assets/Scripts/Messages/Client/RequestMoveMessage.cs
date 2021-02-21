using System.Collections;
using Messages.Client;
using Mirror;

/// <summary>
///     Informs server of predicted movement action
/// </summary>
public class RequestMoveMessage : ClientMessage
{
	public struct RequestMoveMessageNetMessage : NetworkMessage
	{
		public PlayerAction Action;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RequestMoveMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RequestMoveMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		SentByPlayer.Script.PlayerSync.ProcessAction(newMsg.Action);
	}

	public static RequestMoveMessageNetMessage Send(PlayerAction action)
	{
		RequestMoveMessageNetMessage msg = new RequestMoveMessageNetMessage
		{
			Action = action
		};
		new RequestMoveMessage().Send(msg);
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