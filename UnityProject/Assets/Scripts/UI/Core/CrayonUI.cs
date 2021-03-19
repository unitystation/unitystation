using System;
using System.Collections.Generic;
using Items.Tool;
using ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UI.Core
{
	public class CrayonUI : MonoBehaviour
	{
		[SerializeField]
		private GameObject dummyCategoryButton = null;

		[SerializeField]
		private GameObject dummyButton = null;

		[SerializeField]
		private GameObject categoryContent = null;

		[SerializeField]
		private GameObject tileContent = null;

		[SerializeField]
		private TMP_Dropdown colourDropDown = null;

		[SerializeField]
		private GraffitiCategoriesScriptableObject graffitiLists = null;

		[HideInInspector]
		public GameObject openingObject = null;

		private void OnEnable()
		{
			SetUpColour();

			SetUpCategories();
		}

		#region Categories

		private void SetUpCategories()
		{
			dummyCategoryButton.SetActive(true);

			for (int i = 0; i < graffitiLists.GraffitiTilesCategories.Count - 1; i++)
			{
				var newCategory = Instantiate(dummyCategoryButton, categoryContent.transform);

				var i1 = i;
				newCategory.GetComponent<Button>().onClick.AddListener(() => OnClickCategory(i1));
				newCategory.GetComponentInChildren<TMP_Text>().text = graffitiLists.GraffitiTilesCategories[i].name;
			}

			dummyCategoryButton.SetActive(false);

			LoadTiles(0);
		}

		private void OnClickCategory(int index)
		{
			//Clean old ones out
			foreach (Transform child in tileContent.transform)
			{
				GameObject.Destroy(child.gameObject);
			}

			LoadTiles(index);
		}

		#endregion

		#region Tiles

		private void LoadTiles(int categoryIndex)
		{
			dummyButton.SetActive(true);

			for (int i = 0; i < graffitiLists.GraffitiTilesCategories[categoryIndex].GraffitiTiles.Count - 1; i++)
			{
				var newCategory = Instantiate(dummyButton, tileContent.transform);

				var i1 = i;
				newCategory.GetComponent<Button>().onClick.AddListener(() => OnTileSelect(categoryIndex, i1));
				newCategory.GetComponent<Image>().sprite = graffitiLists.GraffitiTilesCategories[categoryIndex].GraffitiTiles[i].PreviewSprite;
				newCategory.GetComponent<Image>().color = GetCurrentColour();

				var name = graffitiLists.GraffitiTilesCategories[categoryIndex].GraffitiTiles[i].name;
				name.Replace("Graffiti", "");
				newCategory.GetComponentInChildren<TMP_Text>().text = name;
			}

			dummyButton.SetActive(false);
		}

		private void OnTileSelect(int categoryIndex, int index)
		{
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetCrayon(openingObject, (uint)categoryIndex, (uint)index, (uint)colourDropDown.value);
			gameObject.SetActive(false);
		}

		#endregion

		#region Colour

		private void SetUpColour()
		{
			colourDropDown.SetActive(true);

			var optionsData = new List<TMP_Dropdown.OptionData>();
			foreach (var colourName in Enum.GetNames(typeof(CrayonSprayCan.Colour)))
			{
				if(colourName == CrayonSprayCan.Colour.UnlimitedRainbow.ToString()) continue;

				optionsData.Add(new TMP_Dropdown.OptionData(colourName));
			}

			colourDropDown.options = optionsData;

			if (openingObject.TryGetComponent<CrayonSprayCan>(out var cr) && cr.IsCan == false)
			{
				colourDropDown.SetActive(false);
			}
		}

		public void OnColourChange()
		{
			foreach (Transform child in tileContent.transform)
			{
				child.GetComponent<Image>().color = GetCurrentColour();
			}
		}

		private Color GetCurrentColour()
		{
			if (openingObject.TryGetComponent<CrayonSprayCan>(out var cr) && cr.IsCan == false)
			{
				//This wont work if admin VVs, but eh
				return CrayonSprayCan.PickableColours[cr.SetColour];
			}

			Color colour = Color.black;

			if ((CrayonSprayCan.Colour) colourDropDown.value == CrayonSprayCan.Colour.UnlimitedRainbow)
			{
				//any random colour
				colour = new Color(Random.Range(0, 1), Random.Range(0, 1) , Random.Range(0, 1));
			}
			else if ((CrayonSprayCan.Colour)colourDropDown.value == CrayonSprayCan.Colour.NormalRainbow)
			{
				//random from set values
				colour = CrayonSprayCan.PickableColours.PickRandom().Value;
			}
			else
			{
				//chosen value
				colour = CrayonSprayCan.PickableColours[(CrayonSprayCan.Colour)colourDropDown.value];
			}

			return colour;
		}

		#endregion

		private void OnDisable()
		{
			//Clean up
			foreach (Transform child in categoryContent.transform)
			{
				GameObject.Destroy(child.gameObject);
			}

			foreach (Transform child in tileContent.transform)
			{
				GameObject.Destroy(child.gameObject);
			}
		}
	}
}
