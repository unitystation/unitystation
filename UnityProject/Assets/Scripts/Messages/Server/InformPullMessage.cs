using System.Collections;
using System.Collections.Generic;
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

		Logger.Log( $"Received: {subject.gameObject?.name} is {getStatus( pulledBy )}", Category.PushPull );

		if ( PlayerManager.LocalPlayer ) {
			if ( subject == PlayerManager.LocalPlayerScript.pushPull.PulledObjectClient && pulledBy == null ) {
				Logger.Log( "Removing all frelling blue arrows for ya!", Category.PushPull );
				for ( var i = 0; i < trackedObjects.Count; i++ ) {
					PushPull trackedObject = trackedObjects[i];
					if ( !trackedObject || trackedObject.Pushable == null ) {
						continue;
					}
					trackedObject.Pushable.OnClientStartMove().RemoveAllListeners();
					trackedObject.PulledObjectClient = null;
				}

				trackedObjects.Clear();
			}
			else if (!trackedObjects.Contains( subject ))
			{
				trackedObjects.Add( subject );
			}
		}

		if ( subject.PulledByClient ) {
			subject.PulledByClient.PulledObjectClient = null;
		}

		subject.PulledByClient = pulledBy;
		if ( pulledBy ) {
			subject.PulledByClient.PulledObjectClient = subject;
		}
	}

	private static List<PushPull> trackedObjects = new List<PushPull>();

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