using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIRightClickButton : MonoBehaviour {
	public class ButtonEntry
	{
		public delegate void Action();
		public string Title;
	}
	public List<ButtonEntry> StoredButtons = new List<ButtonEntry>();
}
