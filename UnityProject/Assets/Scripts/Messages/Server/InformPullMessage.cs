using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

///     Message that tells client that "Subject" is now pulled by "PulledBy"
public class InformPullMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.InformPull;

	public NetworkInstanceId Subject;
	public NetworkInstanceId PulledBy;

	public override IEnumerator Process()
	{
		yield return WaitFor(Subject, PulledBy);
		if ( NetworkObjects[0] == null )
		{
			yield break;
		}

		PushPull subject = NetworkObjects[0].GetComponent<PushPull>();

		PushPull pulledBy = NetworkObjects[1]?.GetComponent<PushPull>();

		if ( subject.AttachedToClient ) {
			subject.AttachedToClient.ControlledObjectClient = null;
		}
		subject.AttachedToClient = pulledBy;
		Logger.Log( $"Received: {subject.gameObject.name} is {getStatus( pulledBy )}", Category.PushPull );
	}

/// <param name="recipient">Send to whom</param>
/// <param name="subject">Who is this message about</param>
/// <param name="pulledBy">Who pulls the subject</param>
	public static InformPullMessage Send(PushPull recipient, PushPull subject, PushPull pulledBy)
	{
		//not sending message to non-players
		if ( !recipient || recipient.registerTile.ObjectType != ObjectType.Player ) {
			return null;
		}
		InformPullMessage msg =
			new InformPullMessage { Subject = subject.gameObject.NetId(),
									PulledBy = pulledBy == null ? NetworkInstanceId.Invalid : pulledBy.gameObject.NetId(),
			};

		msg.SendTo(recipient.gameObject);
		Logger.LogTraceFormat( "Sent to {0}: {1} is {2}", Category.PushPull, recipient, subject, getStatus( pulledBy ) );
		return msg;
	}

	private static string getStatus( PushPull pulledBy ) {
		var status = pulledBy == null ? "no longer being pulled" : "now pulled by " + pulledBy.gameObject.name;
		return status;
	}

	public override string ToString()
	{
		return string.Format("[InformPullMessage Subject={0} PulledBy={1}]", Subject, PulledBy);
	}
}