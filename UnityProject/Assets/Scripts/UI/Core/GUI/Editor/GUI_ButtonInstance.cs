using UnityEditor;


	abstract public class GUI_ButtonInstance : GUI_ComponentInstance
	{

		[MenuItem("GameObject/UI/GUI/Button")]
		[MenuItem("UI/GUI/Button")]
		public static void AddComponent()
		{
			Create("UI/GUI/Button", "Button");
		}
	}

