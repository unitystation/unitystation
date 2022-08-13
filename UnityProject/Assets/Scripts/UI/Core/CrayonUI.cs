using System;
using System.Collections.Generic;
using Core.Transforms;
using Items.Tool;
using ScriptableObjects;
using TMPro;
using UnityEngine;
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
		private TMP_Dropdown directionDropDown = null;

		[SerializeField]
		private GraffitiCategoriesScriptableObject graffitiLists = null;

		[HideInInspector]
		public GameObject openingObject = null;

		private int categoryIndex = -1;
		private int index = -1;

		private void Awake()
		{
			SetUpColour();

			SetUpDirection();
		}

		private void OnEnable()
		{
			if (openingObject == null)
			{
				gameObject.SetActive(false);
				return;
			}

			if (openingObject.TryGetComponent<CrayonSprayCan>(out var cr) && cr.IsCan == false)
			{
				colourDropDown.SetActive(false);
			}
			else
			{
				colourDropDown.SetActive(true);
			}

			SetUpCategories();
		}

		#region Categories

		private void SetUpCategories()
		{
			dummyCategoryButton.SetActive(true);

			for (int i = 0; i < graffitiLists.GraffitiTilesCategories.Count; i++)
			{
				var newCategory = Instantiate(dummyCategoryButton, categoryContent.transform);

				var i1 = i;
				newCategory.GetComponent<Button>().onClick.AddListener(() => OnClickCategory(i1));
				var categoryName = graffitiLists.GraffitiTilesCategories[i].name;
				newCategory.GetComponentInChildren<TMP_Text>().text = categoryName.Replace("FloorGraffiti", "");
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

			var currentDirection = GetDirection();

			for (int i = 0; i < graffitiLists.GraffitiTilesCategories[categoryIndex].GraffitiTiles.Count; i++)
			{
				var newCategory = Instantiate(dummyButton, tileContent.transform);

				var i1 = i;
				newCategory.GetComponent<Button>().onClick.AddListener(() => OnTileSelect(categoryIndex, i1));
				newCategory.GetComponent<Image>().sprite = graffitiLists.GraffitiTilesCategories[categoryIndex].GraffitiTiles[i].PreviewSprite;
				newCategory.GetComponent<Image>().color = GetCurrentColour();
				newCategory.transform.rotation = currentDirection;

				var overlayName = graffitiLists.GraffitiTilesCategories[categoryIndex].GraffitiTiles[i].name;
				newCategory.GetComponentInChildren<TMP_Text>().text = overlayName.Replace("Graffiti", "");
			}

			dummyButton.SetActive(false);
		}

		private void OnTileSelect(int newCategoryIndex, int newIndex)
		{
			if (openingObject == null)
			{
				gameObject.SetActive(false);
				return;
			}

			categoryIndex = newCategoryIndex;
			index = newIndex;

			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetCrayon(openingObject, (uint)categoryIndex, (uint)index, (uint)colourDropDown.value, (OrientationEnum)directionDropDown.value);
			gameObject.SetActive(false);
		}

		#endregion

		#region Colour

		private void SetUpColour()
		{
			var optionsData = new List<TMP_Dropdown.OptionData>();
			foreach (var colourName in Enum.GetNames(typeof(CrayonSprayCan.CrayonColour)))
			{
				if(colourName == CrayonSprayCan.CrayonColour.UnlimitedRainbow.ToString()) continue;

				optionsData.Add(new TMP_Dropdown.OptionData(colourName));
			}

			colourDropDown.options = optionsData;
		}

		public void OnColourChange()
		{
			foreach (Transform child in tileContent.transform)
			{
				child.GetComponent<Image>().color = GetCurrentColour();
			}

			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetCrayon(openingObject, (uint)categoryIndex, (uint)index, (uint)colourDropDown.value, (OrientationEnum)directionDropDown.value);
		}

		private Color GetCurrentColour()
		{
			//If this is a crayon it will use the set colour server side, this only affects client UI colours
			if (openingObject.TryGetComponent<CrayonSprayCan>(out var cr) && cr.IsCan == false)
			{
				if (cr.SetCrayonColour == CrayonSprayCan.CrayonColour.NormalRainbow)
				{
					//random from set values
					return CrayonSprayCan.PickableColours.PickRandom().Value;
				}

				if (cr.SetCrayonColour == CrayonSprayCan.CrayonColour.UnlimitedRainbow)
				{
					//random from any values
					return new Color(Random.Range(0, 1f), Random.Range(0, 1f) , Random.Range(0, 1f));
				}

				//This wont work if admin VVs, but eh
				return CrayonSprayCan.PickableColours[cr.SetCrayonColour];
			}

			//However for can these values will be sent to server, so cannot do CrayonColour.UnlimitedRainbow as it expects enum
			if ((CrayonSprayCan.CrayonColour)colourDropDown.value == CrayonSprayCan.CrayonColour.NormalRainbow)
			{
				//random from set values
				return CrayonSprayCan.PickableColours.PickRandom().Value;
			}

			//chosen value
			return CrayonSprayCan.PickableColours[(CrayonSprayCan.CrayonColour)colourDropDown.value];
		}

		#endregion

		#region Direction

		private void SetUpDirection()
		{
			var optionsData = new List<TMP_Dropdown.OptionData>();

			foreach (var orientation in Enum.GetNames(typeof(OrientationEnum)))
			{
				optionsData.Add(new TMP_Dropdown.OptionData(orientation));
			}

			directionDropDown.options = optionsData;

			//Make up default direction
			directionDropDown.value = 1;
		}

		public void OnDirectionChange()
		{
			if (openingObject == null)
			{
				gameObject.SetActive(false);
				return;
			}

			var currentDirection = GetDirection();

			foreach (Transform child in tileContent.transform)
			{
				child.rotation = currentDirection;
			}

			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetCrayon(openingObject, (uint)categoryIndex, (uint)index, (uint)colourDropDown.value, (OrientationEnum)directionDropDown.value);
		}

		private Quaternion GetDirection()
		{
			switch ((OrientationEnum)directionDropDown.value)
			{
				case OrientationEnum.Up_By0:
					return Quaternion.Euler(0f, 0f, 0);
				case OrientationEnum.Right_By270:
					return Quaternion.Euler(0f, 0f, 270f);
				case OrientationEnum.Left_By90:
					return Quaternion.Euler(0f, 0f, 90f);
				case OrientationEnum.Down_By180:
					return Quaternion.Euler(0f, 0f, 180f);
				default:
					return Quaternion.Euler(0f, 0f, 0);
			}
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
