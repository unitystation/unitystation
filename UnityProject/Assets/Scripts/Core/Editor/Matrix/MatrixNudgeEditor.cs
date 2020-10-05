using UnityEditor;
using UnityEngine;

[CustomEditor( typeof( Matrix ) )]
public class MatrixNudgeEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		EditorGUILayout.HelpBox("Nudge pivot:", MessageType.Info);
		Matrix matrix = (Matrix)target;
		if(GUILayout.Button("Up"))
		{
			Nudge( matrix, Vector3Int.up );
		}
		if(GUILayout.Button("Down"))
		{
			Nudge( matrix, Vector3Int.down );
		}
		if(GUILayout.Button("Left"))
		{
			Nudge( matrix, Vector3Int.left );
		}
		if(GUILayout.Button("Right"))
		{
			Nudge( matrix, Vector3Int.right );
		}
		if(GUILayout.Button("Compress all Bounds"))
		{
			matrix.CompressAllBounds();
		}
	}

	private void Nudge( Matrix script, Vector3Int dir )
	{
		script.transform.position -= dir;
		script.transform.parent.position += dir;
	}
}