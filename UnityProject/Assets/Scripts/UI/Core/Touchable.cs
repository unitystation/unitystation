using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI.Core
{
#if UNITY_EDITOR
	[CustomEditor(typeof(Touchable))]
	public class Touchable_Editor : Editor
	{
		// The method signature itself is required; see <see cref="Touchable"/>'s docstring.
		public override void OnInspectorGUI()
		{
			EditorGUILayout.HelpBox("Touchable allows transparent buttons to be clickable.", MessageType.Info);
		}
	}
#endif

	///<summary>
	/// <para>Correctly backfills the missing Touchable concept in Unity.UI's OO chain.</para>
	/// Courtesy of <see href="https://stackoverflow.com/questions/36888780/how-to-make-an-invisible-transparent-button-work"/>.
	/// </summary>
	[RequireComponent(typeof(CanvasRenderer))]
	public class Touchable : Graphic
	{
		protected override void UpdateGeometry() { }
	}
}
