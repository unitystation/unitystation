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
		private string reagentsPath;
		public string ReagentsPath
		{
			get { return reagentsPath; }
			set
			{
				if (value == reagentsPath) return;
				reagentsPath = value;

				if (File.Exists(ReagentsPath))
				{
					var text = File.ReadAllText(ReagentsPath);
					reagents = deserializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(text);
				}
				else
				{
					reagents = null;
				}
			}
		}

		Dictionary<string, Dictionary<string, object>> reagents;
		Vector2 scrollPos;


		[MenuItem("Window/Chemistry Importer")] static void ShowWindow() => GetWindow<Importer>("Chemistry Importer");

		void OnGUI()
		{
			ReagentsPath = EditorGUILayout.TextField("Reagents path", ReagentsPath);

			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			foreach (var reagent in reagents ?? new Dictionary<string, Dictionary<string, object>>())
			{
				EditorGUILayout.LabelField(reagent.Key);
			}
			EditorGUILayout.EndScrollView();
		}
	}

}
