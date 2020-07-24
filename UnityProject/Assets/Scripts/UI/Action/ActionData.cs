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

	[FormerlySerializedAs("spellName")]
	[SerializeField] protected string actionName = "";
	[SerializeField] protected string description = "";
	public string Name => actionName;
	public string Description => description;

	public List<SpriteDataSO>  Sprites = null;
	public List<SpriteDataSO> Backgrounds = null;

	public List<ActionController> PreventBeingControlledBy = new List<ActionController>();


	public List<EVENT> DisableOnEvent = new List<EVENT>();

	public override string ToString()
	{
		if (SpellList.Instance && this == SpellList.Instance.InvalidData)
		{
			return "[InvalidData]";
		}
		return $"[ActionData '{Name}' ({Description})]";
	}
}


public enum ActionController {
	Inventory

}