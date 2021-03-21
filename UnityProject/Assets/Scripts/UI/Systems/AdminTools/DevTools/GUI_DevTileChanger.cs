using System;
using System.Collections.Generic;
using DatabaseAPI;
using ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.AdminTools.DevTools
{
	public class GUI_DevTileChanger : MonoBehaviour
	{
		[SerializeField]
		[Tooltip("Prefab that should be used for each category item")]
		private GameObject categoryButtonPrefab;
		[SerializeField]
		[Tooltip("content panel into which the list category items should be placed")]
		private GameObject categoryContentPanel;

		[Tooltip("Prefab that should be used for each tile item")]
		[SerializeField]
		private GameObject tileButtonPrefab;
		[SerializeField]
		[Tooltip("content panel into which the list items should be placed")]
		private GameObject tileContentPanel;
		[SerializeField]
		private InputField tileSearchBox;

		[Tooltip("If searchWhileTyping is turned on, don't start searching until at least this many" +
		         " characters are entered.")]
		[SerializeField]
		private int minCharactersForSearch = 3;

		[SerializeField]
		private TMP_Dropdown matrixDropdown = null;

		[SerializeField]
		private TMP_Text modeText= null;

		private bool isFocused;
		private int lastCategory = 0;
		private int categoryIndex = -1;
		private int tileIndex = -1;
		private int matrixIndex = 0;

		private Image selectedButton;
		private bool clickedUI;

		private ActionType currentAction = ActionType.None;

		public SortedDictionary<int, string> MatrixIds = new SortedDictionary<int, string>();

		private void OnEnable()
		{
			//request matrix ids
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdAskForMatrixIds(ServerData.UserID, PlayerList.Instance.AdminToken);

			SetUpCategories();

			modeText.text = currentAction.ToString();
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

			//Dont place tile if we clicked a UI button this frame
			if (clickedUI)
			{
				clickedUI = false;
				return;
			}

			OnClick();
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
			//Clean up old tiles
			foreach (Transform child in tileContentPanel.transform)
			{
				Destroy(child.gameObject);
			}

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
			categoryIndex = lastCategory;
			tileIndex = -1;

			LoadTiles(lastCategory);
		}

		private void OnClickCategory(int index)
		{
			clickedUI = true;

			//Clean old ones out
			foreach (Transform child in tileContentPanel.transform)
			{
				GameObject.Destroy(child.gameObject);
			}

			categoryIndex = index;
			LoadTiles(index);
		}

		#endregion

		#region Tiles

		private void LoadTiles(int categoryIndex)
		{
			tileButtonPrefab.SetActive(true);

			var tiles = TileCategorySO.Instance.TileCategories[categoryIndex].CommonTiles;
			tiles.AddRange(TileCategorySO.Instance.TileCategories[categoryIndex].Tiles);

			for (int i = 0; i < tiles.Count; i++)
			{
				var newTile = Instantiate(tileButtonPrefab, tileContentPanel.transform);

				var i1 = i;
				newTile.GetComponent<Button>().onClick.AddListener(() => OnTileSelect(categoryIndex, i1, newTile));
				newTile.GetComponentInChildren<Image>().sprite = tiles[i].PreviewSprite;
				newTile.GetComponentInChildren<TMP_Text>().text = tiles[i].name;
			}

			tileButtonPrefab.SetActive(false);
		}

		private void OnTileSelect(int newCategoryIndex, int newIndex, GameObject button)
		{
			clickedUI = true;
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

		public void SetUpMatrix()
		{
			var optionsData = new List<TMP_Dropdown.OptionData>();

			foreach (var matrix in MatrixIds)
			{
				optionsData.Add(new TMP_Dropdown.OptionData(matrix.Value));
			}

			matrixDropdown.options = optionsData;
			matrixDropdown.value = matrixIndex;
		}

		public void OnMatrixChange(int index)
		{
			//Sorted dictionary so it should be correct to get index from this dropdown directly
			matrixIndex = index;
		}

		#endregion

		#region ActionButtons

		public void OnActionButtonClick(int buttonType)
		{
			clickedUI = true;

			currentAction = (ActionType) buttonType;

			modeText.text = currentAction.ToString();
		}


		#endregion

		#region Clicking

		private void OnClick()
		{
			if(currentAction == ActionType.None) return;

			//Clicking once
			if (Input.GetMouseButtonDown(0))
			{
				switch (currentAction)
				{
					case ActionType.Place:
						//Also remove if shift is pressed when placing for quick remove
						if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
						{
							RemoveTile();
							return;
						}

						PlaceTile();
						return;
					case ActionType.Remove:
						RemoveTile();
						return;
				}
			}
		}

		#endregion

		private void PlaceTile()
		{
			if(categoryIndex == -1 || tileIndex == -1) return;

			//TODO detect wires and do custom add instead?

			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdPlaceTile(ServerData.UserID,
				PlayerList.Instance.AdminToken, categoryIndex, tileIndex, MouseInputController.MouseWorldPosition.RoundToInt(), matrixIndex);
		}

		private void RemoveTile()
		{
			if(categoryIndex == -1) return;

			var layerType = TileCategorySO.Instance.TileCategories[categoryIndex].LayerType;

			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRemoveTile(ServerData.UserID,
				PlayerList.Instance.AdminToken, MouseInputController.MouseWorldPosition.RoundToInt(), matrixIndex, layerType);
		}

		private enum ActionType
		{
			None,
			Place,
			Remove
		}
	}
}
