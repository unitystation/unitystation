using Construction;
using UnityEngine;

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

		//current object whose menu is being shown
		private BuildingMaterial currentBuildingMaterial;

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
			//delete previous results
			foreach (Transform child in contentPanel.transform)
			{
				Destroy(child.gameObject);
			}

			//display new results
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

		public void CloseBuildMenu()
		{
			SoundManager.Play("Click01");
			transform.GetChild(0).gameObject.SetActive(false);
		}

		//add a list item to the content panel for spawning the specified result
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