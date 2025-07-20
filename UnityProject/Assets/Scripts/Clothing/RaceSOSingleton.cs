using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ScriptableObjects;

#if UNITY_EDITOR
using UnityEditor;
#endif


[CreateAssetMenu(fileName = "RaceSOSingleton", menuName = "Singleton/RaceSOSingleton")]
public class RaceSOSingleton : SingletonScriptableObject<RaceSOSingleton>
{
	//do Config stuff to allow Certain ones
	public List<PlayerHealthData> Races = new();

	public static bool TryGetRaceByName(string raceName, out PlayerHealthData race)
	{
		foreach (var potentialRace in Instance.Races)
		{
			if (potentialRace.name == raceName)
			{
				race = potentialRace;
				return true;
			}
		}

		race = null;
		return false;
	}

	public static List<PlayerHealthData> GetPlayerSpecies()
	{
		return Instance.Races.Where(specie => specie.Base.CanBePlayerChosen).ToList();
	}

	public static List<PlayerHealthData> GetAllSpecies()
	{
		return Instance.Races;
	}
}

#if UNITY_EDITOR

[CustomEditor(typeof(RaceSOSingleton))]
public class RaceSOSingletonEditor : UnityEditor.Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		var raceSingleton = (RaceSOSingleton)target;

		if (GUILayout.Button("Find All Species"))
		{
			// Find all PlayerHealthData assets in the project
			string[] guids = AssetDatabase.FindAssets("t:PlayerHealthData");
			var foundSpecies = guids
				.Select(guid => AssetDatabase.LoadAssetAtPath<PlayerHealthData>(
					AssetDatabase.GUIDToAssetPath(guid)))
				.Where(species => species != null)
				.ToList();

			// Add only species that aren't already in the list
			bool modified = false;
			foreach (var species in foundSpecies)
			{
				if (!raceSingleton.Races.Contains(species))
				{
					raceSingleton.Races.Add(species);
					modified = true;
				}
			}

			if (modified)
			{
				EditorUtility.SetDirty(raceSingleton);
				AssetDatabase.SaveAssets();
			}

			Debug.Log($"Found {foundSpecies.Count} species. Added {foundSpecies.Count - raceSingleton.Races.Count + (modified ? 1 : 0)} new species.");
		}
	}
}
#endif