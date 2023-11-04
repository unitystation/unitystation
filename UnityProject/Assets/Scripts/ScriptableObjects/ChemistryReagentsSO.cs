#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Chemistry;
using Logs;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "ChemistryReagentsSO", menuName = "Singleton/ChemistryReagentsSO")]
	public class ChemistryReagentsSO : SingletonScriptableObject<ChemistryReagentsSO>
	{



		[SerializeField]
		private List<Reaction> allChemistryReactions = new List<Reaction>();

		public List<Reaction> AllChemistryReactions => allChemistryReactions;


		[SerializeField]
		private List<Reagent> allChemistryReagents = new List<Reagent>();

		public List<Reagent> AllChemistryReagents => allChemistryReagents;

		public void Awake()
		{
			for (int i = 0; i < allChemistryReagents.Count; i++)
			{
				if (allChemistryReagents[i] == null)
				{
					Loggy.LogError($"The ChemistryReagentsSO singleton has null at the index: {i}.");
					continue;
				}

				if (allChemistryReagents[i].IndexInSingleton != i)
				{
					Loggy.LogError($"The reagent {allChemistryReagents[i]} has the wrong singleton index. " +
					                $"Expected: {i}. Found: {allChemistryReagents[i].IndexInSingleton}.");
				}
			}
		}

		public void GenerateReagentReactionReferences()
		{
			try
			{
				foreach (var reaction in allChemistryReactions)
				{
					foreach (var required in reaction.ingredients)
					{
						required.Key.RelatedReactions = new Reaction[0];
					}
				}

				foreach (var reaction in allChemistryReactions)
				{
					foreach (var required in reaction.ingredients)
					{
						if (required.Key == null) continue;
						if (required.Key.RelatedReactions == null)
						{
							required.Key.RelatedReactions = new Reaction[0];
						}
						required.Key.RelatedReactions = required.Key.RelatedReactions.Append(reaction).ToArray();
					}
				}
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
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

				EditorUtility.SetDirty(singleton);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}


			if (GUILayout.Button("Collect all Reagents"))
			{
				ChemistryReagentsSO singleton = (ChemistryReagentsSO) target;
				singleton.AllChemistryReagents.Clear();
				singleton.AllChemistryReagents.AddRange(FindAssetsByType<Reagent>());
				EditorUtility.SetDirty(singleton);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			if (GUILayout.Button("Collect all reactions"))
			{
				ChemistryReagentsSO singleton = (ChemistryReagentsSO) target;
				singleton.AllChemistryReactions.Clear();
				singleton.AllChemistryReactions.AddRange(FindAssetsByType<Reaction>());
				EditorUtility.SetDirty(singleton);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
		}
	}
#endif
}
