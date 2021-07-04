using UnityEditor;
using UnityEngine;

namespace Core.Editor
{
	/// <summary>
	/// An assortment of useful, generic methods for Unity editor tool construction
	/// </summary>
	public static class EditorUIUtils
	{
		/// <summary>A label style with rich-text and word-wrap enabled.</summary>
		public static GUIStyle LabelStyle {
			get {

				if (labelStyle == null)
				{
					labelStyle = new GUIStyle(GUI.skin.label);
					labelStyle.richText = true;
					labelStyle.wordWrap = true;
				}

				return labelStyle;
			}
		}
		private static GUIStyle labelStyle;

		/// <summary>
		/// Generates a big button. The button is centered horizontally.
		/// </summary>
		/// <param name="name">The name this button should be labeled with</param>
		/// <returns>True if the button was clicked</returns>
		public static bool BigAssButton(string name)
		{
			GUIStyle buildBtnStyle = new GUIStyle(GUI.skin.button);
			buildBtnStyle.padding = new RectOffset(30, 30, 8, 8);

			GUIContent btnTxt = new GUIContent(name);
			Rect rect = GUILayoutUtility.GetRect(btnTxt, buildBtnStyle, GUILayout.ExpandWidth(false));
			rect.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, rect.center.y);

			return GUI.Button(rect, btnTxt, GUI.skin.button);
		}
	}
}
