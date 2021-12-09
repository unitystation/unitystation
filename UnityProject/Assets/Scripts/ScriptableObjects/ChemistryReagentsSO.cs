#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;
using Chemistry;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "ChemistryReagentsSO", menuName = "Singleton/ChemistryReagentsSO")]
	public class ChemistryReagentsSO : SingletonScriptableObject<ChemistryReagentsSO>
	{
		[SerializeField]
		private List<Reagent> allChemistryReagents = new List<Reagent>();

		public List<Reagent> AllChemistryReagents => allChemistryReagents;

		public void Awake()
		{
			for (int i = 0; i < allChemistryReagents.Count; i++)
			{
				if (allChemistryReagents[i] == null)
				{
					Logger.LogError($"The ChemistryReagentsSO singleton has null at the index: {i}.");
					continue;
				}

				if (allChemistryReagents[i].IndexInSingleton != i)
				{
					Logger.LogError($"The reagent {allChemistryReagents[i]} has the wrong singleton index. " +
					                $"Expected: {i}. Found: {allChemistryReagents[i].IndexInSingleton}.");
				}
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(ChemistryReagentsSO))]
	public class ChemistryReagentsSOEditor : Editor
	{
		public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
		{
			List<T> assets = new List<T>();
			string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
			for (int i = 0; i < guids.Length; i++)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
				if (asset != null)
				{
					assets.Add(asset);
				}
			}

			return assets;
		}



		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (GUILayout.Button("Fix reagents' indexes."))
			{
				ChemistryReagentsSO singleton = (ChemistryReagentsSO) target;
				singleton.AllChemistryReagents.Clear();
				singleton.AllChemistryReagents.AddRange(FindAssetsByType<Reagent>());
				for (int i = 0; i < ChemistryReagentsSO.Instance.AllChemistryReagents.Count; i++)
				{
					if (singleton.AllChemistryReagents[i].IndexInSingleton != i)
					{
						singleton.AllChemistryReagents[i].IndexInSingleton = i;
						EditorUtility.SetDirty(singleton.AllChemistryReagents[i]);
					}
				}

				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
		}
	}
#endif
}
