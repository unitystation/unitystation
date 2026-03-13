#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace US13.Core.Editor.Tools.Debugging.UnityEvents
{
	public class UnityEventScanner : EditorWindow
	{
		private readonly List<EventEntry> _results = new();
		private readonly List<EventEntry> _filtered = new();
		private bool _isScanning;
		private Vector2 _scroll;

		private string _searchText = "";
		private string _searchTextLower = ""; // avoid ToLower() every frame
		private bool _showOnlyWithListeners = true;
		private bool _showOnlyBroken        = false;

		private int _scannedPrefabCount;
		private int _totalEventCount;

		internal static FieldInfo s_PersistentCallsField;
		internal static FieldInfo s_CallsField;
		internal static FieldInfo s_TargetField;
		internal static FieldInfo s_MethodNameField;
		internal static FieldInfo s_ModeField;
		internal static FieldInfo s_ArgumentsField;
		internal static FieldInfo s_ArgUnityObjectField;
		// Assembly-qualified type name strings — these go stale on namespace renames
		internal static FieldInfo s_TargetAssemblyTypeNameField;      // PersistentCall  → m_TargetAssemblyTypeName
		internal static FieldInfo s_ArgAssemblyTypeNameField;         // ArgumentCache   → m_ObjectArgumentAssemblyTypeName
		private static bool      s_CallReflectionReady;
		private static readonly Dictionary<(Type, string), bool> s_MethodExistsCache = new();

		private const float ColPrefab = 220f;
		private const float ColGO = 160f;
		private const float ColComponent = 150f;
		private const float ColField = 150f;
		private const float ColListeners = 70f;
		private const float RowHeight = 18f;
		private const int TruncateLen = 36;

		private float _scrollY;
		private float _viewportHeight; // persisted from last valid Repaint pass
		private int _hoveredIndex = -1; // repaint only when hover changes
		private int _rightClickIndex = -1;

		private GUIStyle _headerStyle;
		private GUIStyle _rowLabelStyle; // one shared instance — never reallocated per row
		private GUIStyle _hasListenersStyle;
		private GUIStyle _noListenersStyle;
		private bool _stylesInitialised;

		// Pre-baked colours
		private static readonly Color RowColorEven = new(0.22f, 0.22f, 0.22f);
		private static readonly Color RowColorOdd = new(0.19f, 0.19f, 0.19f);
		private static readonly Color RowColorHover = new(0.28f, 0.45f, 0.70f, 0.30f);
		private static readonly Color HeaderColor = new(0.12f, 0.12f, 0.12f);
		private static readonly Color StatsColor = new(0.15f, 0.15f, 0.15f);
		private static readonly Color SepColor = new(0.30f, 0.30f, 0.30f);
		private static readonly Color HeaderSepColor = new(0.35f, 0.35f, 0.35f);


		[MenuItem("Tools/UnityEvent Scanner")]
		public static void ShowWindow()
		{
			var win = GetWindow<UnityEventScanner>("UnityEvent Scanner");
			win.minSize = new Vector2(820, 500);
		}

		private void OnGUI()
		{
			InitStyles();
			DrawToolbar();
			DrawFilters();
			DrawStats();
			DrawResultsTable();
		}


		private void InitStyles()
		{
			if (_stylesInitialised) return;
			_stylesInitialised = true;

			_headerStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 11,
				alignment = TextAnchor.MiddleLeft,
				padding = new RectOffset(4, 4, 2, 2)
			};

			// Shared across every cell — allocate once
			_rowLabelStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				padding = new RectOffset(4, 4, 1, 1)
			};

			_hasListenersStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				normal = { textColor = new Color(0.4f, 0.9f, 0.5f) },
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter,
				padding = new RectOffset(4, 4, 1, 1)
			};

			_noListenersStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				normal = { textColor = new Color(0.55f, 0.55f, 0.55f) },
				alignment = TextAnchor.MiddleCenter,
				padding = new RectOffset(4, 4, 1, 1)
			};
		}

		private void DrawToolbar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			GUILayout.Label("UnityEvent Scanner", EditorStyles.boldLabel, GUILayout.Width(160));
			GUILayout.FlexibleSpace();

			if (_isScanning)
			{
				GUILayout.Label("Scanning…", EditorStyles.miniLabel);
			}
			else
			{
				if (GUILayout.Button("▶  Scan All Prefabs", EditorStyles.toolbarButton, GUILayout.Width(140)))
					ScanAllPrefabs();

				if (_results.Count > 0 &&
				    GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(90)))
					ExportCsv();
			}

			EditorGUILayout.EndHorizontal();
		}

		private void DrawFilters()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			GUILayout.Label("Search:", GUILayout.Width(50));

			EditorGUI.BeginChangeCheck();
			_searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.Width(220));
			if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
				_searchText = "";

			GUILayout.Space(12);
			_showOnlyWithListeners = GUILayout.Toggle(
				_showOnlyWithListeners, "  Has listeners only",
				EditorStyles.toolbarButton, GUILayout.Width(140));

			GUILayout.Space(4);
			_showOnlyBroken = GUILayout.Toggle(
				_showOnlyBroken, "  ⚠ Broken / missing only",
				EditorStyles.toolbarButton, GUILayout.Width(165));

			if (EditorGUI.EndChangeCheck())
			{
				_searchTextLower = _searchText.ToLowerInvariant();
				ApplyFilters();
				_hoveredIndex = -1;
				Repaint();
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		private void DrawStats()
		{
			if (_results.Count == 0 && !_isScanning) return;

			var brokenCount = 0;
			foreach (var e in _filtered) if (e.IsBroken) brokenCount++;

			var rect = EditorGUILayout.BeginHorizontal();
			EditorGUI.DrawRect(rect, StatsColor);
			GUILayout.Space(6);
			GUILayout.Label($"Prefabs scanned: {_scannedPrefabCount}", EditorStyles.miniLabel);
			GUILayout.Space(16);
			GUILayout.Label($"UnityEvents found: {_totalEventCount}", EditorStyles.miniLabel);
			GUILayout.Space(16);
			GUILayout.Label($"Showing: {_filtered.Count}", EditorStyles.miniLabel);

			if (brokenCount > 0)
			{
				GUILayout.Space(16);
				var prev = GUI.contentColor;
				GUI.contentColor = new Color(1f, 0.4f, 0.4f);
				GUILayout.Label($"⚠ Broken: {brokenCount}", EditorStyles.miniLabel);
				GUI.contentColor = prev;
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(2);
		}

		private void DrawResultsTable()
		{
			if (_results.Count == 0)
			{
				GUILayout.FlexibleSpace();
				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label(
					_isScanning ? "Scanning…" : "Press 'Scan All Prefabs' to begin.",
					EditorStyles.centeredGreyMiniLabel);
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
				GUILayout.FlexibleSpace();
				return;
			}

			DrawTableHeader();

			// Grab all remaining space for the virtual scroll viewport
			var containerRect = GUILayoutUtility.GetRect(
				0, float.MaxValue, 0, float.MaxValue,
				GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

			var totalRows = _filtered.Count;
			var totalHeight = totalRows * RowHeight;

			// GUILayoutUtility.GetRect returns height=0 during the Layout pass.
			// Only update the stored viewport height when we have a real measurement,
			// so the scroll position is never clamped against a zero-sized rect.
			if (containerRect.height > 1f)
				_viewportHeight = containerRect.height;

			var viewportHeight = _viewportHeight;
			var maxScroll = Mathf.Max(0f, totalHeight - viewportHeight);

			var needsScrollbar = totalHeight > viewportHeight;
			var scrollbarWidth = needsScrollbar ? 13f : 0f;

			var viewRect = new Rect(
				containerRect.x, containerRect.y,
				containerRect.width - scrollbarWidth, viewportHeight);

			// Scrollbar — only update _scrollY from the control during a real Repaint,
			// never during Layout (where all heights are 0).
			if (needsScrollbar && Event.current.type == EventType.Repaint)
				_scrollY = GUI.VerticalScrollbar(
					new Rect(containerRect.xMax - scrollbarWidth, containerRect.y,
						scrollbarWidth, viewportHeight),
					_scrollY, viewportHeight, 0f, totalHeight);
			else if (needsScrollbar)
				// Still register the control during Layout so GUI control IDs stay stable
				GUI.VerticalScrollbar(
					new Rect(containerRect.xMax - scrollbarWidth, containerRect.y,
						scrollbarWidth, viewportHeight),
					_scrollY, viewportHeight, 0f, totalHeight);

			// Mouse-wheel scroll
			if (Event.current.type == EventType.ScrollWheel &&
			    containerRect.Contains(Event.current.mousePosition))
			{
				_scrollY = Mathf.Clamp(
					_scrollY + Event.current.delta.y * RowHeight * 3f,
					0f, maxScroll);
				Event.current.Use();
				Repaint();
			}

			// Only draw rows that are actually visible
			var firstVisible = Mathf.Max(0, Mathf.FloorToInt(_scrollY / RowHeight));
			var lastVisible = Mathf.Min(totalRows - 1,
				Mathf.CeilToInt((_scrollY + _viewportHeight) / RowHeight));

			GUI.BeginClip(viewRect);

			var newHovered = -1;

			for (var i = firstVisible; i <= lastVisible; i++)
			{
				var y = i * RowHeight - _scrollY;
				var rect = new Rect(0f, y, viewRect.width, RowHeight);

				EditorGUI.DrawRect(rect, i % 2 == 0 ? RowColorEven : RowColorOdd);

				if (rect.Contains(Event.current.mousePosition))
				{
					newHovered = i;
					EditorGUI.DrawRect(rect, RowColorHover);

					if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
					{
						var obj = AssetDatabase.LoadAssetAtPath<GameObject>(_filtered[i].PrefabPath);
						if (obj != null) EditorGUIUtility.PingObject(obj);
					}

					if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
					{
						_rightClickIndex = i;
						ShowRowContextMenu(_filtered[i]);
						Event.current.Use();
					}
				}

				DrawRowImmediate(_filtered[i], rect);
			}

			GUI.EndClip();

			// Repaint only when the highlighted row changes — not on every mouse-move
			if (newHovered != _hoveredIndex)
			{
				_hoveredIndex = newHovered;
				Repaint();
			}
		}

		private void ShowRowContextMenu(EventEntry entry)
		{
			var menu = new GenericMenu();

			menu.AddItem(new GUIContent("Ping Prefab"), false, () =>
			{
				var obj = AssetDatabase.LoadAssetAtPath<GameObject>(entry.PrefabPath);
				if (obj != null) EditorGUIUtility.PingObject(obj);
			});

			menu.AddSeparator("");

			menu.ShowAsContext();
		}

		private void DrawTableHeader()
		{
			var rect = EditorGUILayout.BeginHorizontal();
			EditorGUI.DrawRect(rect, HeaderColor);
			GUILayout.Label("Prefab Path", _headerStyle, GUILayout.Width(ColPrefab));
			DrawSep();
			GUILayout.Label("GameObject", _headerStyle, GUILayout.Width(ColGO));
			DrawSep();
			GUILayout.Label("Component", _headerStyle, GUILayout.Width(ColComponent));
			DrawSep();
			GUILayout.Label("Field", _headerStyle, GUILayout.Width(ColField));
			DrawSep();
			GUILayout.Label("Listeners", _headerStyle, GUILayout.Width(ColListeners));
			EditorGUILayout.EndHorizontal();
			EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1), HeaderSepColor);
		}

		private void DrawRowImmediate(EventEntry e, Rect row)
		{
			var x = 0f;
			x = DrawCell(e.DisplayPath, row, x, ColPrefab, _rowLabelStyle);
			DrawSepRect(row, x);
			x += 1f;
			x = DrawCell(e.GameObjectPath, row, x, ColGO, _rowLabelStyle);
			DrawSepRect(row, x);
			x += 1f;
			x = DrawCell(e.ComponentName, row, x, ColComponent, _rowLabelStyle);
			DrawSepRect(row, x);
			x += 1f;
			x = DrawCell(e.FieldName, row, x, ColField, _rowLabelStyle);
			DrawSepRect(row, x);
			x += 1f;
			GUI.Label(new Rect(x, row.y, ColListeners, row.height),
				e.ListenerCount.ToString(),
				e.HasListeners ? _hasListenersStyle : _noListenersStyle);
		}

		private static float DrawCell(string text, Rect row, float x, float width, GUIStyle style)
		{
			GUI.Label(new Rect(x, row.y, width, row.height), text, style);
			return x + width;
		}

		private static void DrawSepRect(Rect row, float x)
		{
			EditorGUI.DrawRect(new Rect(x, row.y, 1f, row.height), SepColor);
		}

		// GUILayout separator — used only in the header (not in the virtual scroll area)
		private static void DrawSep()
		{
			EditorGUI.DrawRect(GUILayoutUtility.GetRect(1, 20, GUILayout.Width(1)), SepColor);
		}

		#region SCANNING

		private void ScanAllPrefabs()
		{
			_results.Clear();
			_filtered.Clear();
			_scannedPrefabCount = 0;
			_totalEventCount    = 0;
			_scrollY            = 0f;
			_hoveredIndex       = -1;
			_isScanning         = true;
			Repaint();

			EnsureCallReflection();
			s_MethodExistsCache.Clear(); // stale after any recompile

			var fieldCache = new Dictionary<Type, FieldInfo[]>();

			try
			{
				var guids = AssetDatabase.FindAssets("t:Prefab");

				for (var g = 0; g < guids.Length; g++)
				{
					var path   = AssetDatabase.GUIDToAssetPath(guids[g]);
					var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
					if (prefab == null) continue;

					_scannedPrefabCount++;

					if (EditorUtility.DisplayCancelableProgressBar(
						    "Scanning prefabs…", path, (float)g / guids.Length))
						break;

					ScanGameObject(prefab, prefab, path, "", fieldCache);
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
				_isScanning      = false;
				_totalEventCount = _results.Count;
				ApplyFilters();
				Repaint();
			}
		}

		private void ScanGameObject(
			GameObject root, GameObject go, string prefabPath, string parentPath,
			Dictionary<Type, FieldInfo[]> fieldCache)
		{
			var goPath = string.IsNullOrEmpty(parentPath) ? go.name : $"{parentPath}/{go.name}";

			foreach (var component in go.GetComponents<Component>())
			{
				if (component == null) continue;

				var compType = component.GetType();

				if (!fieldCache.TryGetValue(compType, out var eventFields))
				{
					eventFields          = CollectUnityEventFields(compType);
					fieldCache[compType] = eventFields;
				}

				if (eventFields.Length == 0) continue;

				var goDisplayPath = goPath == root.name
					? "(root)"
					: goPath.Substring(root.name.Length + 1);

				var displayPath = TruncatePath(prefabPath, TruncateLen);

				foreach (var field in eventFields)
				{
					var ue    = field.GetValue(component) as UnityEventBase;
					var count = ue?.GetPersistentEventCount() ?? 0;

					var brokenCount = ue != null ? CountBrokenListeners(ue) : 0;

					var searchKey = string.Concat(
						prefabPath.ToLowerInvariant(),    "\n",
						goDisplayPath.ToLowerInvariant(), "\n",
						compType.Name.ToLowerInvariant(), "\n",
						field.Name.ToLowerInvariant());

					_results.Add(new EventEntry
					{
						PrefabPath          = prefabPath,
						GameObjectPath      = goDisplayPath,
						ComponentName       = compType.Name,
						FieldName           = field.Name,
						ListenerCount       = count,
						BrokenListenerCount = brokenCount,
						DisplayPath         = displayPath,
						SearchKey           = searchKey
					});
				}
			}

			foreach (UnityEngine.Transform child in go.transform)
			{
				ScanGameObject(root, child.gameObject, prefabPath, goPath, fieldCache);
			}
		}

		private void ScanGameObject(
			GameObject root, GameObject go, string prefabPath, string parentPath,
			Dictionary<Type, FieldInfo[]> fieldCache, List<EventEntry> output)
		{
			var goPath = string.IsNullOrEmpty(parentPath) ? go.name : $"{parentPath}/{go.name}";

			foreach (var component in go.GetComponents<Component>())
			{
				if (component == null) continue;
				var compType = component.GetType();

				if (!fieldCache.TryGetValue(compType, out var eventFields))
				{
					eventFields          = CollectUnityEventFields(compType);
					fieldCache[compType] = eventFields;
				}

				if (eventFields.Length == 0) continue;

				var goDisplayPath = goPath == root.name
					? "(root)"
					: goPath.Substring(root.name.Length + 1);

				var displayPath = TruncatePath(prefabPath, TruncateLen);

				foreach (var field in eventFields)
				{
					var ue           = field.GetValue(component) as UnityEventBase;
					var count        = ue?.GetPersistentEventCount() ?? 0;
					var brokenCount  = ue != null ? CountBrokenListeners(ue) : 0;

					var searchKey = string.Concat(
						prefabPath.ToLowerInvariant(),    "\n",
						goDisplayPath.ToLowerInvariant(), "\n",
						compType.Name.ToLowerInvariant(), "\n",
						field.Name.ToLowerInvariant());

					output.Add(new EventEntry
					{
						PrefabPath          = prefabPath,
						GameObjectPath      = goDisplayPath,
						ComponentName       = compType.Name,
						FieldName           = field.Name,
						ListenerCount       = count,
						BrokenListenerCount = brokenCount,
						DisplayPath         = displayPath,
						SearchKey           = searchKey
					});
				}
			}

			foreach (UnityEngine.Transform child in go.transform)
				ScanGameObject(root, child.gameObject, prefabPath, goPath, fieldCache, output);
		}

		private static FieldInfo[] CollectUnityEventFields(Type compType)
		{
			var result = new List<FieldInfo>();
			var t      = compType;
			while (t != null && t != typeof(object))
			{
				foreach (var f in t.GetFields(
					         BindingFlags.Instance | BindingFlags.Public |
					         BindingFlags.NonPublic))
					if (typeof(UnityEventBase).IsAssignableFrom(f.FieldType))
						result.Add(f);
				t = t.BaseType;
			}
			return result.ToArray();
		}

		// ── Broken-listener detection ──────────────────────────────────────────
		//
		// A persistent listener is broken when any of these are true:
		//
		//   (a) Target is null/missing and a method name is set
		//       → the referenced object was deleted or never assigned.
		//
		//   (b) Target is valid but method name is empty
		//       → method was removed after wiring.
		//
		//   (c) Target is valid, method name is set, but the method no longer
		//       exists on the target's type — this is exactly the "<Missing …>"
		//       case shown in the Inspector when a method is renamed or deleted.
		//       We verify by searching the target's Type for any method with that
		//       name (ignoring overload signature — Unity stores only the name).
		//
		//   (d) Listener is in EventDefinedByArgument / Object mode and its
		//       stored asset argument is a missing reference (non-null fake null).
		//
		// Empty slots (both target and method name unset) are NOT flagged.

		private static int CountBrokenListeners(UnityEventBase ue)
		{
			if (!s_CallReflectionReady    ||
			    s_PersistentCallsField == null ||
			    s_CallsField           == null ||
			    s_TargetField          == null ||
			    s_MethodNameField      == null)
				return 0;

			var group = s_PersistentCallsField.GetValue(ue);
			if (group == null) return 0;

			var calls = s_CallsField.GetValue(group) as System.Collections.IList;
			if (calls == null) return 0;

			var broken = 0;
			foreach (var call in calls)
			{
				if (call == null) { broken++; continue; }

				var target     = s_TargetField.GetValue(call)     as UnityEngine.Object;
				var methodName = s_MethodNameField.GetValue(call) as string;

				var hasTarget = target     != null;
				var hasMethod = !string.IsNullOrEmpty(methodName);

				// (a) Method set but target missing
				if (hasMethod && !hasTarget) { broken++; continue; }

				// (b) Target exists but no method name stored
				if (hasTarget && !hasMethod) { broken++; continue; }

				// Both unset → empty/unconfigured slot, skip
				if (!hasTarget) continue;

				// (c) Target and method name both present — verify the method
				//     actually exists on the target's concrete type.
				var targetType = target.GetType();
				var key        = (targetType, methodName);

				if (!s_MethodExistsCache.TryGetValue(key, out var methodExists))
				{
					methodExists = MethodExistsOnType(targetType, methodName);
					s_MethodExistsCache[key] = methodExists;
				}

				if (!methodExists) { broken++; continue; }

				// (d) Object-mode argument is a fake-null missing asset
				if (s_ArgumentsField != null && s_ArgUnityObjectField != null)
				{
					var args      = s_ArgumentsField.GetValue(call);
					var argObject = args != null
						? s_ArgUnityObjectField.GetValue(args) as UnityEngine.Object
						: null;

					// A non-null C# reference that Unity reports as null == missing asset
					if (argObject != null && !argObject)
						broken++;
				}
			}

			return broken;
		}

		// Returns true if targetType (or any base type / interface) exposes a
		// method with exactly this name.  We intentionally ignore parameter
		// types: Unity only stores the name, so a rename is the only signal we have.
		public static bool MethodExistsOnType(Type targetType, string methodName)
		{
			const BindingFlags flags =
				BindingFlags.Instance | BindingFlags.Static |
				BindingFlags.Public   | BindingFlags.NonPublic;

			var t = targetType;
			while (t != null && t != typeof(object))
			{
				// GetMethod throws AmbiguousMatchException when overloads exist — any
				// match is good enough for our purposes, so we fall back to GetMethods.
				foreach (var m in t.GetMethods(flags))
					if (m.Name == methodName) return true;

				t = t.BaseType;
			}
			return false;
		}

		public static void EnsureCallReflection()
		{
			if (s_CallReflectionReady) return;

			var baseType = typeof(UnityEventBase);
			s_PersistentCallsField = baseType.GetField(
				"m_PersistentCalls", BindingFlags.Instance | BindingFlags.NonPublic);

			if (s_PersistentCallsField != null)
			{
				var groupType = s_PersistentCallsField.FieldType;
				s_CallsField  = groupType.GetField(
					"m_Calls", BindingFlags.Instance | BindingFlags.NonPublic);

				if (s_CallsField != null)
				{
					var listType = s_CallsField.FieldType;
					var callType = listType.IsGenericType
						? listType.GetGenericArguments()[0]
						: null;

					if (callType != null)
					{
						s_TargetField     = callType.GetField("m_Target",
							BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
						s_MethodNameField = callType.GetField("m_MethodName",
							BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
						s_ModeField       = callType.GetField("m_Mode",
							BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
						s_ArgumentsField  = callType.GetField("m_Arguments",
							BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

						// ArgumentCache.m_ObjectArgument holds the asset ref in Object mode
						if (s_ArgumentsField != null)
							s_ArgUnityObjectField = s_ArgumentsField.FieldType.GetField(
								"m_ObjectArgument",
								BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
					}
				}
			}

			s_CallReflectionReady = true;
		}

		// <summary>
		/// Returns true when the assembly-qualified type name stored in the serialized
		/// event data resolves to an actually-loaded type.  A false result means the
		/// class was renamed, moved to a different namespace, or moved to a different
		/// assembly since the prefab was last saved.
		/// </summary>
		internal static bool AssemblyTypeNameIsValid(string assemblyTypeName)
		{
			if (string.IsNullOrEmpty(assemblyTypeName)) return true; // nothing stored = not broken
			// Type.GetType handles "FullTypeName, AssemblyName" format.
			// We pass false for both flags so it never throws.
			return Type.GetType(assemblyTypeName, throwOnError: false, ignoreCase: false) != null;
		}

		/// <summary>
		/// Searches every type in every currently-loaded assembly whose unqualified
		/// class name matches <paramref name="shortTypeName"/> (case-insensitive).
		/// Returns the assembly-qualified names of all candidates, sorted so that
		/// names containing preferred namespace fragments appear first.
		/// </summary>
		internal static List<string> FindTypesByShortName(string shortTypeName,
			string preferredNamespaceHint = null)
		{
			var results = new List<(string fullName, bool preferred)>();

			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				// Skip dynamic assemblies — GetTypes() throws on them
				if (asm.IsDynamic) continue;

				Type[] types;
				try   { types = asm.GetTypes(); }
				catch { continue; }

				foreach (var t in types)
				{
					if (!string.Equals(t.Name, shortTypeName, StringComparison.OrdinalIgnoreCase))
						continue;

					var assemblyQualified = t.AssemblyQualifiedName;
					if (string.IsNullOrEmpty(assemblyQualified)) continue;

					var preferred = !string.IsNullOrEmpty(preferredNamespaceHint) &&
					                (t.Namespace?.Contains(preferredNamespaceHint) ?? false);

					results.Add((assemblyQualified, preferred));
				}
			}

			// Preferred namespace hints bubble to the top
			results.Sort((a, b) =>
			{
				if (a.preferred != b.preferred) return a.preferred ? -1 : 1;
				return string.Compare(a.fullName, b.fullName, StringComparison.Ordinal);
			});

			var final = new List<string>(results.Count);
			foreach (var r in results) final.Add(r.fullName);
			return final;
		}

		#endregion

		#region FILTER

		private void ApplyFilters()
		{
			_filtered.Clear();
			var hasSearch   = !string.IsNullOrEmpty(_searchTextLower);
			var listenersOn = _showOnlyWithListeners;
			var brokenOnly  = _showOnlyBroken;

			foreach (var e in _results)
			{
				if (listenersOn && !e.HasListeners) continue;
				if (brokenOnly  && !e.IsBroken)    continue;
				if (hasSearch   && !e.SearchKey.Contains(_searchTextLower)) continue;
				_filtered.Add(e);
			}
		}

		#endregion

		#region MISC

		private void ExportCsv()
		{
			var path = EditorUtility.SaveFilePanel("Export CSV", "", "UnityEventScan.csv", "csv");
			if (string.IsNullOrEmpty(path)) return;

			var sb = new StringBuilder();
			sb.AppendLine("PrefabPath,GameObjectPath,Component,Field,ListenerCount");
			foreach (var e in _filtered)
				sb.AppendLine(
					$"\"{e.PrefabPath}\",\"{e.GameObjectPath}\",\"{e.ComponentName}\",\"{e.FieldName}\",{e.ListenerCount}");

			File.WriteAllText(path, sb.ToString());
			Debug.Log($"[UnityEventScanner] Exported {_filtered.Count} rows to {path}");
		}

		// Only called at scan time when building EventEntry — never per-frame
		private static string TruncatePath(string path, int maxLen)
		{
			if (path.Length <= maxLen) return path;
			return "…" + path.Substring(path.Length - maxLen + 1);
		}

		#endregion
	}
}
#endif