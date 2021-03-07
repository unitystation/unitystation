using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmoteSO : ScriptableObject
{
	public string emoteName = "";
	public string viewText = "did something!";
	public string youText = "";

	public List<AudioClip> clips;

	public virtual void Do(GameObject player)
	{
		Chat.AddActionMsgToChat(player, "", $"{player.name} {viewText}.");
	}
}
