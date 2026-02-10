using UnityEngine;
using UnityEditor;


	abstract public class GUI_ComponentInstance : Editor
	{

		static GameObject parentObject;

		protected static GameObject Create(string prefabPath, string objectName)
		{
			GameObject GuiComponent = Instantiate(Resources.Load<GameObject>(prefabPath));
			GuiComponent.name = objectName;

			parentObject = Selection.activeGameObject as GameObject;
			if (parentObject != null)
			{
				GuiComponent.transform.SetParent(parentObject.transform, false);
			}

			return GuiComponent;
		}
	}

