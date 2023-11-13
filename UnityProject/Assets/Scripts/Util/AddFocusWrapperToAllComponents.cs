using TMPro;
using UnityEditor;
using UnityEngine;

namespace Util
{
	public static class AddFocusWrapperToAllComponents
	{
		[MenuItem("Tools/UI/AddFocusWrapperToAllInputFields")]
		public static void UpdateAllGameObjects()
		{
			Component[] componentsWithSprite = Resources.FindObjectsOfTypeAll<Component>();
			foreach (Component component in componentsWithSprite)
			{
				var piss = component.GetComponentsInChildren<TMP_InputField>();
				Debug.Log($"found {piss.Length} input fields.");
				foreach (var fuck in piss)
				{
					if (fuck.gameObject.HasComponent<TMPInputFocusWrapper>()) continue;
					fuck.gameObject.AddComponent<TMPInputFocusWrapper>();
					Debug.Log($"added wrapper for {fuck.gameObject.name}.");
				}
			}
		}
	}
}