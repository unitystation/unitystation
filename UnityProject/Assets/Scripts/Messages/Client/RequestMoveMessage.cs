using System.Collections;
using UnityEngine.Networking;

/// <summary>
///     Informs server of predicted movement action
/// </summary>
public class RequestMoveMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RequestMoveMessage;
	public PlayerAction Action;

	public override IEnumerator Process()
	{
//		Logger.Log("Processed " + ToString());

		yield return WaitFor(SentBy);

		NetworkObject.GetComponent<PlayerSync>().ProcessAction(Action);
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
		return $"[RequestMoveMessage Action={Action} SentBy={SentBy}]";
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
		writer.Write(Action.moveActions.Length);
		for ( var i = 0; i < Action.moveActions.Length; i++ )
		{
			writer.Write(Action.moveActions[i]);
		}
	}
}