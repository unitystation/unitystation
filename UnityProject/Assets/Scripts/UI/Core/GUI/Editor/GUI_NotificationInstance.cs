using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

abstract public class GUI_NotificationInstance : GUI_ComponentInstance
{
	[MenuItem("GameObject/UI/GUI/NotificationCounter")]
	[MenuItem("UI/GUI/NotificationCounter")]
	public static void AddComponent()
	{
		Create("UI/GUI/NotificationCounter", "NotificationCounter");
	}
}
