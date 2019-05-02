using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public enum StunnedState
{
	Unknown = 0,
	Stunned = 1,
	NonStunned = 2
}

///   Tells client to make given player appear laying down or back up on feet
public class PlayerUprightMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.PlayerUprightMessage;
	public bool Upright;
	public StunnedState Stunned;
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

		if ( Stunned != StunnedState.Unknown )
		{
			registerPlayer.IsStunnedClient = Stunned == StunnedState.Stunned;
		}
	}

	public static PlayerUprightMessage Send(GameObject recipient, GameObject subjectPlayer, bool upright, bool isStunned)
	{
		if (!IsValid(subjectPlayer, upright))
		{
			return null;
		}
		var msg = new PlayerUprightMessage
		{
			SubjectPlayer = subjectPlayer.NetId(),
			Upright = upright,
			Stunned = isStunned ? StunnedState.Stunned : StunnedState.NonStunned
		};
		msg.SendTo(recipient);
		return msg;
	}

	/// <summary>
	/// Stunned info will ONLY be sent to subject!
	/// </summary>
	/// <param name="subjectPlayer"></param>
	/// <param name="upright"></param>
	/// <param name="isStunned"></param>
	public static void SendToAll(GameObject subjectPlayer, bool upright, bool isStunned)
	{
		if (!IsValid(subjectPlayer, upright))
		{
			return;
		}
		var msg = new PlayerUprightMessage
		{
			SubjectPlayer = subjectPlayer.NetId(),
			Upright = upright,
			Stunned = StunnedState.Unknown
		};
		msg.SendToAllExcept( subjectPlayer );
		msg.Stunned = isStunned ? StunnedState.Stunned : StunnedState.NonStunned;
		msg.SendTo( subjectPlayer );
	}

	private static bool IsValid(GameObject subjectPlayer, bool upright)
	{
		//checks if player is actually in a state where they can become up / down
		var playerScript = subjectPlayer.GetComponent<PlayerScript>();
		if (!upright)
		{
			//cannot lay down if they are restrained
			if (playerScript.playerMove.IsRestrained)
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