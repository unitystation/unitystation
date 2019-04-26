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
			registerPlayer.LayDown();
		}

		if ( Stunned != StunnedState.Unknown )
		{
			registerPlayer.IsStunnedClient = Stunned == StunnedState.Stunned;
		}
	}

	public static PlayerUprightMessage Send(GameObject recipient, GameObject subjectPlayer, bool state, bool isStunned)
	{
		var msg = new PlayerUprightMessage
		{
			SubjectPlayer = subjectPlayer.NetId(),
			Upright = state,
			Stunned = isStunned ? StunnedState.Stunned : StunnedState.NonStunned
		};
		msg.SendTo(recipient);
		return msg;
	}

	/// <summary>
	/// Stunned info will ONLY be sent to subject!
	/// </summary>
	/// <param name="subjectPlayer"></param>
	/// <param name="state"></param>
	/// <param name="isStunned"></param>
	public static void SendToAll(GameObject subjectPlayer, bool state, bool isStunned)
	{
		var msg = new PlayerUprightMessage
		{
			SubjectPlayer = subjectPlayer.NetId(),
			Upright = state,
			Stunned = StunnedState.Unknown
		};
		msg.SendToAllExcept( subjectPlayer );
		msg.Stunned = isStunned ? StunnedState.Stunned : StunnedState.NonStunned;
		msg.SendTo( subjectPlayer );
	}

	public override string ToString()
	{
		return $"[PlayerUprightMessage Upright={Upright} Subject={SubjectPlayer}]";
	}
}