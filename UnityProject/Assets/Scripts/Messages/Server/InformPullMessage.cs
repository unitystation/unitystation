using System.Collections.Generic;
using Mirror;

namespace Messages.Server
{
	///     Message that tells client that "Subject" is now pulled by "PulledBy"
	public class InformPullMessage : ServerMessage<InformPullMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint Subject;
			public uint PulledBy;

			public override string ToString()
			{
				return string.Format("[InformPullMessage Subject={0} PulledBy={1}]", Subject, PulledBy);
			}
		}

		public override void Process(NetMessage msg)
		{
			LoadMultipleObjects(new uint [] {msg.Subject, msg.PulledBy});

			if ( NetworkObjects[0] == null )
			{
				return;
			}
//
// 			PushPull subject = NetworkObjects[0].GetComponent<PushPull>();
//
// 			PushPull pulledBy = NetworkObjects[1]?.GetComponent<PushPull>();
//
// 			Logger.Log( $"Received: {subject.gameObject?.name} is {getStatus( pulledBy )}", Category.PushPull );
//
// 			if ( PlayerManager.LocalPlayer ) {
// 				if ( subject == PlayerManager.LocalPlayerScript.pushPull.PulledObjectClient && pulledBy == null ) {
// //				Logger.Log( "Removing all frelling blue arrows for ya!", Category.PushPull );
// 					for ( var i = 0; i < trackedObjects.Count; i++ ) {
// 						PushPull trackedObject = trackedObjects[i];
// 						if ( !trackedObject || trackedObject.Pushable == null ) {
// 							continue;
// 						}
// 						trackedObject.Pushable.OnClientStartMove().RemoveAllListeners();
// 						trackedObject.Pushable.OnClientStopFollowing();
// 						trackedObject.PulledObjectClient = null;
// 					}
//
// 					trackedObjects.Clear();
// 				}
// 				else if (!trackedObjects.Contains( subject ))
// 				{
// 					trackedObjects.Add( subject );
// 				}
// 			}
//
// 			if ( subject.PulledByClient ) {
// 				subject.PulledByClient.PulledObjectClient = null;
// 			}
//
// 			subject.PulledByClient = pulledBy;
// 			if ( pulledBy ) {
// 				subject.PulledByClient.PulledObjectClient = subject;
// 			}
		}

		private static List<OLDPushPull> trackedObjects = new List<OLDPushPull>();

		/// <param name="recipient">Send to whom</param>
		/// <param name="subject">Who is this message about</param>
		/// <param name="pulledBy">Who pulls the subject</param>
		public static NetMessage Send(OLDPushPull recipient, OLDPushPull subject, OLDPushPull pulledBy)
		{
			//not sending message to non-players
			if ( !recipient || recipient.registerTile.ObjectType != ObjectType.Player ) {
				return new NetMessage();
			}
			NetMessage msg =
				new NetMessage { Subject = subject.gameObject.NetId(),
					PulledBy = pulledBy == null ? NetId.Invalid : pulledBy.gameObject.NetId(),
				};

			SendTo(recipient.gameObject, msg);
			Logger.LogTraceFormat( "Sent to {0}: {1} is {2}", Category.PushPull, recipient, subject, getStatus( pulledBy ) );
			return msg;
		}

		private static string getStatus( OLDPushPull pulledBy ) {
			var status = pulledBy == null ? "no longer being pulled" : "now pulled by " + pulledBy.gameObject.name;
			return status;
		}
	}
}