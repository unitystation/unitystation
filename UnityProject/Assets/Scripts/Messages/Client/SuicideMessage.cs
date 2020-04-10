using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuicideMessage : ClientMessage
{
	public override void Process()
	{

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
	public static SuicideMessage Send(Object obj)
	{
		SuicideMessage msg = new SuicideMessage();
		msg.Send();
		return msg;
	}


}
