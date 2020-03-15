using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

abstract public class GUI_ChatScrollWithInput : GUI_ComponentInstance
{
	[MenuItem("GameObject/UI/GUI/ChatScrollWithInput")]
	[MenuItem("UI/GUI/ChatScrollWithInput")]
	public static void AddComponent()
	{
		Create("UI/GUI/ChatScrollWithInput", "ChatScrollWithInput");
	}
}
