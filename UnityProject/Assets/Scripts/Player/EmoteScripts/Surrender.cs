using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/Surrender")]
public class Surrender : EmoteSO
{
	public override void Do(GameObject player)
	{
		if(player.GetComponent<RegisterPlayer>() == null)
		{
			Debug.LogError("RegisterPlayer could not be found!");
			Chat.AddActionMsgToChat(player, $"{failText}", "");
			return;
		}
		if(CheckHandState(player) == false)
		{
			Debug.Log($"[EmoteSO/{this.name}] - Hands not free, doing special surrender text.");
			Chat.AddActionMsgToChat(player, "You surrender in defeat then lay faced down on the ground.", $"{player.ExpensiveName()} surrenders in defeat then lay faced down on the ground.");
		}
		else
		{
			Debug.Log($"[EmoteSO/{this.name}] - Doing reguler emote check.");
			base.Do(player);
		}
		Debug.Log($"[EmoteSO/{this.name}] - Laying down right now.");
		player.GetComponent<RegisterPlayer>().ServerSetIsStanding(false);
	}
}
