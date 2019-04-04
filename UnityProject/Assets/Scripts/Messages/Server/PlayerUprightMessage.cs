using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

///   Tells client to make given player appear laying down or back up on feet
public class PlayerUprightMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.PlayerUprightMessage;
	public bool Upright;
	/// Whom is it about
	public NetworkInstanceId SubjectPlayer;

	///To be run on client
	public override IEnumerator Process()
	{
//		Logger.Log("Processed " + ToString());
		yield return WaitFor(SubjectPlayer);

		if ( NetworkObject == null ) {
			yield break;
		}
		RegisterPlayer registerPlayer = NetworkObject.GetComponent<RegisterPlayer>();
		if ( !registerPlayer )
		{
			yield break;
		}
		if ( Upright )
		{
			registerPlayer.GetUp();
		}
		else
		{
			registerPlayer.LayDown();
		}
	}

	public static PlayerUprightMessage Send(GameObject recipient, GameObject subjectPlayer, bool state)
	{
		var msg = new PlayerUprightMessage
		{
			SubjectPlayer = subjectPlayer.NetId(),
			Upright = state,
		};
		msg.SendTo(recipient);
		return msg;
	}

	public static void SendToAll(GameObject subjectPlayer, bool state)
	{
		var msg = new PlayerUprightMessage
		{
			SubjectPlayer = subjectPlayer.NetId(),
			Upright = state,
		};
		msg.SendToAll();
	}

	public override string ToString()
	{
		return $"[PlayerUprightMessage Upright={Upright} Subject={SubjectPlayer}]";
	}
}