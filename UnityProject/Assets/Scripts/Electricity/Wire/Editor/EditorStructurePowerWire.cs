using UnityEditor;
using UnityEngine;


	/// <summary>
	///     This StructurePowerWire editor script helps set the correct
	///     wires when building maps
	/// </summary>
	[CustomEditor(typeof(CableInheritance))]
	public class EditorStructurePowerWire : Editor
	{
		private float msgTime;
		private bool showError;
		private Connection startCache;
		private Connection endCache;

		public override void OnInspectorGUI()
		{
			CableInheritance sTarget = (CableInheritance) target;
			startCache = sTarget.WireEndB;
			endCache = sTarget.WireEndA;

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.HelpBox(
				"yeah Enum are Great, The order is done by clockwise starting at north then going to middle then machine connect", MessageType.Info);
			sTarget.WireEndB = (Connection)EditorGUILayout.EnumPopup(sTarget.WireEndB);

			EditorGUILayout.HelpBox(
				"yeah Enum are Great, The order is done by clockwise starting at north then going to middle then machine connect", MessageType.Info);

			sTarget.WireEndA = (Connection)EditorGUILayout.EnumPopup(sTarget.WireEndA);

			sTarget.CableType = (WiringColor) EditorGUILayout.EnumPopup("Wiring Color: ", sTarget.CableType);

			if (EditorGUI.EndChangeCheck())
			{
				try
				{
					sTarget.SetDirection(sTarget.WireEndB, sTarget.WireEndA, sTarget.CableType);
					showError = false;
					PrefabUtility.RecordPrefabInstancePropertyModifications(sTarget);
				}
				catch
				{
					msgTime = 0f;
					showError = true;
					//sTarget.WireEndB = startCache;
					//sTarget.WireEndA = endCache;
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
