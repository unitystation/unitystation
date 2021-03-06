using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects")]
public class EmoteSO : ScriptableObject
{
	public string emote;
	public string viewText;
	public string youText = "";

	public Emote emoteScript;
}
