using UnityEditor;

[CustomEditor(typeof(AtmosManager))]
public class AtmosManagerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		AtmosManager atmosManager = (AtmosManager) target;

		atmosManager.Speed = EditorGUILayout.Slider("Speed", atmosManager.Speed, 0.01f, 1f);
		atmosManager.NumberThreads = EditorGUILayout.IntSlider("Threads", atmosManager.NumberThreads, 1, 1);

		EditorGUILayout.LabelField("Update List Count", AtmosThread.GetUpdateListCount().ToString());

		EditorUtility.SetDirty(atmosManager);
	}
}