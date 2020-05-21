using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]

[CreateAssetMenu(fileName = "ActionData", menuName = "ScriptableObjects/ActionData")]
public class ActionData : ScriptableObject
{
	[FormerlySerializedAs("CallOnClient")]
	[SerializeField]
	private bool callOnClient;
	[FormerlySerializedAs("CallOnServer")]
	[SerializeField]
	private bool callOnServer;
	public virtual bool CallOnClient => callOnClient;
	public virtual bool CallOnServer => callOnServer;

	public List<SpriteSheetAndData> Sprites = new List<SpriteSheetAndData>();
	public List<SpriteSheetAndData> Backgrounds = new List<SpriteSheetAndData>();

	public List<ActionController> PreventBeingControlledBy = new List<ActionController>();


	public List<EVENT> DisableOnEvent = new List<EVENT>();

}


public enum ActionController {
	Inventory

}