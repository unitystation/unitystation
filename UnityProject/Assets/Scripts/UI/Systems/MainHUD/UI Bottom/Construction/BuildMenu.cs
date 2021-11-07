using Construction;
using UnityEngine;
using Construction.Conveyors;

namespace UI.UI_Bottom
{
	/// <summary>
	/// Main logic for build menu UI, showing the player a list of things that can be built
	/// </summary>
	public class BuildMenu : MonoBehaviour
	{
		[Tooltip("Prefab that should be used for each list item")]
		[SerializeField]
		private GameObject listItemPrefab = null;

		[Tooltip("content panel into which the list items should be placed")]
		[SerializeField] private GameObject contentPanel = null;

		[Tooltip("back button to return to main menu")]
		[SerializeField] private GameObject backButton = null;

		[SerializeField] private ConveyorBuildMenu conveyorBuildMenu = null;

		// current object whose menu is being shown
		private BuildingMaterial currentBuildingMaterial;
		//default build list
		private BuildList mainMenu;

		//TODO: Implement, model kinda after dev spawner.

		/// <summary>
		/// Displays the menu for the buildingMaterial
		/// </summary>
		/// <param name="buildingMaterial"></param>
		public void ShowBuildMenu(BuildingMaterial buildingMaterial, BuildList buildListToSave = null)
		{
			conveyorBuildMenu.gameObject.SetActive(false);
			backButton.gameObject.SetActive(false);
			transform.GetChild(0).gameObject.SetActive(true);
			currentBuildingMaterial = buildingMaterial;
			// delete previous results
			foreach (Transform child in contentPanel.transform)
			{
				Destroy(child.gameObject);
			}

			// display new results
			foreach (var entry in buildingMaterial.BuildList.Entries)
			{
				CreateListItem(entry);
			}
			//save main menu
			if (buildListToSave)
			{
				mainMenu = buildListToSave;
				backButton.gameObject.SetActive(true);
			}
			else if (mainMenu != null && mainMenu != buildingMaterial.BuildList)
			{
				backButton.gameObject.SetActive(true);
			}
		}

		public void ShowConveyorBeltMenu(BuildList.Entry entry, BuildingMaterial buildingMaterial)
		{
			CloseBuildMenu();
			conveyorBuildMenu.OpenConveyorBuildMenu(entry, buildingMaterial);
		}

		public void CloseBuildMenu()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			transform.GetChild(0).gameObject.SetActive(false);
		}

		/// <summary>
		/// Displays the default build list for the buildingMaterial. Used by button.
		/// </summary>
		public void ReturnToMainMenu()
		{
			currentBuildingMaterial.BuildList = mainMenu;
			ShowBuildMenu(currentBuildingMaterial);
		}

		// add a list item to the content panel for spawning the specified result
		private void CreateListItem(BuildList.Entry entry)
		{
			if (!entry.CanBuildWith(currentBuildingMaterial)) return;

			GameObject listItem = Instantiate(listItemPrefab);
			listItem.GetComponent<BuildMenuEntryController>().Initialize(entry, currentBuildingMaterial);
			listItem.transform.SetParent(contentPanel.transform);
			listItem.transform.localScale = Vector3.one;
		}
	}
}
