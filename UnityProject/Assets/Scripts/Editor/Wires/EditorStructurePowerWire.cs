using Sprites;
using UnityEditor;
using UnityEngine;

namespace Wiring
{
	/// <summary>
	///     This StructurePowerWire editor script helps set the correct
	///     wires when building maps
	/// </summary>
	[CustomEditor(typeof(StructurePowerWire))]
	public class EditorStructurePowerWire : Editor
	{
		private int endCache;
		private float msgTime;
		private bool showError;
		private int startCache;

		public override void OnInspectorGUI()
		{
			StructurePowerWire sTarget = (StructurePowerWire) target;
			startCache = sTarget.DirectionStart;
			endCache = sTarget.DirectionEnd;

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.HelpBox(
				"The starting dir of this wire in a turf, " + "using 4 bits to indicate N S E W - 1 2 4 8\r\n" +
				"Corners can also be used i.e.: 5 = NE (1 + 4) = 0101\r\n" + "This is the edge of the location where the wire enters the turf", MessageType.Info);
			sTarget.DirectionStart = EditorGUILayout.IntField("DirectionStart: ", sTarget.DirectionStart);

			EditorGUILayout.HelpBox(
				"The ending dir of this wire in a turf, " + "using 4 bits to indicate N S E W - 1 2 4 8\r\n" +
				"Corners can also be used i.e.: 5 = NE (1 + 4) = 0101\r\n" + "This is the edge of the location where the wire exits the turf\r\n" +
				"Can be null of knot wires", MessageType.Info);

			sTarget.DirectionEnd = EditorGUILayout.IntField("DirectionEnd: ", sTarget.DirectionEnd);

			sTarget.Color = (WiringColor) EditorGUILayout.EnumPopup("Wiring Color: ", sTarget.Color);

			if (EditorGUI.EndChangeCheck())
			{
				try
				{
					sTarget.SetDirection(sTarget.DirectionStart, sTarget.DirectionEnd);
					showError = false;
				}
				catch
				{
					msgTime = 0f;
					showError = true;
					sTarget.DirectionStart = startCache;
					sTarget.DirectionEnd = endCache;
				}
			}

			if (showError)
			{
				msgTime += Time.deltaTime;
				if (msgTime > 3f)
				{
					showError = false;
					msgTime = 0f;
				}
				EditorGUILayout.HelpBox("Incorrect start and end combination", MessageType.Error);
			}
			SerializedProperty TRay = serializedObject.FindProperty("TRay");
			EditorGUILayout.PropertyField(TRay, true);

			EditorGUILayout.HelpBox(
				"TODO: Create a specific component to handle wiring changes\r\n" + "via the map editor. Do this by inheriting the component from\r\n" +
				"SpriteRotate.cs", MessageType.Warning);
		}
	}
}