using System.Collections;
using Messages.Client;
using Mirror;

/// <summary>
///     Informs server of predicted movement action
/// </summary>
public class RequestMoveMessage : ClientMessage
{
	public PlayerAction Action;

	public override void Process()
	{
		SentByPlayer.Script.PlayerSync.ProcessAction(Action);
	}

	public static RequestMoveMessage Send(PlayerAction action)
	{
		RequestMoveMessage msg = new RequestMoveMessage
		{
			Action = action
		};
		msg.Send();
		return msg;
	}

	public override string ToString()
	{
		return $"[RequestMoveMessage Action={Action} SentBy={SentByPlayer}]";
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Action.moveActions = new int[reader.ReadInt32()];
		for ( var i = 0; i < Action.moveActions.Length; i++ )
		{
			Action.moveActions[i] = reader.ReadInt32();
		}
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteInt32(Action.moveActions.Length);
		for ( var i = 0; i < Action.moveActions.Length; i++ )
		{
			writer.WriteInt32(Action.moveActions[i]);
		}
	}
}