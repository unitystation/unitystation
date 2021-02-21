using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Mirror;
using UnityEngine;

public class SuicideMessage : ClientMessage
{
	public class SuicideMessageNetMessage : NetworkMessage { }

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as SuicideMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		if (SentByPlayer.Script.TryGetComponent<LivingHealthBehaviour>(out var livingHealthBehaviour))
		{
			if (livingHealthBehaviour.IsDead)
			{
				Logger.LogError("Player '" + SentByPlayer.Name + "' is attempting to commit suicide but is already dead.", Category.Health);
			}
			else
			{
				Logger.Log("Player '" + SentByPlayer.Name + "' has committed suicide", Category.Health);
				livingHealthBehaviour.ApplyDamage(null, float.MaxValue, AttackType.Melee, DamageType.Brute);
			}
		}
	}


	/// <summary>
	/// Tells the server to kill the player that sent this message
	/// </summary>
	/// <param name="obj">Dummy variable that is required to make this signiture different
	/// from the non-static function of the same name. Just pass null. </param>
	/// <returns></returns>
	public static SuicideMessageNetMessage Send(Object obj)
	{
		SuicideMessageNetMessage msg = new SuicideMessageNetMessage();
		new SuicideMessage().Send(msg);
		return msg;
	}


}
