#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Antagonists;

[CustomEditor(typeof(Objective), true)]
public class ObjectiveEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		var objective = (Objective)target;

		if (GUILayout.Button("AddAttributePlayer"))
		{
			objective.attributes.Add(new ObjectiveAttributePlayer());
		} else if (GUILayout.Button("AddAttributeNumber"))
		{
			objective.attributes.Add(new ObjectiveAttributeNumber());
		} else if (GUILayout.Button("AddAttributeItem"))
		{
			objective.attributes.Add(new ObjectiveAttributeItem());
		}  else if (GUILayout.Button("AddAttributeItemTrait"))
		{
			objective.attributes.Add(new ObjectiveAttributeItemTrait());
		} 
	}
}
#endif