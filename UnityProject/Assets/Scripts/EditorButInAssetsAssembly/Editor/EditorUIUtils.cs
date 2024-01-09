#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Shared.Editor
{
	/// <summary>
	/// An assortment of useful, generic methods for Unity editor tool construction
	/// </summary>
	public static class EditorUIUtils
	{
		/// <summary>
		/// A label style with rich-text and word-wrap enabled.
		/// </summary>
		public static GUIStyle LabelStyle {
			get {
				if (labelStyle != null) return labelStyle;

				labelStyle = new GUIStyle(GUI.skin.label)
				{
					richText = true,
					wordWrap = true
				};

				return labelStyle;
			}
		}

		private static GUIStyle labelStyle;

		/// <summary>
		/// Generates a big button. The button is centered horizontally.
		/// </summary>
		/// <param name="name">The name this button should be labeled with</param>
		/// <returns>True if the button was clicked</returns>
		public static bool BigButton(string name)
		{
			var skin = GUI.skin;
			var buildBtnStyle = new GUIStyle(skin.button)
			{
				padding = new RectOffset(30, 30, 8, 8)
			};

			var btnTxt = new GUIContent(name);
			var rect = GUILayoutUtility.GetRect(btnTxt, buildBtnStyle, GUILayout.ExpandWidth(false));
			rect.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, rect.center.y);

			return GUI.Button(rect, btnTxt, skin.button);
		}
	}
}
#endif