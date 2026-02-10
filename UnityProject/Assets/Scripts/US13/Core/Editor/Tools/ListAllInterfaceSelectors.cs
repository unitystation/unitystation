using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using US13.Core.Attributes;

namespace US13.Core.Editor.Tools
{
	public class ListAllInterfaceSelectors : EditorWindow
	{
		private Vector2 _scroll;
		private List<Result> _results = new List<Result>();
		private bool _scanned = false;

		[MenuItem("Tools/US13/List Interface Selectors")]
		public static void ShowWindow()
		{
			GetWindow<ListAllInterfaceSelectors>("Interface Selectors");
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Scan for fields/properties using SelectImplementationAttribute", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Scan All Prefabs & ScriptableObjects"))
			{
				ScanAll();
			}

			if (GUILayout.Button("Clear Results"))
			{
				_results.Clear();
				_scanned = false;
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			EditorGUILayout.LabelField($"Results: {_results.Count}");

			_scroll = EditorGUILayout.BeginScrollView(_scroll);
			foreach (var r in _results)
			{
				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.LabelField(r.assetPath, EditorStyles.miniLabel);
				EditorGUILayout.LabelField($"Asset Type: {r.assetType}  	Owner: {r.ownerType}");
				EditorGUILayout.LabelField($"Member: {r.memberName}  	Member Type: {r.memberType}");
				EditorGUILayout.LabelField($"SelectImplementation FieldType: {r.attributeFieldType}");

				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Ping Asset"))
				{
					EditorGUIUtility.PingObject(r.assetRef);
					Selection.activeObject = r.assetRef;
				}
				if (GUILayout.Button("Select Asset"))
				{
					Selection.activeObject = r.assetRef;
				}
				if (GUILayout.Button("Copy Info"))
				{
					var s = r.ToString();
					EditorGUIUtility.systemCopyBuffer = s;
					Debug.Log("Copied: " + s);
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndScrollView();
		}

		[Serializable]
		private class Result
		{
			public string assetPath;
			public string assetType; // Prefab or ScriptableObject
			public string ownerType;
			public string memberName;
			public string memberType;
			public string attributeFieldType;
			public UnityEngine.Object assetRef;

			public override string ToString()
			{
				return $"{assetPath} | {assetType} | {ownerType} | {memberName} : {memberType} | AttrFieldType={attributeFieldType}";
			}
		}

		/// <summary>
		/// Scans all prefabs and ScriptableObjects in the project and populates _results.
		/// </summary>
		private void ScanAll()
		{
			_results.Clear();

			// We'll search for prefabs and scriptableobject assets separately.
			var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
			var soGuids = AssetDatabase.FindAssets("t:ScriptableObject");

			// Process prefabs
			foreach (var g in prefabGuids)
			{
				var path = AssetDatabase.GUIDToAssetPath(g);
				var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (go == null) continue;

				var components = go.GetComponents<Component>();
				foreach (var comp in components)
				{
					if (comp == null) continue; // missing script
					AnalyzeObjectMembers(path, "Prefab", comp.GetType(), comp, go);
				}
			}

			// Process ScriptableObjects
			foreach (var g in soGuids)
			{
				var path = AssetDatabase.GUIDToAssetPath(g);
				var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
				if (obj == null) continue;

				AnalyzeObjectMembers(path, "ScriptableObject", obj.GetType(), obj, obj);
			}

			_scanned = true;
			Debug.Log($"SelectImplementation scan complete. {_results.Count} matches found.");
		}

		private void AnalyzeObjectMembers(string assetPath, string assetType, Type ownerType, object ownerInstance, UnityEngine.Object assetRef)
		{
			// Check fields
			var fields = ownerType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var f in fields)
			{
				var attr = f.GetCustomAttribute<SelectImplementationAttribute>(true);
				if (attr != null)
				{
					_results.Add(new Result
					{
						assetPath = assetPath,
						assetType = assetType,
						ownerType = ownerType.FullName,
						memberName = f.Name,
						memberType = f.FieldType.FullName,
						attributeFieldType = attr.FieldType != null ? attr.FieldType.FullName : "<null>",
						assetRef = assetRef
					});
				}
			}

			// Check properties
			var props = ownerType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var p in props)
			{
				var attr = p.GetCustomAttribute<SelectImplementationAttribute>(true);
				if (attr != null)
				{
					_results.Add(new Result
					{
						assetPath = assetPath,
						assetType = assetType,
						ownerType = ownerType.FullName,
						memberName = p.Name,
						memberType = p.PropertyType != null ? p.PropertyType.FullName : "<null>",
						attributeFieldType = attr.FieldType != null ? attr.FieldType.FullName : "<null>",
						assetRef = assetRef
					});
				}
			}
		}
	}
}