using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using US13.Clothing;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.Surgery.Procedures;
using US13.Items.Traits;
using US13.Player;
using US13.ScriptableObjects.Health;
using US13.UI.Systems.Lobby;
using BodyPart = US13.HealthV2.Living.BodyParts.BodyPart;

namespace US13.Core.Editor.Tools.Health
{
#if UNITY_EDITOR

	public class MobEditorWindow : EditorWindow
	{
		private PlayerHealthData _currentData;
		private BodyPartsBaseSO _bodyPartsBaseSO;
		private Vector2 _scrollPosition;
		private string[] _categories;
		private int _selectedCategoryIndex;
		private string _customCategory = "";
		private string _fileName = "";
		private readonly string mobSpeciesSOFilesPath = "Assets/Prefabs/Player/Resources/BodyParts/";
		private readonly string mobBodyPartsPath = "Assets/Prefabs/Items/Implants/";

		// Page navigation
		private enum EditorPage { MainMenu, SelectMob, EditMob, CreateMob }
		private EditorPage _currentPage = EditorPage.MainMenu;

		// Editing sections
		private enum EditingSection { BasicSettings, BodyParts, Customization, Food, Registration }
		private EditingSection _currentEditingSection = EditingSection.BasicSettings;

		private bool showBasicSettings = true;
		private bool showBodyParts = true;
		private bool showCustomization = true;
		private bool showSkinColors = true;
		private bool showFood = true;

		private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

		// Styles
		private GUIStyle _headerStyle;
		private GUIStyle _breadcrumbStyle;

		[MenuItem("Window/Mob Editor")]
		public static void ShowWindow()
		{
			GetWindow<MobEditorWindow>("Mob Editor");
		}

		private void OnEnable()
		{
			LoadCategories();
			FindBodyPartsBaseSO();
			InitializeStyles();
		}

		private void InitializeStyles()
		{
			_headerStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 14,
				padding = new RectOffset(5, 5, 5, 5),
				alignment = TextAnchor.MiddleLeft
			};

			_breadcrumbStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				padding = new RectOffset(5, 5, 2, 2)
			};
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginVertical();

			// Draw breadcrumb navigation
			DrawBreadcrumb();

			EditorGUILayout.Space(5);

			// Draw page content
			switch (_currentPage)
			{
				case EditorPage.MainMenu:
					DrawMainMenu();
					break;
				case EditorPage.SelectMob:
					DrawSelectMobPage();
					break;
				case EditorPage.EditMob:
					DrawEditMobPage();
					break;
				case EditorPage.CreateMob:
					DrawCreateMobPage();
					break;
			}

			EditorGUILayout.EndVertical();
		}

		private void DrawBreadcrumb()
		{
			EditorGUILayout.BeginHorizontal(_breadcrumbStyle);
			switch (_currentPage)
			{
				case EditorPage.MainMenu:
					EditorGUILayout.LabelField("Main Menu", EditorStyles.miniLabel);
					break;
				case EditorPage.SelectMob:
					if (GUILayout.Button("Main Menu", EditorStyles.miniButton, GUILayout.Width(80)))
						GoToMainMenu();
					EditorGUILayout.LabelField("→ Select Mob", EditorStyles.miniLabel);
					break;
				case EditorPage.EditMob:
					if (GUILayout.Button("Main Menu", EditorStyles.miniButton, GUILayout.Width(80)))
						GoToMainMenu();
					EditorGUILayout.LabelField("→ Edit Mob", EditorStyles.miniLabel);
					if (_currentData != null)
						EditorGUILayout.LabelField($"({_currentData.name})", EditorStyles.miniLabel);
					break;
				case EditorPage.CreateMob:
					if (GUILayout.Button("Main Menu", EditorStyles.miniButton, GUILayout.Width(80)))
						GoToMainMenu();
					EditorGUILayout.LabelField("→ Create New Mob", EditorStyles.miniLabel);
					break;
			}

			EditorGUILayout.EndHorizontal();
		}

		private void FindBodyPartsBaseSO()
		{
			var guids = AssetDatabase.FindAssets("t:BodyPartsBaseSO");
			if (guids.Length > 0)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[0]);
				_bodyPartsBaseSO = AssetDatabase.LoadAssetAtPath<BodyPartsBaseSO>(path);
			}
		}

		#region Navigation Methods
		private void GoToMainMenu()
		{
			_currentPage = EditorPage.MainMenu;
			_currentData = null;
			_customCategory = "";
			_fileName = "";
		}

		private void GoToSelectMob()
		{
			_currentPage = EditorPage.SelectMob;
			_currentData = null;
		}

		private void GoToEditMob(PlayerHealthData data)
		{
			_currentData = data;
			_currentPage = EditorPage.EditMob;
		}

		private void GoToCreateMob()
		{
			_currentPage = EditorPage.CreateMob;
			_customCategory = "";
			_fileName = "";
		}
		#endregion

		#region Page Drawings
		private void DrawMainMenu()
		{
			EditorGUILayout.LabelField("Mob Editor", _headerStyle);
			EditorGUILayout.HelpBox("Welcome to the Mob Editor. Select an option below to get started.", MessageType.Info);

			EditorGUILayout.Space(10);

			// Create New Section
			EditorGUILayout.LabelField("Create New Mob Species", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField("Create a brand new species with custom configurations.", EditorStyles.wordWrappedLabel);
			if (GUILayout.Button("+ Create New Species", GUILayout.Height(35)))
				GoToCreateMob();
			EditorGUILayout.EndVertical();

			EditorGUILayout.Space(10);

			// Edit Existing Section
			EditorGUILayout.LabelField("Edit Existing Species", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField("Select and edit an existing species configuration.", EditorStyles.wordWrappedLabel);
			if (GUILayout.Button("Edit Species", GUILayout.Height(35)))
				GoToSelectMob();
			EditorGUILayout.EndVertical();
		}

		private void DrawSelectMobPage()
		{
			EditorGUILayout.LabelField("Select Species to Edit", _headerStyle);
			EditorGUILayout.HelpBox("Click on a species to edit its configuration.", MessageType.Info);

			EditorGUILayout.Space(10);

			var guids = AssetDatabase.FindAssets("t:PlayerHealthData");
			if (guids.Length == 0)
			{
				EditorGUILayout.HelpBox("No species found. Create a new one first.", MessageType.Warning);
				if (GUILayout.Button("Create New Species", GUILayout.Height(30)))
					GoToCreateMob();
				return;
			}

			// Display species in a scrollable list
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var data = AssetDatabase.LoadAssetAtPath<PlayerHealthData>(path);

				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.LabelField(data.name, EditorStyles.boldLabel);
				EditorGUILayout.LabelField($"Path: {path}", EditorStyles.miniLabel);

				if (GUILayout.Button("Edit", GUILayout.Height(30)))
					GoToEditMob(data);

				EditorGUILayout.EndVertical();
				EditorGUILayout.Space(5);
			}

			EditorGUILayout.EndScrollView();
		}

		private void DrawEditMobPage()
		{
			EditorGUILayout.LabelField($"Editing: {_currentData.name}", _headerStyle);

			// Check if data is initialized
			if (_currentData.Base == null)
			{
				EditorGUILayout.HelpBox("Base Race Health Data is null. Please initialize it first.", MessageType.Error);
				if (GUILayout.Button("Initialize Base Race Health Data", GUILayout.Height(30)))
					_currentData.Base = new RaceHealthData();
				return;
			}

			// Main layout: Sidebar on left, content on right
			EditorGUILayout.BeginHorizontal();

			// Left sidebar with section buttons
			EditorGUILayout.BeginVertical(GUILayout.Width(200));
			EditorGUILayout.LabelField("Settings Categories", EditorStyles.boldLabel);
			EditorGUILayout.Space(10);

			// Section buttons
			if (DrawSectionButton("Basic Settings", EditingSection.BasicSettings))
				_currentEditingSection = EditingSection.BasicSettings;

			if (DrawSectionButton("Body Parts & Limbs", EditingSection.BodyParts))
				_currentEditingSection = EditingSection.BodyParts;

			if (DrawSectionButton("Customization", EditingSection.Customization))
				_currentEditingSection = EditingSection.Customization;

			if (DrawSectionButton("Food Products", EditingSection.Food))
				_currentEditingSection = EditingSection.Food;

			if (DrawSectionButton("Registration", EditingSection.Registration))
				_currentEditingSection = EditingSection.Registration;

			EditorGUILayout.EndVertical();

			// Right content area
			EditorGUILayout.BeginVertical();
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

			// Quick Info Box at top
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField($"Editing: {_currentData.name}", EditorStyles.miniLabel);
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space(10);

			// Draw the selected section content
			DrawSelectedSectionContent();

			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();

			// Footer buttons
			EditorGUILayout.Space(10);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Save Changes", GUILayout.Height(35)))
			{
				EditorUtility.SetDirty(_currentData);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				EditorUtility.DisplayDialog("Success", "Species saved successfully!", "OK");
				GoToSelectMob();
			}
			if (GUILayout.Button("Cancel", GUILayout.Height(35)))
				GoToSelectMob();
			EditorGUILayout.EndHorizontal();
		}

		private bool DrawSectionButton(string label, EditingSection section)
		{
			var isSelected = _currentEditingSection == section;
			var style = isSelected ? EditorStyles.toolbarButton : EditorStyles.miniButton;

			if (isSelected)
			{
				GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
			}

			var clicked = GUILayout.Button(label, style, GUILayout.Height(35));

			GUI.backgroundColor = Color.white;

			return clicked;
		}

		private void DrawSelectedSectionContent()
		{
			switch (_currentEditingSection)
			{
				case EditingSection.BasicSettings:
					DrawBasicSettingsContent();
					break;
				case EditingSection.BodyParts:
					DrawBodyPartsContent();
					break;
				case EditingSection.Customization:
					DrawCustomizationContent();
					break;
				case EditingSection.Food:
					DrawFoodContent();
					break;
				case EditingSection.Registration:
					DrawRegistrationContent();
					break;
			}
		}
		#endregion

		private void DrawCreateMobPage()
		{
			EditorGUILayout.LabelField("Create New Mob Species", _headerStyle);
			EditorGUILayout.HelpBox("Create a new mob species by selecting a category and entering a file name.", MessageType.Info);

			EditorGUILayout.Space(10);

			// Category selection
			EditorGUILayout.LabelField("Category", EditorStyles.boldLabel);
			if (_categories != null && _categories.Length > 0)
			{
				_selectedCategoryIndex = EditorGUILayout.Popup("Select Category", _selectedCategoryIndex, _categories);
				_customCategory = _categories[_selectedCategoryIndex];
			}
			else
			{
				_customCategory = EditorGUILayout.TextField("Custom Category", _customCategory);
			}

			EditorGUILayout.Space(10);

			// File name input
			EditorGUILayout.LabelField("Species Name", EditorStyles.boldLabel);
			_fileName = EditorGUILayout.TextField("File Name", _fileName);

			EditorGUILayout.Space(20);

			// Create button
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Create Species", GUILayout.Height(40), GUILayout.Width(150)))
			{
				if (string.IsNullOrEmpty(_customCategory) || string.IsNullOrEmpty(_fileName))
				{
					EditorUtility.DisplayDialog("Error", "Please fill in both category and file name.", "OK");
				}
				else
				{
					CreateNewPlayerHealthData(_customCategory, _fileName);
					GoToEditMob(_currentData);
				}
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		private void LoadCategories()
		{
			if (Directory.Exists(mobSpeciesSOFilesPath))
			{
				var directories = Directory.GetDirectories(mobSpeciesSOFilesPath);
				_categories = new string[directories.Length];
				for (var i = 0; i < directories.Length; i++)
					_categories[i] = Path.GetFileName(directories[i]); // Get only the folder name, not the full path
			}
			else
			{
				_categories = new[] { "No Categories Found" };
			}
		}


		private void CreateNewPlayerHealthData(string category, string fileName)
		{
			var folderPath = $"{mobSpeciesSOFilesPath}{category}";
			if (!AssetDatabase.IsValidFolder(folderPath))
			{
				Directory.CreateDirectory(folderPath);
				AssetDatabase.Refresh();
			}

			_currentData = CreateInstance<PlayerHealthData>();

			var path = $"{folderPath}/{fileName}.asset";
			AssetDatabase.CreateAsset(_currentData, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private void DrawPlayerHealthDataEditor()
		{
			if (_currentData == null)
			{
				EditorGUILayout.HelpBox("No species data loaded.", MessageType.Warning);
				return;
			}

			// Reference BodyPartsBaseSO
			_bodyPartsBaseSO = (BodyPartsBaseSO)EditorGUILayout.ObjectField("Body Parts Base", _bodyPartsBaseSO,
				typeof(BodyPartsBaseSO), false);

			EditorGUILayout.Space(10);

			DrawBasicSettingsSection();
			EditorGUILayout.Space(15);

			DrawBodyPartsSection();
			EditorGUILayout.Space(15);

			DrawCustomizationSection();
			EditorGUILayout.Space(15);

			DrawFoodSection();
			EditorGUILayout.Space(15);

			DrawRegistrationSection();
		}

		#region Section Drawing Methods
		private void DrawBasicSettingsSection()
		{
			showBasicSettings = EditorGUILayout.Foldout(showBasicSettings, "Basic Settings", true, EditorStyles.foldout);
			if (showBasicSettings)
			{
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);

				EditorGUILayout.LabelField("Species Identification Data", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox("Data that will be used to identify this species. Do not leave empty.", MessageType.Info);

				if (_currentData?.Base == null)
				{
					EditorGUILayout.HelpBox("Base Race Health Data is null. Initialize it first!", MessageType.Error);
					if (GUILayout.Button("Initialize Base Race Health Data", GUILayout.Height(30)))
						_currentData.Base = new RaceHealthData();
					EditorGUILayout.EndVertical();
					return;
				}

				var raceData = _currentData.Base;

				raceData.RootImplantProcedure = (ImplantProcedure)EditorGUILayout.ObjectField("Root Implant Procedure",
					raceData.RootImplantProcedure, typeof(ImplantProcedure), false);

				raceData.ClueString = EditorGUILayout.TextField("Clue String", raceData.ClueString);

				EditorGUILayout.Space(5);
				EditorGUILayout.LabelField("Customization Options", EditorStyles.boldLabel);
				raceData.allowedToChangeling = EditorGUILayout.Toggle("Allowed for Changeling", raceData.allowedToChangeling);
				raceData.CanBePlayerChosen = EditorGUILayout.Toggle("Can Be Player Chosen", raceData.CanBePlayerChosen);

				raceData.PreviewSprite = (SpriteDataSO)EditorGUILayout.ObjectField("Preview Sprite",
					raceData.PreviewSprite, typeof(SpriteDataSO), false);

				EditorGUILayout.Space(10);
				EditorGUILayout.LabelField("Health Systems", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox("Health systems are edited in the PlayerHealthData ScriptableObject directly.", MessageType.Info);
				if (GUILayout.Button("Open Player Health Data Asset", GUILayout.Height(25)))
				{
					Selection.activeObject = _currentData;
					EditorGUIUtility.PingObject(_currentData);
				}

				EditorGUILayout.EndVertical();
			}
		}

		private void DrawBodyPartsSection()
		{
			showBodyParts = EditorGUILayout.Foldout(showBodyParts, "Body Parts & Limbs", true, EditorStyles.foldout);
			if (showBodyParts)
			{
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);

				if (_currentData?.Base == null)
				{
					EditorGUILayout.HelpBox("Base Race Health Data is null.", MessageType.Error);
					EditorGUILayout.EndVertical();
					return;
				}

				EditorGUILayout.LabelField("Limb Configuration", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox("Every mob is composed of different limbs that include organs inside them. " +
				                        "Use the 'Generate Limbs' button to auto-create variants for all body parts.", MessageType.Info);

				// Generate button
				EditorGUILayout.BeginHorizontal();
				if (_bodyPartsBaseSO != null && GUILayout.Button("Generate All Limbs", GUILayout.Height(35)))
					GenerateBodyPartVariants(_currentData.Base);
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space(10);

				// Body parts fields
				_currentData.Base.Head = DrawObjectListField("Head", _currentData.Base.Head, _currentData.Base);
				EditorGUILayout.Space(8);
				_currentData.Base.Torso = DrawObjectListField("Torso", _currentData.Base.Torso, _currentData.Base);
				EditorGUILayout.Space(8);
				_currentData.Base.ArmRight = DrawObjectListField("Right Arm", _currentData.Base.ArmRight, _currentData.Base);
				EditorGUILayout.Space(8);
				_currentData.Base.ArmLeft = DrawObjectListField("Left Arm", _currentData.Base.ArmLeft, _currentData.Base);
				EditorGUILayout.Space(8);
				_currentData.Base.LegRight = DrawObjectListField("Right Leg", _currentData.Base.LegRight, _currentData.Base);
				EditorGUILayout.Space(8);
				_currentData.Base.LegLeft = DrawObjectListField("Left Leg", _currentData.Base.LegLeft, _currentData.Base);

				EditorGUILayout.EndVertical();
			}
		}

		private void DrawCustomizationSection()
		{
			showCustomization = EditorGUILayout.Foldout(showCustomization, "Customization Settings", true, EditorStyles.foldout);
			if (showCustomization)
			{
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);

				if (_currentData?.Base == null)
				{
					EditorGUILayout.HelpBox("Base Race Health Data is null.", MessageType.Error);
					EditorGUILayout.EndVertical();
					return;
				}

				// Body Type Settings
				EditorGUILayout.LabelField("Body Type Configuration", EditorStyles.boldLabel);
				_currentData.Base.bodyTypeSettings = DrawBodyTypeSettings(_currentData.Base.bodyTypeSettings);
				EditorGUILayout.Space(10);

				// Skin Colors
				DrawSkinColorsSubsection();
				EditorGUILayout.Space(10);

				// Customisation Settings
				EditorGUILayout.LabelField("Customization Blacklist", EditorStyles.boldLabel);
				foreach (var setting in _currentData.Base.CustomisationSettings)
				{
					EditorGUILayout.LabelField($"Group: {setting.CustomisationGroup.name}", EditorStyles.miniLabel);
					EditorGUILayout.LabelField("Blacklisted Items:", EditorStyles.boldLabel);
					foreach (var blacklisted in setting.Blacklist)
					{
						EditorGUILayout.ObjectField(blacklisted, typeof(PlayerCustomisationData), false);
					}
					EditorGUILayout.Space(5);
				}

				EditorGUILayout.EndVertical();
			}
		}

		private void DrawSkinColorsSubsection()
		{
			showSkinColors = EditorGUILayout.Foldout(showSkinColors, "Skin Colors", true);
			if (showSkinColors)
			{
				var raceData = _currentData?.Base;
				if (raceData == null)
				{
					EditorGUILayout.HelpBox("Base Race Health Data is null.", MessageType.Error);
					return;
				}

				var skinColorCount = EditorGUILayout.IntField("Number of Skin Colors", raceData.SkinColours.Count);
				while (skinColorCount > raceData.SkinColours.Count)
				{
					raceData.SkinColours.Add(Color.white);
				}

				while (skinColorCount < raceData.SkinColours.Count)
				{
					raceData.SkinColours.RemoveAt(raceData.SkinColours.Count - 1);
				}

				for (var i = 0; i < raceData.SkinColours.Count; i++)
				{
					raceData.SkinColours[i] = EditorGUILayout.ColorField($"Color {i + 1}", raceData.SkinColours[i]);
				}

				EditorGUILayout.Space(10);
				EditorGUILayout.LabelField("Body Parts Using Skin Tone", EditorStyles.boldLabel);

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Parts affected by skin color:", EditorStyles.miniLabel);
				if (GUILayout.Button("Add Body Part", GUILayout.Height(20)))
				{
					raceData.BodyPartsThatShareTheSkinTone.Add(null);
				}
				EditorGUILayout.EndHorizontal();

				for (var i = 0; i < raceData.BodyPartsThatShareTheSkinTone.Count; i++)
				{
					EditorGUILayout.BeginHorizontal();
					raceData.BodyPartsThatShareTheSkinTone[i] = (BodyPart)EditorGUILayout.ObjectField($"Body Part {i + 1}",
						raceData.BodyPartsThatShareTheSkinTone[i], typeof(BodyPart), false);
					if (GUILayout.Button("✕", GUILayout.Width(25)))
					{
						raceData.BodyPartsThatShareTheSkinTone.RemoveAt(i);
						break;
					}
					EditorGUILayout.EndHorizontal();
				}
			}
		}

		private void DrawFoodSection()
		{
			showFood = EditorGUILayout.Foldout(showFood, "Food Products", true, EditorStyles.foldout);
			if (showFood)
			{
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);

				var raceData = _currentData?.Base;
				if (raceData == null)
				{
					EditorGUILayout.HelpBox("Base Race Health Data is null.", MessageType.Error);
					EditorGUILayout.EndVertical();
					return;
				}

				EditorGUILayout.LabelField("Harvested Food Items", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox("Items produced when this species is processed.", MessageType.Info);

				raceData.MeatProduce = (GameObject)EditorGUILayout.ObjectField("Meat Produce", raceData.MeatProduce, typeof(GameObject), false);
				EditorGUILayout.Space(5);
				raceData.SkinProduce = (GameObject)EditorGUILayout.ObjectField("Skin Produce", raceData.SkinProduce, typeof(GameObject), false);
				EditorGUILayout.Space(10);

				EditorGUILayout.LabelField("Skinning Tool", EditorStyles.boldLabel);
				raceData.SkinningItemTrait = (ItemTrait)EditorGUILayout.ObjectField("Required Tool Trait", raceData.SkinningItemTrait, typeof(ItemTrait), false);

				EditorGUILayout.EndVertical();
			}
		}

		private void DrawRegistrationSection()
		{
			if (_currentData == null || _currentData.Base == null)
				return;

			if (RaceSOSingleton.Instance != null && RaceSOSingleton.Instance.Races.Contains(_currentData) == false)
			{
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.LabelField("⚠Registration Required", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox("This species is not registered in the RaceSOSingleton. " +
				                        "It must be added for this race to function in-game.", MessageType.Warning);
				if (GUILayout.Button("Register Species Automatically", GUILayout.Height(30)))
				{
					RaceSOSingleton.Instance.Races.Add(_currentData);
					EditorUtility.DisplayDialog("Success", "Species registered successfully!", "OK");
				}
				EditorGUILayout.EndVertical();
			}
		}
		#endregion

		private BodyTypeSettings DrawBodyTypeSettings(BodyTypeSettings bodyTypeSettings)
		{
			EditorGUILayout.LabelField("Body Type Settings", EditorStyles.boldLabel);

			// Adjust the number of BodyTypes in the list
			var bodyTypeCount =
				EditorGUILayout.IntField("Number of Body Types", bodyTypeSettings.AvailableBodyTypes.Count);
			while (bodyTypeCount > bodyTypeSettings.AvailableBodyTypes.Count)
				bodyTypeSettings.AvailableBodyTypes.Add(new BodyTypeName());
			while (bodyTypeCount < bodyTypeSettings.AvailableBodyTypes.Count)
				bodyTypeSettings.AvailableBodyTypes.RemoveAt(bodyTypeSettings.AvailableBodyTypes.Count - 1);

			// Loop through all BodyTypes and draw UI for each one
			for (var i = 0; i < bodyTypeSettings.AvailableBodyTypes.Count; i++)
			{
				// Assuming BodyType is an enum, you can use EnumPopup to select a value
				bodyTypeSettings.AvailableBodyTypes[i].bodyType =
					(BodyType)EditorGUILayout.EnumPopup($"Body Type {i + 1}",
						bodyTypeSettings.AvailableBodyTypes[i].bodyType);

				// Draw a field for the name of the body type
				bodyTypeSettings.AvailableBodyTypes[i].Name =
					EditorGUILayout.TextField("Body Type Name", bodyTypeSettings.AvailableBodyTypes[i].Name);
			}

			return bodyTypeSettings;
		}


		// Method to generate prefab variants
		private void GenerateBodyPartVariants(RaceHealthData raceData)
		{
			var variantFolderPath = $"{mobBodyPartsPath}/{_currentData.name}/";
			if (Directory.Exists(variantFolderPath) == false) Directory.CreateDirectory(variantFolderPath);

			raceData.Head.Elements.Add(CreatePrefabVariant(_bodyPartsBaseSO.HeadBase,
				$"{variantFolderPath}{_currentData.name}-Head.prefab"));
			raceData.Torso.Elements.Add(CreatePrefabVariant(_bodyPartsBaseSO.TorsoBase,
				$"{variantFolderPath}{_currentData.name}-Torso.prefab"));
			raceData.ArmRight.Elements.Add(CreatePrefabVariant(_bodyPartsBaseSO.ArmRightBase,
				$"{variantFolderPath}{_currentData.name}-ArmRight.prefab"));
			raceData.ArmLeft.Elements.Add(CreatePrefabVariant(_bodyPartsBaseSO.ArmLeftBase,
				$"{variantFolderPath}{_currentData.name}-ArmLeft.prefab"));
			raceData.LegRight.Elements.Add(CreatePrefabVariant(_bodyPartsBaseSO.LegRightBase,
				$"{variantFolderPath}{_currentData.name}-LegRight.prefab"));
			raceData.LegLeft.Elements.Add(CreatePrefabVariant(_bodyPartsBaseSO.LegLeftBase,
				$"{variantFolderPath}{_currentData.name}LegLeft.prefab"));

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private GameObject CreatePrefabVariant(GameObject basePrefab, string variantPath)
		{
			if (basePrefab == null)
			{
				Debug.LogError("Base prefab is null! Cannot create variant.");
				return null;
			}

			var instance = Instantiate(basePrefab);
			PrefabUtility.SaveAsPrefabAssetAndConnect(instance, variantPath, InteractionMode.AutomatedAction);
			DestroyImmediate(instance);

			return AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
		}

		private ObjectList DrawObjectListField(string label, ObjectList objectList, RaceHealthData raceHealthData)
		{
			if (objectList == null)
				return objectList;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
			if (GUILayout.Button("Add " + label, GUILayout.Height(15))) objectList.Elements.Add(null);
			EditorGUILayout.EndHorizontal();

			var count = Mathf.Max(0, EditorGUILayout.IntField("Number of Elements", objectList.Elements.Count));
			while (count < objectList.Elements.Count)
				objectList.Elements.RemoveAt(objectList.Elements.Count - 1);
			while (count > objectList.Elements.Count)
				objectList.Elements.Add(null);

			for (var i = 0; i < objectList.Elements.Count; i++)
			{
				objectList.Elements[i] = (GameObject)EditorGUILayout.ObjectField($"Element {i + 1}",
					objectList.Elements[i], typeof(GameObject), false);

				if (objectList.Elements[i] != null &&
				    objectList.Elements[i].TryGetComponent<BodyPart>(out var bodyPart))
				{
					var elementName = objectList.Elements[i].name;
					foldoutStates.TryAdd(elementName, true);

					foldoutStates[elementName] =
						EditorGUILayout.Foldout(foldoutStates[elementName], $"Sprites for {elementName}");
					if (foldoutStates[elementName])
					{
						var bodyTypeSprites = bodyPart.GetBodyTypesSprites;
						RenderBodySpritesSettings(bodyTypeSprites, raceHealthData);
						RenderBodyItemSpritesOptions(objectList, bodyPart, elementName, i);
					}
				}
			}
			return objectList;
		}

		private static void RenderBodyItemSpritesOptions(ObjectList objectList, BodyPart bodyPart, string elementName, int i)
		{
			var handler = bodyPart.BodyPartItemSprite;
			if (handler != null)
			{
				EditorGUILayout.LabelField("Item Sprites", EditorStyles.boldLabel);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.HelpBox(
					"Cannot edit item sprites directly in this tool. Manually assign SpriteSOs in prefab.",
					MessageType.Warning);
				if (GUILayout.Button($"Open {elementName} prefab"))
				{
					Selection.activeObject = objectList.Elements[i];
					EditorGUIUtility.PingObject(objectList.Elements[i]);
				}
				EditorGUILayout.EndHorizontal();
			}
		}

		private static void RenderBodySpritesSettings(BodyTypesWithOrder bodyTypeSprites, RaceHealthData raceHealthData)
		{
			EditorGUILayout.LabelField("Body Sprites", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox(
				"The sprites mobs will render on their physical bodies. " +
				"Each sprite index should match this mob's defined BodyType index.",
				MessageType.Info);
			var bodyTypeIndex = 0;
			foreach (var spriteDataSO in bodyTypeSprites.BodyTypes)
			{
				EditorGUILayout.BeginHorizontal();
				if (spriteDataSO == null || spriteDataSO.Sprites == null)
				{
					EditorGUILayout.HelpBox("SpriteSO has missing data. Please assign a SpriteSO in its prefab.", MessageType.Error);
					continue;
				}
				try
				{
					var previewSprite = spriteDataSO.Sprites[0]?.GetFirstSprite.texture;
					if (previewSprite != null)
					{
						GUILayout.Box(previewSprite, GUILayout.Width(50), GUILayout.Height(50));
					}
				}
				catch (Exception e)
				{
					EditorGUILayout.HelpBox("SpriteSO has missing data. Please fill out everything.", MessageType.Error);
				}
				EditorGUILayout.EndHorizontal();

				for (var k = 0; k < spriteDataSO.Sprites.Count; k++)
				{
					if (spriteDataSO.Sprites[k] == null)
					{
						EditorGUILayout.HelpBox("SpriteSO has missing sprite data. Please assign it in the prefab.", MessageType.Error);
						continue;
					}
					if (raceHealthData.bodyTypeSettings.AvailableBodyTypes.Count <= bodyTypeIndex)
					{
						spriteDataSO.Sprites[k] = (SpriteDataSO)EditorGUILayout.ObjectField($"Sprite {k + 1}",
							spriteDataSO.Sprites[k], typeof(SpriteDataSO), false);
					}
					else
					{
						spriteDataSO.Sprites[k] = (SpriteDataSO)EditorGUILayout.ObjectField($"Sprite {raceHealthData.bodyTypeSettings.AvailableBodyTypes[bodyTypeIndex].Name} {k + 1}",
							spriteDataSO.Sprites[k], typeof(SpriteDataSO), false);
					}
				}
				bodyTypeIndex++;
			}
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Add Sprite", GUILayout.Height(15)))
			{
				bodyTypeSprites.BodyTypes.Add(bodyTypeSprites.BodyTypes.Count > 0
					? bodyTypeSprites.BodyTypes[0]
					: null);
			}
			if (GUILayout.Button("Remove Sprite", GUILayout.Height(15)))
				if (bodyTypeSprites.BodyTypes.Count > 0)
					bodyTypeSprites.BodyTypes.RemoveAt(bodyTypeSprites.BodyTypes.Count - 1);
			EditorGUILayout.EndHorizontal();
		}

		#region Section Content Methods
		private void DrawBasicSettingsContent()
		{
			EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Configure the fundamental properties that identify this species.", MessageType.Info);
			EditorGUILayout.Space(10);

			// Reference BodyPartsBaseSO
			_bodyPartsBaseSO = (BodyPartsBaseSO)EditorGUILayout.ObjectField("Body Parts Base", _bodyPartsBaseSO,
				typeof(BodyPartsBaseSO), false);
			EditorGUILayout.Space(10);

			var raceData = _currentData.Base;

			EditorGUILayout.LabelField("Species Identification", EditorStyles.boldLabel);
			raceData.RootImplantProcedure = (ImplantProcedure)EditorGUILayout.ObjectField("Root Implant Procedure",
				raceData.RootImplantProcedure, typeof(ImplantProcedure), false);
			raceData.ClueString = EditorGUILayout.TextField("Clue String", raceData.ClueString);
			raceData.PreviewSprite = (SpriteDataSO)EditorGUILayout.ObjectField("Preview Sprite",
				raceData.PreviewSprite, typeof(SpriteDataSO), false);

			EditorGUILayout.Space(15);
			EditorGUILayout.LabelField("Gameplay Options", EditorStyles.boldLabel);
			raceData.allowedToChangeling = EditorGUILayout.Toggle("Allowed for Changeling", raceData.allowedToChangeling);
			raceData.CanBePlayerChosen = EditorGUILayout.Toggle("Can Be Player Chosen", raceData.CanBePlayerChosen);

			EditorGUILayout.Space(15);
			EditorGUILayout.LabelField("Health Systems", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Health systems are configured in the PlayerHealthData ScriptableObject directly.", MessageType.Info);
			if (GUILayout.Button("Open Player Health Data Asset", GUILayout.Height(30)))
			{
				Selection.activeObject = _currentData;
				EditorGUIUtility.PingObject(_currentData);
			}
		}

		private void DrawBodyPartsContent()
		{
			EditorGUILayout.LabelField("Body Parts & Limbs", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Configure the limbs and body parts that make up this species.", MessageType.Info);
			EditorGUILayout.Space(10);

			// Generate button
			if (_bodyPartsBaseSO != null && GUILayout.Button("Generate All Limbs", GUILayout.Height(40)))
				GenerateBodyPartVariants(_currentData.Base);
			EditorGUILayout.Space(10);

			// Body parts configuration
			EditorGUILayout.LabelField("Limb Assignments", EditorStyles.boldLabel);
			_currentData.Base.Head = DrawObjectListField("Head", _currentData.Base.Head, _currentData.Base);
			EditorGUILayout.Space(8);
			_currentData.Base.Torso = DrawObjectListField("Torso", _currentData.Base.Torso, _currentData.Base);
			EditorGUILayout.Space(8);
			_currentData.Base.ArmRight = DrawObjectListField("Right Arm", _currentData.Base.ArmRight, _currentData.Base);
			EditorGUILayout.Space(8);
			_currentData.Base.ArmLeft = DrawObjectListField("Left Arm", _currentData.Base.ArmLeft, _currentData.Base);
			EditorGUILayout.Space(8);
			_currentData.Base.LegRight = DrawObjectListField("Right Leg", _currentData.Base.LegRight, _currentData.Base);
			EditorGUILayout.Space(8);
			_currentData.Base.LegLeft = DrawObjectListField("Left Leg", _currentData.Base.LegLeft, _currentData.Base);
		}

		private void DrawCustomizationContent()
		{
			EditorGUILayout.LabelField("Customization Settings", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Configure appearance options and customization restrictions.", MessageType.Info);
			EditorGUILayout.Space(10);

			// Body Type Settings
			EditorGUILayout.LabelField("Body Type Configuration", EditorStyles.boldLabel);
			_currentData.Base.bodyTypeSettings = DrawBodyTypeSettings(_currentData.Base.bodyTypeSettings);
			EditorGUILayout.Space(15);

			// Skin Colors
			EditorGUILayout.LabelField("Skin Colors", EditorStyles.boldLabel);
			var raceData = _currentData.Base;
			var skinColorCount = EditorGUILayout.IntField("Number of Skin Colors", raceData.SkinColours.Count);
			while (skinColorCount > raceData.SkinColours.Count)
				raceData.SkinColours.Add(Color.white);
			while (skinColorCount < raceData.SkinColours.Count)
				raceData.SkinColours.RemoveAt(raceData.SkinColours.Count - 1);

			for (var i = 0; i < raceData.SkinColours.Count; i++)
				raceData.SkinColours[i] = EditorGUILayout.ColorField($"Color {i + 1}", raceData.SkinColours[i]);

			EditorGUILayout.Space(15);
			EditorGUILayout.LabelField("Body Parts Using Skin Tone", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Parts affected by skin color:", EditorStyles.miniLabel);
			if (GUILayout.Button("Add Body Part", GUILayout.Height(20)))
				raceData.BodyPartsThatShareTheSkinTone.Add(null);
			EditorGUILayout.EndHorizontal();

			for (var i = 0; i < raceData.BodyPartsThatShareTheSkinTone.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				raceData.BodyPartsThatShareTheSkinTone[i] = (BodyPart)EditorGUILayout.ObjectField($"Body Part {i + 1}",
					raceData.BodyPartsThatShareTheSkinTone[i], typeof(BodyPart), false);
				if (GUILayout.Button("✕", GUILayout.Width(25)))
				{
					raceData.BodyPartsThatShareTheSkinTone.RemoveAt(i);
					break;
				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Space(15);
			EditorGUILayout.LabelField("Customization Blacklist", EditorStyles.boldLabel);
			foreach (var setting in raceData.CustomisationSettings)
			{
				EditorGUILayout.LabelField($"Group: {setting.CustomisationGroup.name}", EditorStyles.miniLabel);
				EditorGUILayout.LabelField("Blacklisted Items:", EditorStyles.boldLabel);
				foreach (var blacklisted in setting.Blacklist)
					EditorGUILayout.ObjectField(blacklisted, typeof(PlayerCustomisationData), false);
				EditorGUILayout.Space(5);
			}
		}

		private void DrawFoodContent()
		{
			EditorGUILayout.LabelField("🍖 Food Products", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Configure what items are produced when this species is harvested.", MessageType.Info);
			EditorGUILayout.Space(10);

			var raceData = _currentData.Base;

			EditorGUILayout.LabelField("Harvested Items", EditorStyles.boldLabel);
			raceData.MeatProduce = (GameObject)EditorGUILayout.ObjectField("Meat Produce", raceData.MeatProduce, typeof(GameObject), false);
			EditorGUILayout.Space(5);
			raceData.SkinProduce = (GameObject)EditorGUILayout.ObjectField("Skin Produce", raceData.SkinProduce, typeof(GameObject), false);

			EditorGUILayout.Space(15);
			EditorGUILayout.LabelField("Processing Requirements", EditorStyles.boldLabel);
			raceData.SkinningItemTrait = (ItemTrait)EditorGUILayout.ObjectField("Required Tool Trait", raceData.SkinningItemTrait, typeof(ItemTrait), false);
		}

		private void DrawRegistrationContent()
		{
			EditorGUILayout.LabelField("Species Registration", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Ensure this species is properly registered in the game's systems.", MessageType.Info);
			EditorGUILayout.Space(10);

			if (RaceSOSingleton.Instance != null && RaceSOSingleton.Instance.Races.Contains(_currentData) == false)
			{
				EditorGUILayout.HelpBox("This species is not registered in the RaceSOSingleton. It must be added for this race to function in-game.", MessageType.Warning);
				if (GUILayout.Button("Register Species Automatically", GUILayout.Height(40)))
				{
					RaceSOSingleton.Instance.Races.Add(_currentData);
					EditorUtility.DisplayDialog("Success", "Species registered successfully!", "OK");
				}
			}
			else
			{
				EditorGUILayout.HelpBox("This species is properly registered in the RaceSOSingleton.", MessageType.Info);
			}
		}
		#endregion
	}
#endif
}



