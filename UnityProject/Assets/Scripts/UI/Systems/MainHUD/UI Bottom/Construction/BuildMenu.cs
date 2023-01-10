using Construction;
using UnityEngine;
using Construction.Conveyors;
using TMPro;

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

		[SerializeField] private ConveyorBuildMenu conveyorBuildMenu = null;

		public ConveyorBuildMenu ConveyorBuildMenu => conveyorBuildMenu;

		// current object whose menu is being shown
		private BuildingMaterial currentBuildingMaterial;

		[Tooltip("The number of the specified item to make ")]
		[SerializeField] public TMP_InputField NumberInputField = null;


		//TODO: Implement, model kinda after dev spawner.

		/// <summary>
		/// Displays the menu for the buildingMaterial
		/// </summary>
		/// <param name="buildingMaterial"></param>
		public void ShowBuildMenu(BuildingMaterial buildingMaterial)
		{
			conveyorBuildMenu.gameObject.SetActive(false);
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
		}

		public void ShowConveyorBeltMenu(BuildList.Entry entry, BuildingMaterial buildingMaterial)
		{
			CloseBuildMenu();
			conveyorBuildMenu.OpenConveyorBuildMenu(entry, buildingMaterial);
		}

		public void ShowConveyorBeltMenu()
		{
			CloseBuildMenu();
			conveyorBuildMenu.OpenConveyorBuildMenu();
		}

		public void CloseBuildMenu()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			transform.GetChild(0).gameObject.SetActive(false);
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
