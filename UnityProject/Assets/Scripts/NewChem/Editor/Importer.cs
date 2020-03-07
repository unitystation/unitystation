using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Chemistry.Editor
{
	public class Importer : EditorWindow
	{
		Deserializer deserializer = new DeserializerBuilder().Build();
		private string reagentExportPath;
		private string reactionExportPath;

		private string reagentsPath;
		private string reactionPath;


		[MenuItem("Window/Chemistry Importer")]
		static void ShowWindow() => GetWindow<Importer>("Chemistry Importer");

		void OnGUI()
		{
			ReagentsGui();
			ReactionsGui();

			reagentExportPath = EditorGUILayout.TextField("Reagent export path", reagentExportPath);
			reactionExportPath = EditorGUILayout.TextField("Reaction export path", reactionExportPath);

			EditorGUI.BeginDisabledGroup(
				!Directory.Exists(reagentsPath) ||
				!Directory.Exists(reactionPath) ||
				!Directory.Exists(reagentExportPath));

			if (GUILayout.Button("Save"))
			{
				Export();
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

		private void Export()
		{
			var reagentFiles = Directory.EnumerateFiles(reagentsPath);
			var reagentsText = File.ReadAllText(reagentsPath);
			var reagentsData = deserializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(reagentsText);

			var reactionText = File.ReadAllText(reactionPath);
			var reactionsData = deserializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(reactionText);

			var reagents = reagentFiles
				.Select(file => (file, data: deserializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(File.ReadAllText(file))))
				.Select(reagents => new Grouping<string, Dictionary<string, Reagent>>(reagents.file, 
					reagents.data
					.Where(dict => dict.Value.ContainsKey("name"))
					.ToDictionary(reagentData => reagentData.Key, ToReagent)));

			var flatReagents = reagents
				.SelectMany(dict => dict)
				.ToDictionary(pair => pair.Key, pair => pair.Value);

			var reactions = reactionsData
				.ToDictionary(r => r.Key, reactionData => ToReaction(reactionData, flatReagents));

			foreach (var reagent in reagents)
			{
				var path = Path.Combine(reagentExportPath + reagent., reagent.Value.Name + ".asset");
				AssetDatabase.CreateAsset(reagent.Value, path);
			}

			foreach (var reaction in reactions)
			{
				var path = Path.Combine(reactionExportPath, reaction.Key.Replace("datum/chemical_reaction/", "").Replace('/', '_') + ".asset");
				AssetDatabase.CreateAsset(reaction.Value, path);
			}
		}

		private static Reaction ToReaction(KeyValuePair<string, Dictionary<string, object>> reactionData, Dictionary<string, Reagent> reagents)
		{
			var value = reactionData.Value;
			var reaction = CreateInstance<Reaction>();

			if (value.TryGetValue("results", out var resultsData))
			{
				reaction.results = new ReagentMix();
				var results = ((Dictionary<object, object>)resultsData).ToDictionary(
					r => reagents[(string)r.Key],
					r => int.Parse((string)r.Value));

				foreach (var result in results)
				{
					reaction.results.Add(result);
				}
			}

			if (value.TryGetValue("required_reagents", out var ingredientsData))
			{
				reaction.ingredients = new ReagentMix();
				var ingredients = ((Dictionary<object, object>)ingredientsData).ToDictionary(
					r => reagents[(string)r.Key],
					r => int.Parse((string)r.Value));

				foreach (var ingredient in ingredients)
				{
					reaction.ingredients.Add(ingredient);
				}
			}

			if (value.TryGetValue("required_catalysts", out var catalystsData))
			{
				reaction.catalysts = new ReagentMix();
				var catalysts = ((Dictionary<object, object>)catalystsData).ToDictionary(
					r => reagents[(string)r.Key],
					r => int.Parse((string)r.Value));

				foreach (var catalyst in catalysts)
				{
					reaction.catalysts.Add(catalyst);
				}
			}

			if (value.TryGetValue("required_temp", out var temperatureData))
			{
				var temp = int.Parse((string)temperatureData);

				if (value.TryGetValue("is_cold_recipe", out var coldRecipe))
				{
					reaction.tempMax = temp;
				}
				else
				{
					reaction.tempMin = temp;
				}
			}

			return reaction;
		}

		private static Reagent ToReagent(KeyValuePair<string, Dictionary<string, object>> reagentData)
		{
			var value = reagentData.Value;

			var reagent = CreateInstance<Reagent>();

			if (value.TryGetValue("name", out var name))
			{
				reagent.Name = (string)name;
			}

			if (value.TryGetValue("description", out var description))
			{
				reagent.description = (string)description;
			}

			if (value.TryGetValue("color", out var colorString))
			{
				if (ColorUtility.TryParseHtmlString((string)colorString, out var color))
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
	}
}