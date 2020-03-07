using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

[CreateAssetMenu(fileName = "ActionData", menuName = "ScriptableObjects/ActionData")]
public class ActionData : ScriptableObject
{
	public bool CallOnClient;
	public bool CallOnServer;

	public List<SpriteSheetAndData> Sprites = new List<SpriteSheetAndData>();
	public List<SpriteSheetAndData> Backgrounds = new List<SpriteSheetAndData>();

	public List<ActionController> PreventBeingControlledBy = new List<ActionController>();


	public List<EVENT> DisableOnEvent = new List<EVENT>();

}


public enum ActionController { 
	Inventory

}