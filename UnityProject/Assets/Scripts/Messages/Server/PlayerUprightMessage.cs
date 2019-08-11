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