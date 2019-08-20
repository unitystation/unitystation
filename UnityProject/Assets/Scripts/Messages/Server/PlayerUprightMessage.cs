using System;
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
			//we ignore restraint
			registerPlayer.LayDown(true);
		}
	}

	/// <summary>
	/// Stunned info will ONLY be sent to subject!
	/// </summary>
	/// <param name="subjectPlayer"></param>
	/// <param name="upright"></param>
	public static void SendToAll(GameObject subjectPlayer, bool upright, bool buckling)
	{
		if (!IsValid(subjectPlayer, upright, buckling))
		{
			return;
		}
		var msg = new PlayerUprightMessage
		{
			SubjectPlayer = subjectPlayer.NetId(),
			Upright = upright,
		};
		msg.SendToAllExcept( subjectPlayer );
		msg.SendTo( subjectPlayer );
	}

	/// <summary>
	/// Sends the info on the subject's current status to a specific client.
	/// </summary>
	/// <param name="recipient">player who should recieve the message</param>
	/// <param name="subjectPlayer">player whose status is being communicated</param>
	public static void Sync(GameObject recipient, GameObject subjectPlayer)
	{
		var msg = new PlayerUprightMessage
		{
			SubjectPlayer = subjectPlayer.NetId(),
			Upright = !subjectPlayer.GetComponent<RegisterPlayer>().IsDownServer
		};
		msg.SendTo(recipient);
	}

	private static bool IsValid(GameObject subjectPlayer, bool upright, bool buckling)
	{
		if(buckling)
		{
			return true;
		}

		//checks if player is actually in a state where they can become up / down
		var playerScript = subjectPlayer.GetComponent<PlayerScript>();
		var registerPlayer = subjectPlayer.GetComponent<RegisterPlayer>();

		if (playerScript.playerMove.IsBuckled)
		{
			return false;
		}

		if(upright) //getting up
		{
			if (registerPlayer.IsDownServer)
			{
				return false;
			}
		}
		return true;
	}

	public override string ToString()
	{
		return $"[PlayerUprightMessage Upright={Upright} Subject={SubjectPlayer}]";
	}
}