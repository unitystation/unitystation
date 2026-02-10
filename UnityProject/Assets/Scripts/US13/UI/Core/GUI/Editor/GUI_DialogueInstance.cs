using UnityEditor;


	abstract public class GUI_DialogueInstance : GUI_ComponentInstance
	{

		[MenuItem("GameObject/UI/GUI/Dialogue")]
		[MenuItem("UI/GUI/Dialogue")]
		public static void AddComponent()
		{
			Create("UI/GUI/Dialogue", "Dialogue");
		}
	}

