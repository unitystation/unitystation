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
					Loggy.Error($"The ChemistryReagentsSO reagents singleton has null at the index: {i}.");
					continue;
				}

				if (allChemistryReagents[i].IndexInSingleton != i)
				{
					Loggy.Error($"The reagent {allChemistryReagents[i]} has the wrong singleton index. " +
					                $"Expected: {i}. Found: {allChemistryReagents[i].IndexInSingleton}.");
				}
			}

			for (int i = 0; i < allChemistryReactions.Count; i++)
			{
				if (allChemistryReactions[i] == null)
				{
					Loggy.Error($"The ChemistryReagentsSO reactions singleton has null at the index: {i}.");
					continue;
				}
				if (allChemistryReactions[i].IndexInSingleton != i)
				{
					Loggy.Error($"The reaction {allChemistryReactions[i]} has the wrong singleton index. " +
					            $"Expected: {i}. Found: {allChemistryReactions[i].IndexInSingleton}.");
				}
			}
		}

		public void GenerateReagentReactionReferences()
		{
			try
			{
				var reactionMap = new Dictionary<Reagent, List<Reaction>>();

				foreach (var reaction in allChemistryReactions)
				{
					if (reaction == null) continue;

					foreach (var required in reaction.ingredients)
					{
						if (required.Key == null) continue;

						if (reactionMap.ContainsKey(required.Key) == false)
						{
							reactionMap[required.Key] = new List<Reaction>();
						}

						var list = reactionMap[required.Key];

						if (list.Contains(reaction) == false)
						{
							list.Add(reaction);
						}
					}
				}

				foreach (var pair in reactionMap)
				{
					pair.Key.RelatedReactions = pair.Value.ToArray();
				}
			}
			catch (Exception e)
			{
				Loggy.Error(e.ToString());
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

			if (GUILayout.Button("Fix reactions' indexes."))
			{
				ChemistryReagentsSO singleton = (ChemistryReagentsSO) target;
				for (int i = 0; i < ChemistryReagentsSO.Instance.AllChemistryReactions.Count; i++)
				{
					if (singleton.AllChemistryReactions[i].IndexInSingleton != i)
					{
						singleton.AllChemistryReactions[i].IndexInSingleton = i;
						EditorUtility.SetDirty(singleton.AllChemistryReactions[i]);
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
