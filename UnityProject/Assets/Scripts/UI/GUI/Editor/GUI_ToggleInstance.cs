using UnityEditor;


	abstract public class GUI_ToggleInstance : GUI_ComponentInstance
	{

		[MenuItem("GameObject/UI/GUI/Toggle")]
		[MenuItem("UI/GUI/Toggle")]
		public static void AddComponent()
		{
			Create("UI/GUI/Toggle", "Toggle");
		}
	}

