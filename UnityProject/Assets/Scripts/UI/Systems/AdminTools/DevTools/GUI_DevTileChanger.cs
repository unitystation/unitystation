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
		private TileCategorySO tileCategoryList = null;

		[SerializeField]
		private TMP_Dropdown matrixDropdown = null;

		private bool isFocused;
		private int lastCategory = 0;
		private int categoryIndex = -1;
		private int tileIndex = -1;
		private int matrixIndex = 0;

		private Image selectedButton;

		public SortedDictionary<int, string> MatrixIds = new SortedDictionary<int, string>();

		private void OnEnable()
		{
			//request matrix ids
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdAskForMatrixIds(ServerData.UserID, PlayerList.Instance.AdminToken);
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
		void Update()
		{
			if (tileSearchBox.isFocused && isFocused == false)
			{
				InputFocus();
			}
			else if (tileSearchBox.isFocused == false && isFocused)
			{
				InputUnfocus();
			}
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

			for (int i = 0; i < tileCategoryList.TileCategories.Count; i++)
			{
				var newCategory = Instantiate(categoryButtonPrefab, categoryContentPanel.transform);

				var i1 = i;
				newCategory.GetComponent<Button>().onClick.AddListener(() => OnClickCategory(i1));
				var categoryName = tileCategoryList.TileCategories[i].name;
				newCategory.GetComponentInChildren<TMP_Text>().text = categoryName.Replace("TileList", "");
			}

			categoryButtonPrefab.SetActive(false);

			LoadTiles(lastCategory);
		}

		private void OnClickCategory(int index)
		{
			//Clean old ones out
			foreach (Transform child in tileContentPanel.transform)
			{
				GameObject.Destroy(child.gameObject);
			}

			LoadTiles(index);
		}

		#endregion

		#region Tiles

		private void LoadTiles(int categoryIndex)
		{
			tileButtonPrefab.SetActive(true);

			var tiles = tileCategoryList.TileCategories[categoryIndex].CommonTiles;
			tiles.AddRange(tileCategoryList.TileCategories[categoryIndex].Tiles);

			for (int i = 0; i < tiles.Count; i++)
			{
				var newTile = Instantiate(tileButtonPrefab, tileContentPanel.transform);

				var i1 = i;
				newTile.GetComponent<Button>().onClick.AddListener(() => OnTileSelect(categoryIndex, i1, newTile));
				newTile.GetComponent<Image>().sprite = tiles[i].PreviewSprite;
				newTile.GetComponentInChildren<TMP_Text>().text = tiles[i].name;
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

			selectedButton = button.GetComponent<Image>();
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

		private void OnMatrixChange(int index)
		{
			//Sorted dictionary so it should be correct to get index from this dropdown directly
			matrixIndex = index;
		}

		#endregion

		private void PlaceTile()
		{
			if(categoryIndex == -1 || tileIndex == -1) return;

			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdPlaceTile(ServerData.UserID,
				PlayerList.Instance.AdminToken, categoryIndex, tileIndex, MouseInputController.MouseWorldPosition.RoundToInt(), matrixIndex);
		}
	}
}
