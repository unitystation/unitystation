using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ElectricalManager))]
public class ElectricalManagerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		ElectricalManager electricalManager = (ElectricalManager)target;

		GUIContent speedContent = new GUIContent("Speed", "frequency of Atmos simulation updates (Millieseconds between each update)");

		electricalManager.MSSpeed = EditorGUILayout.Slider(speedContent, electricalManager.MSSpeed, 0, 5000);

		AddButtonGroup(electricalManager);

		DrawDefaultInspector();
	}


	private static void AddButtonGroup(ElectricalManager electricalManager)
	{
		EditorGUILayout.BeginHorizontal();

		GUI.enabled = Application.isPlaying && electricalManager.Mode != ElectricalMode.Manual;

		if (GUILayout.Button("SetSpeed"))
		{
			AtmosManager.SetInternalSpeed();
		}

		if (!electricalManager.Running)
		{
			if (GUILayout.Button("Start"))
			{
				electricalManager.StartSimulation();
			}
		}
		else if (GUILayout.Button("Stop"))
		{
			electricalManager.StopSimulation();
		}

		GUI.enabled = Application.isPlaying && !electricalManager.Running;

		if (GUILayout.Button("Step"))
		{
			ElectricalSynchronisation.RunStep(false);
		}

		GUI.enabled = true;

		EditorGUILayout.EndHorizontal();
	}
}
#endif