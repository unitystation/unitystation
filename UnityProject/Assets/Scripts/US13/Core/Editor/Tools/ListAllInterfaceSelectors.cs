using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using US13.Core.Attributes;

namespace US13.Core.Editor.Tools
{
	/// <summary>
	/// A tool to scan all prefabs and ScriptableObjects in the project for fields/properties using SelectImplementationAttribute,
	/// which is commonly used for interface-typed fields that require manual namespace updates.
	/// (Max): This tool is heavily vibe-coded in a lot of areas, and can honestly be cleaned up to be much faster and more user-friendly.
	/// </summary>
	public class ListAllInterfaceSelectors : EditorWindow
	{
		private Vector2 _scroll;
		private List<Result> _results = new List<Result>();
		private bool _scanned = false;

		// Grouping/filter UI state
		private string _filterText = string.Empty;
		private Dictionary<string, bool> _groupVisible = new Dictionary<string, bool>();
		private Dictionary<string, bool> _groupFoldout = new Dictionary<string, bool>();

		[MenuItem("Tools/Windows/List All Interface Selectors")]
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
				_groupVisible.Clear();
				_groupFoldout.Clear();
			}

			if (GUILayout.Button("Log Results to Console"))
			{
				if (_results.Count == 0) Debug.Log("No results to log. Run Scan first.");
				else
				{
					foreach (var r in _results) Debug.Log(r.ToString());
					Debug.Log($"Logged {_results.Count} results.");
				}
			}

			// Batch fix button: runs AutoFixNamespaces on all found asset paths
			if (GUILayout.Button("Auto-Fix Namespaces (All Results)"))
			{
				if (_results.Count == 0)
				{
					Debug.LogWarning("No results to auto-fix. Run Scan first.");
				}
				else
				{
					var unique = new HashSet<string>(_results.Select(r => r.assetPath));
					int i = 0;
					foreach (var path in unique)
					{
						i++;
						EditorUtility.DisplayProgressBar("Auto-Fix Namespaces", $"Processing {path} ({i}/{unique.Count})", (float)i / unique.Count);
						RawAssetEditor.AutoFixNamespaces(path);
					}
					EditorUtility.ClearProgressBar();
					Debug.Log($"Auto-fixed namespaces for {unique.Count} assets.");
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			// Filter and group controls
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
			_filterText = EditorGUILayout.TextField(_filterText);
			if (GUILayout.Button("Clear", GUILayout.Width(50))) _filterText = string.Empty;
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			// Build groups
			var groups = _results.GroupBy(r => string.IsNullOrEmpty(r.attributeFieldType) ? "<null>" : r.attributeFieldType)
				.OrderBy(g => g.Key).ToList();

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Show All Groups"))
			{
				foreach (var g in groups) _groupVisible[g.Key] = true;
			}
			if (GUILayout.Button("Hide All Groups"))
			{
				foreach (var g in groups) _groupVisible[g.Key] = false;
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			EditorGUILayout.LabelField($"Results: {_results.Count}   Groups: {groups.Count}", EditorStyles.miniLabel);

			_scroll = EditorGUILayout.BeginScrollView(_scroll);

			// Render groups
			foreach (var g in groups)
			{
				var key = g.Key;
				if (!_groupVisible.ContainsKey(key)) _groupVisible[key] = true;
				if (!_groupFoldout.ContainsKey(key)) _groupFoldout[key] = true;

				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.BeginHorizontal();
				_groupVisible[key] = EditorGUILayout.ToggleLeft($"{key} ({g.Count()})", _groupVisible[key], GUILayout.Width(400));
				_groupFoldout[key] = EditorGUILayout.Foldout(_groupFoldout[key], "Details");
				EditorGUILayout.EndHorizontal();

				if (_groupVisible[key] && _groupFoldout[key])
				{
					// List entries in group filtered by search
					foreach (var r in g)
					{
						if (!String.IsNullOrEmpty(_filterText))
						{
							var f = _filterText.ToLowerInvariant();
							if (!(r.assetPath != null && r.assetPath.ToLowerInvariant().Contains(f) ||
								(r.ownerType != null && r.ownerType.ToLowerInvariant().Contains(f)) ||
								(r.memberName != null && r.memberName.ToLowerInvariant().Contains(f)) ||
								(r.memberType != null && r.memberType.ToLowerInvariant().Contains(f))))
								continue;
						}

						EditorGUILayout.BeginVertical("box");
						EditorGUILayout.LabelField(r.assetPath, EditorStyles.miniLabel);
						EditorGUILayout.LabelField($"Asset Type: {r.assetType}  \tOwner: {r.ownerType}");
						EditorGUILayout.LabelField($"Member: {r.memberName}  \tMember Type: {r.memberType}");
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
						if (GUILayout.Button("Open Raw"))
						{
							RawAssetEditor.ShowEditor(r.assetPath);
						}
						if (GUILayout.Button("Auto-Fix Namespaces"))
						{
							RawAssetEditor.AutoFixNamespaces(r.assetPath);
						}
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.EndVertical();
					}
				}

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
			AnalyzeObjectMembers(assetPath, assetType, ownerType, ownerInstance, assetRef, "", new HashSet<Type>());
		}

		private void AnalyzeObjectMembers(string assetPath, string assetType, Type ownerType, object ownerInstance, UnityEngine.Object assetRef, string memberPathPrefix, HashSet<Type> visited)
		{
			if (ownerType == null) return;
			if (visited.Contains(ownerType)) return;
			visited.Add(ownerType);

			// Check fields
			var fields = ownerType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var f in fields)
			{
				var attr = f.GetCustomAttribute<SelectImplementationAttribute>(true);
				var memberFullName = string.IsNullOrEmpty(memberPathPrefix) ? f.Name : memberPathPrefix + "." + f.Name;
				if (attr != null)
				{
					_results.Add(new Result
					{
						assetPath = assetPath,
						assetType = assetType,
						ownerType = ownerType.FullName,
						memberName = memberFullName,
						memberType = f.FieldType != null ? f.FieldType.FullName : "<null>",
						attributeFieldType = attr.FieldType != null ? attr.FieldType.FullName : "<null>",
						assetRef = assetRef
					});
				}

				// Recurse into the field's type if it's a serializable class/struct (to find attributes inside nested types)
				Type fieldType = f.FieldType;
				// If it's a collection, get the element type
				if (fieldType.IsArray) fieldType = fieldType.GetElementType();
				else if (fieldType.IsGenericType)
				{
					var genDef = fieldType.GetGenericTypeDefinition();
					if (genDef == typeof(List<>)) fieldType = fieldType.GetGenericArguments()[0];
				}

				// Skip primitives, enums, strings and UnityEngine.Object types
				if (fieldType != null && !fieldType.IsPrimitive && fieldType != typeof(string) && !fieldType.IsEnum && !typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
				{
					// For instance value, try to get the actual nested instance to allow deeper traversal with real values (helps for arrays/lists)
					object nestedInstance = null;
					if (ownerInstance != null)
					{
						try { nestedInstance = f.GetValue(ownerInstance); } catch { nestedInstance = null; }
					}

					AnalyzeObjectMembers(assetPath, assetType, fieldType, nestedInstance, assetRef, memberFullName, visited);
				}
			}

			// Check properties
			var props = ownerType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var p in props)
			{
				var attr = p.GetCustomAttribute<SelectImplementationAttribute>(true);
				var memberFullName = string.IsNullOrEmpty(memberPathPrefix) ? p.Name : memberPathPrefix + "." + p.Name;
				if (attr != null)
				{
					_results.Add(new Result
					{
						assetPath = assetPath,
						assetType = assetType,
						ownerType = ownerType.FullName,
						memberName = memberFullName,
						memberType = p.PropertyType != null ? p.PropertyType.FullName : "<null>",
						attributeFieldType = attr.FieldType != null ? attr.FieldType.FullName : "<null>",
						assetRef = assetRef
					});
				}

				// Recurse into property type similar to fields (if getter accessible and serializable)
				Type propType = p.PropertyType;
				if (propType.IsArray) propType = propType.GetElementType();
				else if (propType.IsGenericType)
				{
					var genDef = propType.GetGenericTypeDefinition();
					if (genDef == typeof(List<>)) propType = propType.GetGenericArguments()[0];
				}

				if (propType != null && !propType.IsPrimitive && propType != typeof(string) && !propType.IsEnum && !typeof(UnityEngine.Object).IsAssignableFrom(propType))
				{
					object nestedInstance = null;
					if (ownerInstance != null)
					{
						try { nestedInstance = p.GetValue(ownerInstance); } catch { nestedInstance = null; }
					}
					AnalyzeObjectMembers(assetPath, assetType, propType, nestedInstance, assetRef, memberFullName, visited);
				}
			}
		}

		/// <summary>
		/// Small popup editor to view and edit the raw serialized asset file (text-based prefabs and .asset files).
		/// Provides backup, manual edit, simple namespace auto-fix and rid replacement utilities.
		/// </summary>
		public class RawAssetEditor : EditorWindow
		{
			private string _assetPath;
			private string _fullPath;
			private string _text;
			private Vector2 _scroll;
			private string _oldRid = "";
			private string _newRid = "";
			private bool _isTextAsset = true;

			public static void ShowEditor(string assetPath)
			{
				var w = GetWindow<RawAssetEditor>("Raw Asset Editor");
				w._assetPath = assetPath;
				w.LoadAssetText();
				w.Show();
			}

			private void LoadAssetText()
			{
				_text = string.Empty;
				_fullPath = GetFullPath(_assetPath);
				if (!File.Exists(_fullPath))
				{
					_text = $"File not found: {_fullPath}";
					_isTextAsset = false;
					return;
				}

				try
				{
					// Read all text. If it's binary or very large, user will see it and should cancel.
					_text = File.ReadAllText(_fullPath);
					_isTextAsset = true;
				}
				catch (Exception ex)
				{
					_text = "Failed to read file: " + ex.Message;
					_isTextAsset = false;
				}
			}

			private static string GetFullPath(string assetPath)
			{
				if (assetPath.StartsWith("Assets/"))
				{
					return Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar));
				}
				// fallback - try to resolve from project root
				return Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
			}

			private void OnGUI()
			{
				EditorGUILayout.LabelField(_assetPath, EditorStyles.boldLabel);
				EditorGUILayout.Space();

				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Backup File (.bak)"))
				{
					BackupFile();
				}
				if (GUILayout.Button("Revert from Backup"))
				{
					RevertFromBackup();
					LoadAssetText();
				}
				if (GUILayout.Button("Refresh"))
				{
					LoadAssetText();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();

				EditorGUILayout.LabelField("Quick fixes:", EditorStyles.boldLabel);
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Auto-Fix Namespaces (Objects. -> US13.Objects.)"))
				{
					AutoFixNamespaces(_assetPath);
					LoadAssetText();
				}
				if (GUILayout.Button("Auto-Fix DeconstructionMethods"))
				{
					AutoFixDeconstructionMethods(_assetPath);
					LoadAssetText();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("RID Replace (exact text replace)");
				EditorGUILayout.BeginHorizontal();
				_oldRid = EditorGUILayout.TextField("Old RID", _oldRid);
				if (GUILayout.Button("Generate New RID"))
				{
					_newRid = GenerateRid().ToString();
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				_newRid = EditorGUILayout.TextField("New RID", _newRid);
				if (GUILayout.Button("Replace RID"))
				{
					if (string.IsNullOrEmpty(_oldRid) || string.IsNullOrEmpty(_newRid))
					{
						Debug.LogWarning("Both old and new RID must be set.");
					}
					else
					{
						_text = _text.Replace(_oldRid, _newRid);
						SaveAndReimport();
						LoadAssetText();
					}
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();

				_scroll = EditorGUILayout.BeginScrollView(_scroll);
				if (_isTextAsset)
				{
					_text = EditorGUILayout.TextArea(_text, GUILayout.ExpandHeight(true));
				}
				else
				{
					EditorGUILayout.LabelField(_text);
				}
				EditorGUILayout.EndScrollView();

				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Save"))
				{
					SaveAndReimport();
					LoadAssetText();
				}
				if (GUILayout.Button("Close"))
				{
					Close();
				}
				EditorGUILayout.EndHorizontal();
			}

			private void BackupFile()
			{
				try
				{
					var bak = _fullPath + ".bak";
					File.Copy(_fullPath, bak, true);
					Debug.Log("Backup created: " + bak);
				}
				catch (Exception ex)
				{
					Debug.LogError("Backup failed: " + ex);
				}
			}

			private void RevertFromBackup()
			{
				try
				{
					var bak = _fullPath + ".bak";
					if (!File.Exists(bak))
					{
						Debug.LogWarning("No backup found: " + bak);
						return;
					}
					File.Copy(bak, _fullPath, true);
					AssetDatabase.ImportAsset(_assetPath, ImportAssetOptions.ForceUpdate);
					Debug.Log("Reverted from backup: " + bak);
				}
				catch (Exception ex)
				{
					Debug.LogError("Revert failed: " + ex);
				}
			}

			private static long GenerateRid()
			{
				// Generate a 64-bit positive number using ticks + random to reduce collisions.
				var rnd = new System.Random();
				var buffer = new byte[8];
				rnd.NextBytes(buffer);
				long val = BitConverter.ToInt64(buffer, 0);
				if (val < 0) val = -val;
				// ensure non-zero
				if (val == 0) val = DateTime.UtcNow.Ticks & 0x7FFFFFFFFFFFFFFF;
				return val;
			}

			private void SaveAndReimport()
			{
				if (!_isTextAsset)
				{
					Debug.LogError("Asset not a text-serializable file; won't save.");
					return;
				}
				try
				{
					File.WriteAllText(_fullPath, _text);
					AssetDatabase.ImportAsset(_assetPath, ImportAssetOptions.ForceUpdate);
					Debug.Log("Saved and reimported: " + _assetPath);
				}
				catch (Exception ex)
				{
					Debug.LogError("Failed to save file: " + ex);
				}
			}

			private static string ResolveNamespaceForClass(string className, string currentNs)
			{
				try
				{
					var assemblies = AppDomain.CurrentDomain.GetAssemblies();
					var candidates = new List<Type>();
					foreach (var a in assemblies)
					{
						Type[] types;
						try { types = a.GetTypes(); } catch { continue; }
						foreach (var t in types)
						{
							if (t.Name == className && !string.IsNullOrEmpty(t.Namespace))
								candidates.Add(t);
						}
					}

					if (candidates.Count == 0)
					{
						// If no exact class match in loaded assemblies, attempt to guess by prefixing US13 to the existing namespace
						if (!string.IsNullOrEmpty(currentNs) && !currentNs.StartsWith("US13."))
							return "US13." + currentNs.Trim();
						return currentNs;
					}

					// Prefer a candidate in US13 namespace
					var preferred = candidates.FirstOrDefault(t => !string.IsNullOrEmpty(t.Namespace) && t.Namespace.StartsWith("US13."));
					if (preferred != null) return preferred.Namespace;
					// Otherwise pick the first candidate's namespace
					return candidates[0].Namespace;
				}
				catch (Exception ex)
				{
					Debug.LogWarning($"ResolveNamespaceForClass failed for {className}: {ex.Message}");
					return currentNs;
				}
			}

			private static string FixTypeBodyNamespaces(string typeBody)
			{
				// typeBody example: "class: FalseWallDeconstruction, ns: Objects.Doors.DoorDeconstruction.DeconstructionMethods,"
				try
				{
					var classMatch = Regex.Match(typeBody, @"class:\s*([^,}]+)");
					var nsMatch = Regex.Match(typeBody, @"ns:\s*([^,}]+)");
					if (!classMatch.Success) return typeBody;
					var className = classMatch.Groups[1].Value.Trim();
					var currentNs = nsMatch.Success ? nsMatch.Groups[1].Value.Trim() : string.Empty;

					var resolvedNs = ResolveNamespaceForClass(className, currentNs);
					if (string.IsNullOrEmpty(resolvedNs)) return typeBody;

					if (nsMatch.Success)
					{
						// replace the ns value preserving spacing
						var newBody = Regex.Replace(typeBody, @"ns:\s*[^,}]+", "ns: " + resolvedNs);
						return newBody;
					}
					else
					{
						// no ns present; append it
						return typeBody.TrimEnd() + ", ns: " + resolvedNs + ",";
					}
				}
				catch (Exception ex)
				{
					Debug.LogWarning("FixTypeBodyNamespaces failed: " + ex.Message);
					return typeBody;
				}
			}

			public static void AutoFixNamespaces(string assetPath)
			{
				var full = GetFullPath(assetPath);
				if (!File.Exists(full))
				{
					Debug.LogError("File not found: " + full);
					return;
				}
				string text;
				try
				{
					text = File.ReadAllText(full);
				}
				catch (Exception ex)
				{
					Debug.LogError("Failed to read file: " + ex);
					return;
				}

				// Parse all type: {...} blocks and fix their ns by resolving the implementation type
				var typeBlockRegex = new Regex(@"type:\s*\{([^}]*)\}", RegexOptions.Multiline);
				var replaced = typeBlockRegex.Replace(text, match =>
				{
					var inner = match.Groups[1].Value;
					var fixedInner = FixTypeBodyNamespaces(inner);
					return "type: {" + fixedInner + "}";
				});

				if (replaced == text)
				{
					Debug.Log("No namespace changes required in " + assetPath);
					return;
				}

				// Backup
				var bak = full + ".bak";
				try
				{
					File.Copy(full, bak, true);
					File.WriteAllText(full, replaced);
					AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
					Debug.Log("Auto-fixed namespaces and saved: " + assetPath + " (backup: " + bak + ")");
				}
				catch (Exception ex)
				{
					Debug.LogError("Failed to auto-fix namespaces: " + ex);
				}
			}

			public static void AutoFixDeconstructionMethods(string assetPath)
			{
				var full = GetFullPath(assetPath);
				if (!File.Exists(full))
				{
					Debug.LogError("File not found: " + full);
					return;
				}
				string text;
				try
				{
					text = File.ReadAllText(full);
				}
				catch (Exception ex)
				{
					Debug.LogError("Failed to read file: " + ex);
					return;
				}

				var deconPattern = new Regex(@"DeconstructionMethods:\s*\r?\n\s*-\s*rid:\s*(\d+)", RegexOptions.Multiline);
				var matches = deconPattern.Matches(text);
				if (matches.Count == 0)
				{
					Debug.Log("No DeconstructionMethods sections found in " + assetPath);
					return;
				}

				bool changed = false;
				// iterate from last to first so replacements don't shift earlier indexes
				for (int i = matches.Count - 1; i >= 0; i--)
				{
					var m = matches[i];
					var oldRid = m.Groups[1].Value;
					var newRid = GenerateRid().ToString();

					// Replace the top-level rid for this DeconstructionMethods entry
					int replaceIndex = m.Groups[1].Index;
					text = text.Substring(0, replaceIndex) + newRid + text.Substring(replaceIndex + oldRid.Length);

					// Find RefIds: block after this DeconstructionMethods occurrence
					int searchStart = m.Index;
					int refIdsPos = text.IndexOf("RefIds:", searchStart, StringComparison.Ordinal);
					if (refIdsPos == -1)
					{
						Debug.LogWarning($"No RefIds found for DeconstructionMethods (rid={oldRid}) in {assetPath}");
						changed = true; // top-level rid changed regardless
						continue;
					}

					// Find end of RefIds block (next top-level key or EOF)
					int refBlockEnd = text.Length;
					var boundaryMatch = Regex.Match(text.Substring(refIdsPos), @"\r?\n(?=[^\s-].+?:)");
					if (boundaryMatch.Success)
					{
						refBlockEnd = refIdsPos + boundaryMatch.Index;
					}

					string refBlock = text.Substring(refIdsPos, refBlockEnd - refIdsPos);

					// Find last existing RefIds entry (to copy class/ns and indentation)
					var entryRegex = new Regex(@"-\s*rid:\s*(\d+)\s*\r?\n([ \t]*)type:\s*\{([^}]*)\}", RegexOptions.Multiline);
					var entryMatches = entryRegex.Matches(refBlock);
					string indent = "  ";
					string typeBody = "class: Unknown, ns: US13.Unknown,";
					if (entryMatches.Count > 0)
					{
						var last = entryMatches[entryMatches.Count - 1];
						indent = last.Groups[2].Value; // indentation before 'type:' line
						typeBody = last.Groups[3].Value.Trim();
						// use context-aware namespace fixer
						typeBody = FixTypeBodyNamespaces(typeBody);
					}

					// If a RefIds entry with newRid already exists, skip adding
					if (refBlock.Contains(newRid))
					{
						Debug.Log($"RefIds already contains new RID {newRid} in {assetPath}");
						changed = true;
						continue;
					}

					// Build new entry text
					var newEntry = "\n" + indent + "- rid: " + newRid + "\n" + indent + "type: {" + typeBody + "}";

					// Insert new entry before refBlockEnd
					text = text.Substring(0, refBlockEnd) + newEntry + text.Substring(refBlockEnd);
					changed = true;
				}

				if (changed)
				{
					try
					{
						var bak = full + ".bak";
						File.Copy(full, bak, true);
						File.WriteAllText(full, text);
						AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
						Debug.Log("Auto-fixed DeconstructionMethods and saved: " + assetPath + " (backup: " + bak + ")");
					}
					catch (Exception ex)
					{
						Debug.LogError("Failed to auto-fix deconstruction methods: " + ex);
					}
				}
				else
				{
					Debug.Log("No changes made to DeconstructionMethods in " + assetPath);
				}
			}
		}
	}
}
