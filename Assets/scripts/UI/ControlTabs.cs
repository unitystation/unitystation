using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayGroup;

namespace UI
{
	public class ControlTabs : MonoBehaviour
	{
		public Button statsTab;
		public Button objectsTab;
		public Button optionsTab;
		public Button moreTab;

		public GameObject panelStats;
		public GameObject panelObjects;
		public GameObject panelOptions;
		public GameObject panelMore;

		public Color unselectColor;
		public Color selectedColor;

		private bool objectWindowExists;

		private enum WindowSelect
		{
			stats,
			objects,
			options,
			more
		}

		private static ControlTabs controlTabs;

		public static ControlTabs Instance
		{
			get
			{
				if (!controlTabs)
				{
					controlTabs = FindObjectOfType<ControlTabs>();
				}

				return controlTabs;
			}
		}

		void Start()
		{
			SelectWindow(WindowSelect.stats);
			objectWindowExists = false;
		}

		private void SelectWindow(WindowSelect winSelect)
		{
			switch (winSelect) {
				case WindowSelect.stats:
					UnselectAll();
					statsTab.image.color = selectedColor;
					panelStats.SetActive(true);
					break;
				case WindowSelect.objects:
					UnselectAll();
					objectsTab.image.color = selectedColor;
					break;
				case WindowSelect.options:
					UnselectAll();
					optionsTab.image.color = selectedColor;
					panelOptions.SetActive(true);
					break;
				case WindowSelect.more:
					UnselectAll();
					moreTab.image.color = selectedColor;
					panelMore.SetActive(true);
					break;
			}
		}

		private void UnselectAll()
		{
			statsTab.image.color = unselectColor;
			objectsTab.image.color = unselectColor;
			optionsTab.image.color = unselectColor;
			moreTab.image.color = unselectColor;
			panelStats.SetActive(false);
			panelOptions.SetActive(false);
			panelMore.SetActive(false);
		}

		public void Button_Stats()
		{
			SelectWindow(WindowSelect.stats);
			SoundManager.Play("Click01");
		}

		public void Button_Objects()
		{
			SelectWindow(WindowSelect.objects);
			SoundManager.Play("Click01");
		}

		public void Button_Options()
		{ 
			SelectWindow(WindowSelect.options);
			SoundManager.Play("Click01");
		}

		public void Button_More()
		{
			SelectWindow(WindowSelect.more);
			SoundManager.Play("Click01");
		}

		public static void CheckItemListTab()
		{
			if (!Instance.objectWindowExists){
				return;
			}

			UITileList.UpdateItemPanelList();

			if(!PlayerManager.LocalPlayerScript.IsInReach(UITileList.GetListedItemsLocation())) {
				HideItemListTab();
			}
		}

		/// <summary>
		/// Displays the Objects tab
		/// </summary>
		/// <param name="tiles">List of GameObjects to include in the objects tab</param>
		public static void ShowItemListTab(List<GameObject> tiles)
		{
			//If window exists, player is perhaps alt-clicking at another tile. Only slide tabs if object window doesn't already exist.
			if(Instance.objectWindowExists) {
				UITileList.ClearItemPanel();
			} else {
				SlideOptionsAndMoreTabs(Vector3.right);
			}

			foreach (GameObject tile in tiles) {
				UITileList.AddObjectToItemPanel(tile);

				//TODO re-implement for new tile system
				if(tile.GetComponent<FloorTile>()) {
					Instance.objectsTab.GetComponentInChildren<Text>().text = tile.name;
				}
			}

			Instance.objectsTab.gameObject.SetActive(true);
			Instance.Button_Objects();
			Instance.objectWindowExists = true;
		}

		/// <summary>
		/// Hides the Objects tab
		/// </summary>
		public static void HideItemListTab()
		{
			if (!Instance.objectWindowExists)
			{
				return;
			}

			SlideOptionsAndMoreTabs(Vector3.left);

			Instance.objectsTab.gameObject.SetActive(false);
			Instance.Button_Stats();
			Instance.objectWindowExists = false;
			Instance.objectsTab.GetComponentInChildren<Text>().text = "Objects";
			UITileList.ClearItemPanel();
		}

		//TODO: Perhaps implement a more robust solution that can arbitrarily insert tabs in any position? Would require refactor for tabs to be indexed.
		private static void SlideOptionsAndMoreTabs(Vector3 direction)
		{
			float width = Instance.objectsTab.GetComponent<RectTransform>().rect.width;
			RectTransform optionsRect = Instance.optionsTab.GetComponent<RectTransform>();
			RectTransform moreRect = Instance.moreTab.GetComponent<RectTransform>();

			optionsRect.localPosition += direction * (width / 2f);
			moreRect.localPosition += direction * (width / 2f);
		}
	}
}
