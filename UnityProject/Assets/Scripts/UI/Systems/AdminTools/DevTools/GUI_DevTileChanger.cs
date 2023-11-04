using System;
using System.Collections.Generic;
using System.Linq;
using AdminCommands;
using DatabaseAPI;
using Logs;
using ScriptableObjects;
using TileManagement;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Systems.AdminTools.DevTools
{
	public class GUI_DevTileChanger : MonoBehaviour
	{
		[SerializeField]
		[Tooltip("Prefab that should be used for each category item")]
		private GameObject categoryButtonPrefab = null;
		[SerializeField]
		[Tooltip("content panel into which the list category items should be placed")]
		private GameObject categoryContentPanel = null;

		[Tooltip("Prefab that should be used for each tile item")]
		[SerializeField]
		private GameObject tileButtonPrefab = null;
		[SerializeField]
		[Tooltip("content panel into which the list items should be placed")]
		private GameObject tileContentPanel = null;

		[SerializeField]
		private InputField tileSearchBox = null;

		[SerializeField]
		private TMP_Dropdown matrixDropdown = null;

		[SerializeField]
		private TMP_Text modeText = null;

		[SerializeField]
		private TMP_Dropdown directionDropdown = null;

		[SerializeField]
		private Toggle colourToggle = null;

		[SerializeField]
		private ColorPicker colourPicker = null;

		private bool isFocused;
		private int categoryIndex = 0;
		private int tileIndex = -1;
		private int matrixIndex = 0;
		private int directionIndex = 0;

		private Image selectedButton;
		private LightingSystem lightingSystem;

		private const int MinCharactersForSearch = 1;

		private ActionType currentAction = ActionType.None;

		private Vector3Int dragStartPos;

		private void Awake()
		{
			lightingSystem = Camera.main.GetComponent<LightingSystem>();

			SetUpDirections();
		}

		private void OnEnable()
		{
			SetUpMatrix();

			SetUpCategories();

			modeText.text = currentAction.ToString();
			lightingSystem.enabled = false;
			UIManager.IsMouseInteractionDisabled = true;
		}

		private void OnDisable()
		{
			//Clean up
			foreach (Transform child in categoryContentPanel.transform)
			{
				Destroy(child.gameObject);
			}

			foreach (Transform child in tileContentPanel.transform)
			{
				Destroy(child.gameObject);
			}

			UIManager.IsMouseInteractionDisabled = false;
			lightingSystem.enabled = true;
		}

		public void Open()
		{
			gameObject.SetActive(true);
		}

		public void Close()
		{
			gameObject.SetActive(false);
		}

		/// <summary>
		/// There is no event for focusing input, so we must check for it manually in Update
		/// </summary>
		void LateUpdate()
		{
			if (tileSearchBox.isFocused && isFocused == false)
			{
				InputFocus();
			}
			else if (tileSearchBox.isFocused == false && isFocused)
			{
				InputUnfocus();
			}

			//Right click to stop placing
			if (CommonInput.GetMouseButtonDown(1))
			{
				OnActionButtonClick((int)ActionType.None);
				return;
			}

			//Ignore click if pointer is hovering over GUI
			if (EventSystem.current.IsPointerOverGameObject())
			{
				return;
			}

			//Single click
			if(OnClick()) return;

			OnDrag();
		}

		private void InputFocus()
		{
			//disable keyboard commands while input is focused
			isFocused = true;
			UIManager.IsInputFocus = true;
		}

		private void InputUnfocus()
		{
			//disable keyboard commands while input is focused
			isFocused = false;
			UIManager.IsInputFocus = false;
		}

		#region Categories

		private void SetUpCategories()
		{
			categoryButtonPrefab.SetActive(true);

			for (int i = 0; i < TileCategorySO.Instance.TileCategories.Count; i++)
			{
				var newCategory = Instantiate(categoryButtonPrefab, categoryContentPanel.transform);

				var i1 = i;
				newCategory.GetComponent<Button>().onClick.AddListener(() => OnClickCategory(i1));
				var categoryName = TileCategorySO.Instance.TileCategories[i].name;
				newCategory.GetComponentInChildren<TMP_Text>().text = categoryName.Replace("TileList", "");
			}

			categoryButtonPrefab.SetActive(false);
			tileIndex = -1;

			LoadTiles(categoryIndex);
		}

		private void OnClickCategory(int index)
		{
			//Clean old ones out
			foreach (Transform child in tileContentPanel.transform)
			{
				GameObject.Destroy(child.gameObject);
			}

			tileSearchBox.text = "";
			tileIndex = -1;

			categoryIndex = index;
			LoadTiles(index);
		}

		#endregion

		#region Tiles

		private void LoadTiles(int categoryIndex)
		{
			//Clean up old tiles
			foreach (Transform child in tileContentPanel.transform)
			{
				Destroy(child.gameObject);
			}

			tileButtonPrefab.SetActive(true);

			if (Enum.TryParse(directionDropdown.options[directionDropdown.value].text, out OrientationEnum value) == false)
			{
				Chat.AddExamineMsgToClient("Failed to find orientation!");
				return;
			}

			var rotation = Orientation.FromEnum(value).Degrees;

			for (int i = 0; i < TileCategorySO.Instance.TileCategories[categoryIndex].CombinedTileList.Count; i++)
			{
				var newTile = Instantiate(tileButtonPrefab, tileContentPanel.transform);

				var i1 = i;
				newTile.GetComponent<Button>().onClick.AddListener(() => OnTileSelect(categoryIndex, i1, newTile));

				var tile = TileCategorySO.Instance.TileCategories[categoryIndex].CombinedTileList[i];

				var image = newTile.GetComponentInChildren<Image>();
				image.sprite = tile.PreviewSprite;
				image.color = colourToggle.isOn ? colourPicker.CurrentColor : Color.white;

				var rect = image.GetComponent<RectTransform>();
				rect.localRotation = Quaternion.Euler(0, 0, -(rotation-90)); //Orientation angles are starting from the positive x axis going anti-clockwise (Like unit circle), but unity transforms start from positive y and go clockwise.

				newTile.GetComponentInChildren<TMP_Text>().text = tile.name;
			}

			tileButtonPrefab.SetActive(false);
		}

		private void OnTileSelect(int newCategoryIndex, int newIndex, GameObject button)
		{
			categoryIndex = newCategoryIndex;
			tileIndex = newIndex;

			//remove selection from old button if possible
			if (selectedButton != null)
			{
				selectedButton.color = Color.white;
			}

			selectedButton = button.GetComponentInChildren<Image>();
			selectedButton.color = Color.green;
		}

		#endregion

		#region Matrix

		private void SetUpMatrix()
		{
			var optionsData = new List<TMP_Dropdown.OptionData>();

			var stationId = MatrixManager.MainStationMatrix.Id;
			TMP_Dropdown.OptionData stationOption = null;

			foreach (var matrix in MatrixManager.Instance.ActiveMatrices)
			{
				var option = new TMP_Dropdown.OptionData(matrix.Value.Name);
				optionsData.Add(option);

				if(matrix.Key != stationId) continue;
				stationOption = option;
			}

			matrixDropdown.options = optionsData;
			matrixDropdown.value = stationOption!= null ? optionsData.IndexOf(stationOption) : matrixIndex;
		}

		public void OnMatrixChange()
		{
			//Sorted dictionary so it should be correct to get index from this dropdown directly
			matrixIndex = matrixDropdown.value;
		}

		#endregion

		#region ActionButtons

		public void OnActionButtonClick(int buttonType)
		{
			currentAction = (ActionType) buttonType;

			modeText.text = currentAction.ToString();
		}


		#endregion

		#region Clicking

		private bool OnClick()
		{
			if(currentAction == ActionType.None) return true;

			//Clicking once
			if (CommonInput.GetMouseButtonDown(0))
			{
				dragStartPos = MouseInputController.MouseWorldPosition.RoundToInt();

				switch (currentAction)
				{
					case ActionType.Place:
						//Also remove if shift is pressed when placing for quick remove
						if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
						{
							RemoveTile();
							return true;
						}

						PlaceTile();
						return true;
					case ActionType.Remove:
						//Also place if shift is pressed when placing for quick place
						if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
						{
							PlaceTile();
							return true;
						}

						RemoveTile();
						return true;
					case ActionType.Colour:
						ChangeColour();
						return true;
					default:
						Loggy.LogError($"Unknown case: {currentAction.ToString()} in switch!");
						return true;
				}
			}

			return false;
		}

		private void OnDrag()
		{
			if(currentAction == ActionType.None) return;

			//Being held down
			if (CommonInput.GetMouseButton(0)) return;

			if (CommonInput.GetMouseButtonUp(0) == false) return;

			if(dragStartPos == MouseInputController.MouseWorldPosition.RoundToInt()) return;

			//End drag
			switch (currentAction)
			{
				case ActionType.Place:
					//Also remove if shift is pressed when placing for quick remove
					if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
					{
						RemoveTile();
						break;
					}

					PlaceTile();
					break;
				case ActionType.Remove:
					//Also place if shift is pressed when placing for quick place
					if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
					{
						PlaceTile();
						break;
					}

					RemoveTile();
					break;
				case ActionType.Colour:
					ChangeColour();
					break;
				default:
					Loggy.LogError($"Unknown case: {currentAction.ToString()} in switch!");
					break;
			}
		}

		#endregion

		#region Directions

		private void SetUpDirections()
		{
			var optionsData = new List<TMP_Dropdown.OptionData>();

			foreach (OrientationEnum orientation in Enum.GetValues(typeof(OrientationEnum)))
			{
				optionsData.Add(new TMP_Dropdown.OptionData(orientation.ToString()));
			}

			directionDropdown.options = optionsData;

			//1 index is up by 0 degrees
			directionDropdown.value = 1;
			directionIndex = directionDropdown.value;
		}

		public void OnDirectionChange()
		{
			directionIndex = directionDropdown.value;

			if (Enum.TryParse(directionDropdown.options[directionIndex].text, out OrientationEnum value) == false)
			{
				Chat.AddExamineMsgToClient("Failed to find orientation!");
				return;
			}

			var rotation = Orientation.FromEnum(value).Degrees;

			foreach (Transform child in tileContentPanel.transform)
			{
				var image = child.GetComponentInChildren<Image>();
				var rect = image.GetComponent<RectTransform>();
				rect.localRotation = Quaternion.Euler(0, 0, rotation);
			}
		}

		#endregion

		#region Colour

		public void OnColourChange()
		{
			foreach (Transform child in tileContentPanel.transform)
			{
				var image = child.GetComponentInChildren<Image>();
				image.color = colourToggle.isOn ? colourPicker.CurrentColor : Color.white;
			}
		}

		#endregion

		#region Searching

		public void OnSearchBox()
		{
			var inputText = tileSearchBox.text.ToLower();

			if (inputText.Length < MinCharactersForSearch)
			{
				//Set all tiles active
				foreach (Transform child in tileContentPanel.transform)
				{
					child.SetActive(true);
				}

				return;
			}

			//Search for tile
			foreach (Transform child in tileContentPanel.transform)
			{
				var text = child.GetComponentInChildren<TMP_Text>().text.ToLower();

				child.SetActive(text.Contains(inputText));
			}
		}

		#endregion

		private void PlaceTile()
		{
			if(categoryIndex == -1 || tileIndex == -1) return;

			if (Enum.TryParse(directionDropdown.options[directionIndex].text, out OrientationEnum orientation) == false)
			{
				Chat.AddExamineMsgToClient("Failed to find orientation!");
				return;
			}

			var matrixId =
				MatrixManager.Instance.ActiveMatrices.Where(x =>
					x.Value.Name == matrixDropdown.options[matrixIndex].text).ToList();

			if (matrixId.Any() == false)
			{
				Chat.AddExamineMsgToClient("Invalid matrix selected!");
				return;
			}

			Color? colour = colourToggle.isOn ? colourPicker.CurrentColor : null;

			var startPos = MouseInputController.MouseWorldPosition.RoundToInt();

			var data = new PlaceStruct
			{
				categoryIndex = categoryIndex,
				tileIndex = tileIndex,
				startWorldPosition = startPos,
				endWorldPosition = dragStartPos,
				matrixId = matrixId.First().Key,
				orientation = orientation,
				colour = colour
			};

			AdminCommandsManager.Instance.CmdPlaceTile(data);
		}

		private void RemoveTile()
		{
			if(categoryIndex == -1) return;

			var matrixId =
				MatrixManager.Instance.ActiveMatrices.Where(x =>
					x.Value.Name == matrixDropdown.options[matrixIndex].text).ToList();

			if (matrixId.Any() == false)
			{
				Chat.AddExamineMsgToClient("Invalid matrix selected!");
				return;
			}

			var category = TileCategorySO.Instance.TileCategories[categoryIndex];

			var startPos = MouseInputController.MouseWorldPosition.RoundToInt();

			var data = new RemoveStruct
			{
				categoryIndex = categoryIndex,
				startWorldPosition = startPos,
				endWorldPosition = dragStartPos,
				matrixId = matrixId.First().Key,
				layerType = category.LayerType,
				overlayType = category.OverlayType
			};

			AdminCommandsManager.Instance.CmdRemoveTile(data);
		}

		private void ChangeColour()
		{
			if(categoryIndex == -1) return;

			var matrixId =
				MatrixManager.Instance.ActiveMatrices.Where(x =>
					x.Value.Name == matrixDropdown.options[matrixIndex].text).ToList();

			if (matrixId.Any() == false)
			{
				Chat.AddExamineMsgToClient("Invalid matrix selected!");
				return;
			}

			Color? colour = colourToggle.isOn ? colourPicker.CurrentColor : null;

			var startPos = MouseInputController.MouseWorldPosition.RoundToInt();

			var data = new ColourStruct
			{
				categoryIndex = categoryIndex,
				startWorldPosition = startPos,
				endWorldPosition = dragStartPos,
				matrixId = matrixId.First().Key,
				colour = colour
			};

			AdminCommandsManager.Instance.CmdColourTile(data);
		}

		public struct PlaceStruct
		{
			public int categoryIndex;
			public int tileIndex;
			public Vector3Int startWorldPosition;
			public Vector3Int endWorldPosition;
			public int matrixId;
			public OrientationEnum orientation;
			public Color? colour;
		}

		public struct RemoveStruct
		{
			public int categoryIndex;
			public Vector3Int startWorldPosition;
			public Vector3Int endWorldPosition;
			public int matrixId;
			public LayerType layerType;
			public OverlayType overlayType;
		}

		public struct ColourStruct
		{
			public int categoryIndex;
			public Vector3Int startWorldPosition;
			public Vector3Int endWorldPosition;
			public int matrixId;
			public Color? colour;
		}

		private enum ActionType
		{
			None,
			Place,
			Remove,
			Colour
		}
	}
}
