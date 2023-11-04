using System;
using Construction;
using Logs;
using Messages.Client;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UI_Bottom
{
	/// <summary>
	/// Main logic for an entry in the build menu.
	/// </summary>
	public class BuildMenuEntryController : MonoBehaviour
	{
		[Tooltip("Main image of the object to build")]
		[SerializeField]
		private Image image = null;

		[Tooltip("Secondary image of the object to build")]
		[SerializeField]
		private Image secondaryImage = null;

		[Tooltip("Tertiary image of the object to build")] //example: airlocks
		[SerializeField]
		private Image tertiaryImage = null;

		[Tooltip("Name describing what this entry is.")]
		[SerializeField]
		private Text entryName = null;

		[Tooltip("Amount of material required for this entry.")]
		[SerializeField]
		private Text materialCost = null;

		[Tooltip("Image of the material required.")]
		[SerializeField]
		private Image materialImage = null;

		[Tooltip("Secondary image of the material required.")]
		[SerializeField]
		private Image materialSecondaryImage = null;

		//menu and entry this entry is for
		private BuildingMaterial buildingMaterial;
		private BuildList.Entry entry;

		private void Awake()
		{
			image.enabled = false;
			secondaryImage.enabled = false;
			tertiaryImage.enabled = false;
			materialImage.enabled = false;
			materialSecondaryImage.enabled = false;
		}

		/// <summary>
		/// Initialize this entry using the specified building list enty
		/// </summary>
		/// <param name="entry">entry whose contest should be displayed</param>
		/// <param name="buildingMaterial">buildingMaterial this entry comes from</param>
		public void Initialize(BuildList.Entry entry, BuildingMaterial buildingMaterial)
		{
			this.entry = entry;
			this.buildingMaterial = buildingMaterial;
			image.sprite = null;
			secondaryImage.sprite = null;
			tertiaryImage.sprite = null;
			materialImage.sprite = null;
			materialSecondaryImage.sprite = null;
			entryName.text = null;
			materialCost.text = null;

			if (entry.Prefab)
			{
				entry.Prefab.PopulateImageSprites(image, secondaryImage, tertiaryImage);
			}
			else
			{
				Loggy.LogError($"Construction Entry {entry.Name} doesn't use prefab", Category.Construction);
			}

			entryName.text = entry.Name;

			buildingMaterial.gameObject.PopulateImageSprites(materialImage, materialSecondaryImage);

			materialCost.text = entry.Cost.ToString();
		}

		public void OnClick()
		{
			//Show the conveyor belt build menu
			if (entry.Name.Equals("Conveyor Belt"))
			{
				UIManager.BuildMenu.ShowConveyorBeltMenu(entry, buildingMaterial);
				return;
			}


			if (int.TryParse(UIManager.BuildMenu.NumberInputField.text, out var numberWanted) == false)
			{
				numberWanted = 1;
				UIManager.BuildMenu.NumberInputField.text = numberWanted.ToString();
			}


			RequestBuildMessage.Send(entry, buildingMaterial, numberWanted);
			UIManager.BuildMenu.CloseBuildMenu();
		}
	}
}