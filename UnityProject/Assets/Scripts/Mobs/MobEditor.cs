using System.IO;
using HealthV2;
using ScriptableObjects.Health;
using UnityEditor;
using UnityEngine;
using BodyPart = HealthV2.BodyPart;

namespace Mobs
{
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
		private int pageId;

		private bool showBasicSettings = true;
		private bool showBodyParts = true;
		private bool showCustomization = true;
		private bool showItems = true;

		[MenuItem("Window/Mob Editor")]
		public static void ShowWindow()
		{
			GetWindow<MobEditorWindow>("Mob Editor");
		}

		private void OnEnable()
		{
			LoadCategories();
			FindBodyPartsBaseSO();
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginVertical();

			switch (pageId)
			{
				case 0:
					DrawCreateNewOrEditExisting();
					break;
				case 1:
					DrawPlayerEditor();
					break;
				case 2:
					DrawPlayerHealthDataEditor();
					break;
				case 3:
					DrawCreateNewPage();
					break;
			}

			EditorGUILayout.EndVertical();
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

		private void DrawPlayerEditor()
		{
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
			DrawPlayerHealthDataEditor();
			EditorGUILayout.EndScrollView();

			if (GUILayout.Button("Save"))
			{
				EditorUtility.SetDirty(_currentData);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				_currentData = null;
				pageId = 0;
			}
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

		private void DrawCreateNewOrEditExisting()
		{
			GUILayout.Label("Create New Player Health Data", EditorStyles.boldLabel);

			if (GUILayout.Button("Create New")) pageId = 3;

			GUILayout.Space(20);

			GUILayout.Label("Edit Existing Player Health Data", EditorStyles.boldLabel);
			var guids = AssetDatabase.FindAssets("t:PlayerHealthData");
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var data = AssetDatabase.LoadAssetAtPath<PlayerHealthData>(path);
				if (GUILayout.Button(data.name))
				{
					_currentData = data;
					pageId = 1;
				}
			}
		}

		private void DrawCreateNewPage()
		{
			GUILayout.Label("Select a Category or Create a New One", EditorStyles.boldLabel);

			_selectedCategoryIndex = EditorGUILayout.Popup("Category", _selectedCategoryIndex, _categories);
			_customCategory = EditorGUILayout.TextField("Custom Category", _customCategory);

			_fileName = EditorGUILayout.TextField("File Name", _fileName);

			if (GUILayout.Button("Create"))
			{
				if (string.IsNullOrEmpty(_fileName))
				{
					EditorUtility.DisplayDialog("Error", "File name cannot be empty.", "OK");
					return;
				}

				var category = string.IsNullOrEmpty(_customCategory)
					? _categories[_selectedCategoryIndex]
					: _customCategory;
				CreateNewPlayerHealthData(category, _fileName);
				pageId = 2;
			}

			if (GUILayout.Button("Back")) _currentData = null;
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
			GUILayout.Label($"Edit Player Health Data for: {_currentData.name}", EditorStyles.centeredGreyMiniLabel);

			// Reference BodyPartsBaseSO
			_bodyPartsBaseSO = (BodyPartsBaseSO)EditorGUILayout.ObjectField("Body Parts Base", _bodyPartsBaseSO,
				typeof(BodyPartsBaseSO), false);

			if (_currentData.Base == null)
			{
				GUILayout.Label("Base Race Health Data is null. Please initialize it first.");
				if (GUILayout.Button("Initialize Base Race Health Data"))
					_currentData.Base = new RaceHealthData();
			}
			else
			{
				DrawRaceHealthDataEditor(_currentData.Base);
			}
		}

		private void DrawRaceHealthDataEditor(RaceHealthData raceData)
		{
			showBasicSettings = EditorGUILayout.Foldout(showBasicSettings, "Basic Settings");
			if (showBasicSettings)
			{
				GUILayout.Label($"Data that will be used to identify this species. Do not leave empty.", EditorStyles.helpBox);
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				raceData.RootImplantProcedure = (ImplantProcedure)EditorGUILayout.ObjectField("Root Implant Procedure",
					raceData.RootImplantProcedure, typeof(ImplantProcedure), false);

				raceData.ClueString = EditorGUILayout.TextField("Clue String", raceData.ClueString);

				raceData.allowedToChangeling =
					EditorGUILayout.Toggle("Allowed to Changeling", raceData.allowedToChangeling);

				raceData.CanBePlayerChosen = EditorGUILayout.Toggle("Can Be Player Chosen", raceData.CanBePlayerChosen);

				raceData.PreviewSprite = (SpriteDataSO)EditorGUILayout.ObjectField("Preview Sprite",
					raceData.PreviewSprite, typeof(SpriteDataSO), false);


				EditorGUILayout.LabelField("Health Systems", EditorStyles.boldLabel);

				EditorGUILayout.HelpBox("Health systems can only be edited within the PlayerHealthData ScriptableObject. Click the button below to open it.", MessageType.Info);
				if (GUILayout.Button("Open Player Health Data"))
				{
					Selection.activeObject = _currentData;
					EditorGUIUtility.PingObject(_currentData);
				}
				EditorGUILayout.EndVertical();
			}

			GUILayout.Space(20);

			showBodyParts = EditorGUILayout.Foldout(showBodyParts, "Body Parts");
			if (showBodyParts)
			{
				GUILayout.Label($"Every mob is composed of different limbs that include organs inside them. " +
				                $"You can generate all required limbs with the button bellow.", EditorStyles.helpBox);
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				// Button to generate variants for the body parts
				if (_bodyPartsBaseSO != null && GUILayout.Button("Generate Limbs", GUILayout.Height(30)))
					GenerateBodyPartVariants(raceData);

				raceData.Head = DrawObjectListField("Head", raceData.Head);
				GUILayout.Space(5);
				raceData.Torso = DrawObjectListField("Torso", raceData.Torso);
				GUILayout.Space(5);
				raceData.ArmRight = DrawObjectListField("Right Arm", raceData.ArmRight);
				GUILayout.Space(5);
				raceData.ArmLeft = DrawObjectListField("Left Arm", raceData.ArmLeft);
				GUILayout.Space(5);
				raceData.LegRight = DrawObjectListField("Right Leg", raceData.LegRight);
				GUILayout.Space(5);
				raceData.LegLeft = DrawObjectListField("Left Leg", raceData.LegLeft);
				EditorGUILayout.EndVertical();
			}

			GUILayout.Space(20);

			// Customisation Settings
			showCustomization = EditorGUILayout.Foldout(showCustomization, "Customization Settings");
			if (showCustomization)
			{
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				// Body Type Settings
				raceData.bodyTypeSettings = DrawBodyTypeSettings(raceData.bodyTypeSettings);
				GUILayout.Space(5);

				// Skin Colours
				showItems = EditorGUILayout.Foldout(showItems, "Skin Colors");
				if (showItems)
				{
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
						raceData.SkinColours[i] =
							EditorGUILayout.ColorField($"Skin Color {i + 1}", raceData.SkinColours[i]);
					}

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Body Parts That Share The Skin Tone", EditorStyles.boldLabel);
					if (GUILayout.Button("Add a body part to look for", GUILayout.Height(25)))
					{
						raceData.BodyPartsThatShareTheSkinTone.Add(null); // Add a new element to the list
					}
					EditorGUILayout.EndHorizontal();
					for (var i = 0; i < raceData.BodyPartsThatShareTheSkinTone.Count; i++)
					{
						EditorGUILayout.BeginHorizontal();
						raceData.BodyPartsThatShareTheSkinTone[i] = (BodyPart)EditorGUILayout.ObjectField($"Body Part {i + 1}",
							raceData.BodyPartsThatShareTheSkinTone[i], typeof(BodyPart), false);
						if (GUILayout.Button("Remove", GUILayout.Width(60)))
						{
							raceData.BodyPartsThatShareTheSkinTone.RemoveAt(i);
							break; // Exit loop to avoid modifying the list while iterating
						}
						EditorGUILayout.EndHorizontal();
					}

				}
				GUILayout.Space(5);
				foreach (var setting in raceData.CustomisationSettings)
				{
					EditorGUILayout.LabelField("Customisation Group", setting.CustomisationGroup.name);
					EditorGUILayout.LabelField("Blacklist:", EditorStyles.boldLabel);
					foreach (var blacklisted in setting.Blacklist)
					{
						EditorGUILayout.ObjectField(blacklisted, typeof(PlayerCustomisationData), false);
					}

				}
				EditorGUILayout.EndVertical();
			}

			GUILayout.Space(20);

			showItems = EditorGUILayout.Foldout(showItems, "Food");
			if (showItems)
			{
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				raceData.MeatProduce =
					(GameObject)EditorGUILayout.ObjectField("Meat Produce", raceData.MeatProduce, typeof(GameObject),
						false);
				raceData.SkinProduce =
					(GameObject)EditorGUILayout.ObjectField("Skin Produce", raceData.SkinProduce, typeof(GameObject),
						false);
				GUILayout.Space(5);
				raceData.SkinningItemTrait = (ItemTrait)EditorGUILayout.ObjectField("Skinning Item Trait",
					raceData.SkinningItemTrait, typeof(ItemTrait), false);
				EditorGUILayout.EndVertical();
			}

			if (RaceSOSingleton.Instance.Races.Contains(_currentData) == false)
			{
				GUILayout.Label($"This species is not present in the RaceSOSingleton. " +
				                $"It must be added inside that singleton for this race to function in-game.", EditorStyles.helpBox);
				if (GUILayout.Button("Add automatically.", GUILayout.Height(30)))
				{
					RaceSOSingleton.Instance.Races.Add(_currentData);
				}
			}
		}

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
			PrefabUtility.SaveAsPrefabAsset(instance, variantPath);
			DestroyImmediate(instance);

			return AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
		}

		private ObjectList DrawObjectListField(string label, ObjectList objectList)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
			if (GUILayout.Button("Add " + label, GUILayout.Height(15)))
			{
				objectList.Elements.Add(null);
			}
			EditorGUILayout.EndHorizontal();

			var count = Mathf.Max(0, EditorGUILayout.IntField("Number of Elements", objectList.Elements.Count));
			while (count < objectList.Elements.Count)
				objectList.Elements.RemoveAt(objectList.Elements.Count - 1);
			while (count > objectList.Elements.Count)
				objectList.Elements.Add(null);

			for (var i = 0; i < objectList.Elements.Count; i++)
				objectList.Elements[i] = (GameObject)EditorGUILayout.ObjectField($"Element {i + 1}",
					objectList.Elements[i], typeof(GameObject), false);

			return objectList;
		}
	}
}