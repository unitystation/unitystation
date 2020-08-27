using System.Threading;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AtmosManager))]
public class AtmosManagerEditor : Editor
{

	public override void OnInspectorGUI()
	{
		AtmosManager atmosManager = (AtmosManager) target;

		GUIContent speedContent = new GUIContent("Speed", "frequency of Atmos simulation updates (Millieseconds between each update)");
		GUIContent SubOperations = new GUIContent("SubOperations", "The number of operations done in each update ( meant for sub millisecond adjustment )");
		GUIContent numThreadsContent =
			new GUIContent("Threads", "not currently implemented, thread count is always locked at one regardless of this setting");

		atmosManager.Mode = (AtmosMode)EditorGUILayout.EnumPopup("Mode", atmosManager.Mode);

		atmosManager.Speed = EditorGUILayout.Slider(speedContent, atmosManager.Speed, 0, 5000);

		GUI.enabled = atmosManager.Mode == AtmosMode.Threaded;

		atmosManager.NumberThreads = EditorGUILayout.IntSlider(numThreadsContent, atmosManager.NumberThreads, 1, 1);

		GUI.enabled = true;

		AddButtonGroup(atmosManager);

		EditorGUILayout.LabelField("Update List Count", AtmosThread.GetUpdateListCount().ToString());
		DrawDefaultInspector ();
	}

	private static void AddButtonGroup(AtmosManager atmosManager)
	{
		EditorGUILayout.BeginHorizontal();

		GUI.enabled = Application.isPlaying && atmosManager.Mode != AtmosMode.Manual;

		if (GUILayout.Button("SetSpeed"))
		{
			AtmosManager.SetInternalSpeed();
		}

		if (!atmosManager.Running)
		{
			if (GUILayout.Button("Start"))
			{
				atmosManager.StartSimulation();
			}
		}
		else if (GUILayout.Button("Stop"))
		{
			atmosManager.StopSimulation();
		}

		GUI.enabled = Application.isPlaying && !atmosManager.Running;

		if (GUILayout.Button("Step"))
		{
			AtmosThread.RunStep();
		}

		GUI.enabled = true;

		EditorGUILayout.EndHorizontal();
	}
}