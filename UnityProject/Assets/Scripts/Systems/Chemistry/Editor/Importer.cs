using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Logs;
using UnityEditor;
using UnityEngine;
using YamlDotNet.Serialization;
using Unity.EditorCoroutines.Editor;

namespace Chemistry.Editor
{
	public class Importer : EditorWindow
	{
		Deserializer deserializer = (Deserializer) new DeserializerBuilder().Build();
		private string reagentExportPath;
		private string reactionExportPath;
		private string reactionSetExportPath;

		private string reagentsPath;
		private string reactionPath;
		private int progress;
		private int maxProgress;
		private bool cancel;
		private bool overwrite;


		[MenuItem("Window/Chemistry Importer")]
		static void ShowWindow() => GetWindow<Importer>("Chemistry Importer");

		void OnGUI()
		{
			ReagentsGui();
			ReactionsGui();

			reagentExportPath = EditorGUILayout.TextField("Reagent export path", reagentExportPath);
			reactionExportPath = EditorGUILayout.TextField("Reaction export path", reactionExportPath);
			reactionSetExportPath = EditorGUILayout.TextField("Reaction set export path", reactionSetExportPath);
			overwrite = GUILayout.Toggle(overwrite, "Overwrite existing data");

			EditorGUI.BeginDisabledGroup(
				!Directory.Exists(reagentsPath) ||
				!Directory.Exists(reactionPath) ||
				!Directory.Exists(reagentExportPath));

			if (GUILayout.Button("Save"))
			{
				this.StartCoroutine(Export());
			}

			if (progress != 0)
			{
				if (EditorUtility.DisplayCancelableProgressBar("Chemistry import", "Importing chemicals...",
					progress / (float) maxProgress))
				{
					cancel = true;
				}
			}
			else
			{
				EditorUtility.ClearProgressBar();
			}

			EditorGUI.EndDisabledGroup();
		}

		private void ReagentsGui()
		{
			EditorGUILayout.BeginHorizontal();
			reagentsPath = EditorGUILayout.TextField("Reagents path", reagentsPath);
			if (Directory.Exists(reagentsPath))
			{
				EditorGUILayout.LabelField($"Contains {Directory.GetFiles(reagentsPath, "*.yml").Length} reagents");
			}

			EditorGUILayout.EndHorizontal();
		}

		private void ReactionsGui()
		{
			EditorGUILayout.BeginHorizontal();
			reactionPath = EditorGUILayout.TextField("Reactions path", reactionPath);
			if (Directory.Exists(reactionPath))
			{
				EditorGUILayout.LabelField($"Contains {Directory.GetFiles(reactionPath, "*.yml").Length} reactions");
			}

			EditorGUILayout.EndHorizontal();
		}

		private IEnumerator Export()
		{
			progress = 0;
			var reagentFiles = Directory.EnumerateFiles(reagentsPath, "*.yml");

			var reagentGroups = reagentFiles
				.Select(file => (file,
					data: deserializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(
						File.ReadAllText(file))))
				.Select(reagents => new Grouping<string, KeyValuePair<string, Reagent>>(reagents.file,
					reagents.data
						.Where(dict => dict.Value.ContainsKey("name"))
						.ToDictionary(reagentData => reagentData.Key, ToReagent)))
				.ToArray();

			var flatReagents = reagentGroups
				.SelectMany(dict => dict)
				.ToDictionary(pair => pair.Key, pair => pair.Value);

			foreach (var reagentsGroup in reagentGroups)
			{
				var prefix = ToPascalCase(Path.GetFileNameWithoutExtension(reagentsGroup.Key))
					.Replace("Reagents", "");
				Loggy.Log(prefix, Category.Editor);
				var prefixPath = Path.Combine(reagentExportPath, prefix);
				if (!Directory.Exists(prefixPath))
				{
					Directory.CreateDirectory(prefixPath);
				}

				foreach (var reagent in reagentsGroup)
				{
					var path = Path.Combine(prefixPath, ToPascalCase(reagent.Value.Name) + ".asset");
					var localPath = LocalPath(path);

					if (!File.Exists(path) || overwrite)
					{
						AssetDatabase.CreateAsset(reagent.Value, path);
					}
					else
					{
						var existAsset = AssetDatabase.LoadAssetAtPath<Reagent>(localPath);
						if (existAsset)
							flatReagents[reagent.Key] = existAsset;
					}


					progress++;
					if (cancel)
					{
						progress = 0;
						cancel = false;
						yield break;
					}

					yield return new EditorWaitForSeconds(0f);
				}
			}


			var reactionFiles = Directory.EnumerateFiles(reactionPath, "*.yml")
				.ToArray();

			var reactionSetsData = new Dictionary<string, List<Reaction>>();

			var reactionGroups = reactionFiles
				.Select(file => (file,
					data: deserializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(
						File.ReadAllText(file))))
				.Select(reactions => new Grouping<string, KeyValuePair<string, Reaction>>(
					reactions.file,
					reactions.data
						.ToDictionary(r => r.Key, r => ToReaction(r, flatReagents, reactionSetsData))))
				.ToArray();

			maxProgress = flatReagents.Count + reactionGroups.Sum(r => r.Count());



			foreach (var reactionsGroup in reactionGroups)
			{
				var prefix = ToPascalCase(Path.GetFileNameWithoutExtension(reactionsGroup.Key));
				var prefixPath = Path.Combine(reactionExportPath, prefix);
				Loggy.Log(prefix, Category.Editor);
				foreach (var reaction in reactionsGroup)
				{
					var path = Path.Combine(
						prefixPath,
						ToPascalCase(reaction.Key.Replace("datum/chemical_reaction/", "")) +
						".asset");

					if (!Directory.Exists(Path.GetDirectoryName(path)))
					{
						Directory.CreateDirectory(Path.GetDirectoryName(path));
					}

					var localPath = LocalPath(path);
					if (!File.Exists(path) || overwrite)
					{
						AssetDatabase.CreateAsset(reaction.Value, localPath);
					}
					else
					{
						var existAsset = AssetDatabase.LoadAssetAtPath<Reaction>(localPath);
						if (existAsset)
						{
							foreach (var data in reactionSetsData)
							{
								var index = data.Value.IndexOf(reaction.Value);
								if (index > 0)
									data.Value[index] = existAsset;
							}
						}
					}


					progress++;
					if (cancel)
					{
						progress = 0;
						cancel = false;
						yield break;
					}

					yield return new EditorWaitForSeconds(0f);
				}
			}

			foreach (var reactionSetData in reactionSetsData)
			{
				var reactionSet = CreateInstance<ReactionSet>();
				reactionSet.reactions = reactionSetData.Value.ToArray();

				var prefix = ToPascalCase(
					Path.GetDirectoryName(reactionSetData.Key
						.Replace("obj/item/", "")));

				var prefixPath = Path.Combine(reactionSetExportPath, prefix);

				if (!Directory.Exists(prefixPath))
				{
					Directory.CreateDirectory(prefixPath);
				}

				var path = Path.Combine(
					prefixPath,
					ToPascalCase(Path.GetFileName(reactionSetData.Key)) +
					".asset");

				var localPath = LocalPath(path);

				if (!File.Exists(path) || overwrite)
					AssetDatabase.CreateAsset(reactionSet, localPath);
				else
				{
					var origSet = AssetDatabase.LoadAssetAtPath<ReactionSet>(localPath);
					var newSet = origSet.reactions.Union(reactionSet.reactions);

					origSet.reactions = newSet.ToArray();
					AssetDatabase.SaveAssets();
				}

				if (cancel)
				{
					progress = 0;
					cancel = false;
					yield break;
				}

				yield return new EditorWaitForSeconds(0f);
			}

			progress = 0;
		}

		private static Reaction ToReaction(
			KeyValuePair<string, Dictionary<string, object>> reactionData,
			Dictionary<string, Reagent> reagents,
			Dictionary<string, List<Reaction>> reactionSets)
		{
			var value = reactionData.Value;
			var reaction = CreateInstance<Reaction>();

			if (value.TryGetValue("results", out var resultsData))
			{
				reaction.results = new SerializableDictionary<Reagent, int>();
				var results = ((Dictionary<object, object>) resultsData).ToDictionary(
					r => {
						return reagents[(string)r.Key]; },
					r => int.Parse((string) r.Value));

				foreach (var result in results)
				{
					reaction.results.m_dict.Add(result.Key, result.Value);
				}
			}

			if (value.TryGetValue("required_reagents", out var ingredientsData))
			{
				reaction.ingredients = new SerializableDictionary<Reagent, int>();
				var ingredients = ((Dictionary<object, object>) ingredientsData).ToDictionary(
					r => reagents[(string) r.Key],
					r => int.Parse((string) r.Value));

				foreach (var ingredient in ingredients)
				{
					reaction.ingredients.m_dict.Add(ingredient.Key, ingredient.Value);
				}
			}

			if (value.TryGetValue("required_catalysts", out var catalystsData))
			{
				reaction.catalysts = new SerializableDictionary<Reagent, int>();
				var catalysts = ((Dictionary<object, object>) catalystsData).ToDictionary(
					r => reagents[(string) r.Key],
					r => int.Parse((string) r.Value));

				foreach (var catalyst in catalysts)
				{
					reaction.catalysts.m_dict.Add(catalyst.Key, catalyst.Value);
				}
			}

			if (value.TryGetValue("required_temp", out var temperatureData))
			{
				var temp = int.Parse((string) temperatureData);

				if (value.TryGetValue("is_cold_recipe", out var coldRecipe))
				{
					reaction.tempMax = temp;
				}
				else
				{
					reaction.tempMin = temp;
				}
			}

			value.TryGetValue("required_container", out var containerObj);
			var container = (string) containerObj ?? "Default";
			if (!reactionSets.ContainsKey(container))
			{
				reactionSets[container] = new List<Reaction>();
			}

			reactionSets[container].Add(reaction);

			return reaction;
		}

		private static Reagent ToReagent(KeyValuePair<string, Dictionary<string, object>> reagentData)
		{
			var value = reagentData.Value;

			var reagent = CreateInstance<Reagent>();

			if (value.TryGetValue("name", out var name))
			{
				reagent.Name = (string) name;
			}

			if (value.TryGetValue("description", out var description))
			{
				reagent.description = (string) description;
			}

			if (value.TryGetValue("color", out var colorString))
			{
				if (ColorUtility.TryParseHtmlString((string) colorString, out var color))
				{
					reagent.color = new Color(color.r, color.g, color.b, color.a);
				}
			}

			if (value.TryGetValue("reagent_state", out var state))
			{
				switch (state)
				{
					case "SOLID":
						reagent.state = ReagentState.Solid;
						break;
					case "LIQUID":
						reagent.state = ReagentState.Liquid;
						break;
					case "GAS":
						reagent.state = ReagentState.Gas;
						break;
				}
			}

			return reagent;
		}

		public string ToPascalCase(string original)
		{
			Regex invalidCharsRgx = new Regex("[^_a-zA-Z0-9]");
			Regex whiteSpace = new Regex(@"(?<=\s)");
			Regex startsWithLowerCaseChar = new Regex("^[a-z]");
			Regex firstCharFollowedByUpperCasesOnly = new Regex("(?<=[A-Z])[A-Z0-9]+$");
			Regex lowerCaseNextToNumber = new Regex("(?<=[0-9])[a-z]");
			Regex upperCaseInside = new Regex("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");

			// replace white spaces with undescore, then replace all invalid chars with empty string
			var pascalCase = invalidCharsRgx.Replace(whiteSpace.Replace(original, "_"), string.Empty)
				// split by underscores
				.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
				// set first letter to uppercase
				.Select(w => startsWithLowerCaseChar.Replace(w, m => m.Value.ToUpper()))
				// replace second and all following upper case letters to lower if there is no next lower (ABC -> Abc)
				.Select(w => firstCharFollowedByUpperCasesOnly.Replace(w, m => m.Value.ToLower()))
				// set upper case the first lower case following a number (Ab9cd -> Ab9Cd)
				.Select(w => lowerCaseNextToNumber.Replace(w, m => m.Value.ToUpper()))
				// lower second and next upper case letters except the last if it follows by any lower (ABcDEf -> AbcDef)
				.Select(w => upperCaseInside.Replace(w, m => m.Value.ToLower()));

			return string.Concat(pascalCase);
		}

		private class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
		{
			private readonly TKey key;
			private readonly IEnumerable<TElement> values;

			public Grouping(TKey key, IEnumerable<TElement> values)
			{
				this.key = key;
				this.values = values ?? throw new ArgumentNullException(nameof(values));
			}

			public TKey Key => key;

			public IEnumerator<TElement> GetEnumerator()
			{
				return values.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		private string LocalPath(string path)
		{
			if (path.StartsWith(Application.dataPath))
			{
				return "Assets" + path.Substring(Application.dataPath.Length);
			}

			return path;
		}
	}
}