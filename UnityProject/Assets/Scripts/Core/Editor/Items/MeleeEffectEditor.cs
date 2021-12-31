using UnityEditor;
using Weapons;

namespace CustomInspectors 
{
	[CustomEditor(typeof(MeleeEffect))]
	public class MeleeEffectEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			MeleeEffect meleeEffect = (MeleeEffect)target;

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Stun");

			meleeEffect.canStun = EditorGUILayout.Toggle("Can Stun?", meleeEffect.canStun);

			if (meleeEffect.canStun == true)
			{
				EditorGUI.indentLevel++;

				meleeEffect.stunTime = EditorGUILayout.FloatField("Stun Duration", meleeEffect.stunTime);

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Teleport");

			meleeEffect.canTeleport = EditorGUILayout.Toggle("Can Teleport?", meleeEffect.canTeleport);

			if (meleeEffect.canTeleport == true)
			{
				EditorGUI.indentLevel++;

				meleeEffect.avoidSpace = EditorGUILayout.Toggle("Avoid Space?", meleeEffect.avoidSpace);
				meleeEffect.avoidImpassable = EditorGUILayout.Toggle("Avoid Impassables?", meleeEffect.avoidImpassable);
				meleeEffect.minTeleportDistance = EditorGUILayout.IntSlider("Min Teleport distance", meleeEffect.minTeleportDistance, 0, 15);
				meleeEffect.maxTeleportDistance = EditorGUILayout.IntSlider("Max Teleport distance", meleeEffect.maxTeleportDistance, 0, 15);

				EditorGUI.indentLevel--;
			}
		}
	}
}
