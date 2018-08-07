using UnityEditor;


	abstract public class GUI_TextInputInstance : GUI_ComponentInstance
	{

		[MenuItem("GameObject/UI/GUI/TextInput")]
		[MenuItem("UI/GUI/TextInput")]
		public static void AddComponent()
		{
			Create("UI/GUI/TextInput", "TextInput");
		}
	}

