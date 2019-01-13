using UnityEditor;

[CustomEditor(typeof(AtmosSystem))]
public class AtmosSystemEditor : Editor
{
	public override void OnInspectorGUI()
	{
		AtmosSystem atmosSystem = (AtmosSystem) target;

		atmosSystem.Speed = EditorGUILayout.Slider("Speed", atmosSystem.Speed, 0.01f, 1f);

		EditorGUILayout.LabelField("Update List Count", atmosSystem.GetUpdateListCount().ToString());

		EditorUtility.SetDirty(atmosSystem);
	}
}