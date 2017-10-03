using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Wiring
{
	/// <summary>
	/// This StructurePowerWire editor script helps set the correct
	/// wires when building maps
	/// </summary>
	[CustomEditor(typeof(StructurePowerWire))]
	public class EditorStructurePowerWire : Editor
	{
		public override void OnInspectorGUI()
		{

			StructurePowerWire sTarget = (StructurePowerWire)target;
			serializedObject.Update();

			EditorGUILayout.HelpBox("The starting dir of this wire in a turf, " +
									"using 4 bits to indicate N S E W - 1 2 4 8\r\n" +
									"Corners can also be used i.e.: 5 = NE (1 + 4) = 0101\r\n" +
									"This is the edge of the location where the wire enters the turf", MessageType.Info);
			SerializedProperty DirectionStart = serializedObject.FindProperty("DirectionStart");
			EditorGUILayout.PropertyField((DirectionStart));

			EditorGUILayout.HelpBox("The ending dir of this wire in a turf, " +
									"using 4 bits to indicate N S E W - 1 2 4 8\r\n" +
									"Corners can also be used i.e.: 5 = NE (1 + 4) = 0101\r\n" +
									"This is the edge of the location where the wire exits the turf\r\n" +
									"Can be null of knot wires", MessageType.Info);

			SerializedProperty DirectionEnd = serializedObject.FindProperty("DirectionEnd");
			EditorGUILayout.PropertyField((DirectionEnd));

			SerializedProperty Color = serializedObject.FindProperty("Color");
			EditorGUILayout.PropertyField((Color));

			SerializedProperty TRay = serializedObject.FindProperty("TRay");
			EditorGUILayout.PropertyField((TRay));

			EditorGUILayout.HelpBox("TODO: Create a specific component to handle wiring changes\r\n" +
			                        "via the map editor. Do this by inheriting the component from\r\n" +
			                        "SpriteRotate.cs", MessageType.Warning);
		}

	}
}
