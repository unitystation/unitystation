using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if true
using UnityEditor;
#endif

namespace SecureStuff
{
	[System.Serializable]
	public class SerializedAction
	{
		[SerializeField] private UnityEngine.Object target;
		[SerializeField] private string methodName;

		private Action cachedAction;

		public void Invoke()
		{
			if (cachedAction == null) Cache();
			cachedAction?.Invoke();
		}

		private void Cache()
		{
			if (target == null || string.IsNullOrEmpty(methodName))
				return;

			var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			if (method != null && method.GetParameters().Length == 0)
			{
				cachedAction = (Action)Delegate.CreateDelegate(typeof(Action), target, method);
			}
			else
			{
				Debug.LogWarning($"Method '{methodName}' on {target} not found or has parameters.");
			}
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(SerializedAction))]
	public class SerializedActionDrawer : PropertyDrawer
	{
		private const BindingFlags methodFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			var targetProp = property.FindPropertyRelative("target");
			var methodProp = property.FindPropertyRelative("methodName");

			Rect objectRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			Rect methodRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);

			EditorGUI.PropertyField(objectRect, targetProp, new GUIContent(label.text + " Target"));

			UnityEngine.Object targetObj = targetProp.objectReferenceValue;
			List<string> methodNames = new List<string> { "<None>" };
			int selectedIndex = 0;

			if (targetObj != null)
			{
				MethodInfo[] methods = targetObj.GetType().GetMethods(methodFlags);

				foreach (var m in methods)
				{
					if (m.ReturnType == typeof(void) && m.GetParameters().Length == 0)
					{
						methodNames.Add(m.Name);
						if (m.Name == methodProp.stringValue)
							selectedIndex = methodNames.Count - 1;
					}
				}
			}

			int newIndex = EditorGUI.Popup(methodRect, "Method", selectedIndex, methodNames.ToArray());

			if (newIndex != selectedIndex)
			{
				methodProp.stringValue = newIndex == 0 ? "" : methodNames[newIndex];
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight * 2 + 4;
		}
	}
#endif
}