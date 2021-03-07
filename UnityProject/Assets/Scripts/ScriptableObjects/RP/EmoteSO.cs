using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AddressableReferences;

public class EmoteSO : ScriptableObject
{
	[Tooltip("The name of the emote that triggers it's behavior when called under /me or *, never leave it blank!")]
	public string emoteName = "";
	[SerializeField, Tooltip("The emote text viewed by others around you.")]
	private string viewText = "did something!";
	[SerializeField, Tooltip("The emote text viewed by you only. Leave blank if you don't want text to appear.")]
	private string youText = "";

	[SerializeField, Tooltip("A list of sounds that can be played when this emote happens, " +
		"Mainly used for simple emotes that don't require gender/Job checks.")]
	private List<AddressableAudioSource> emoteDefaultSounds = new List<AddressableAudioSource>();

	public virtual void Do(GameObject player)
	{
		Chat.AddActionMsgToChat(player, "", $"{player.ExpensiveName()} {viewText}.");
	}
}
