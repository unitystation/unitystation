using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using Tilemaps.Scripts.Tiles;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ControlTabs : MonoBehaviour
	{
		private static ControlTabs controlTabs;
		public Button ItemListTab;

		private bool itemListTabExists;
		public Button moreTab;
		public Button optionsTab;
		public GameObject PanelItemList;
		public GameObject panelMore;
		public GameObject panelOptions;

		public GameObject panelStats;
		public Color selectedColor;
		public Button statsTab;

		public Color unselectColor;

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

		private void Start()
		{
			SelectWindow(WindowSelect.stats);
			itemListTabExists = false;
		}

		private void SelectWindow(WindowSelect winSelect)
		{
			switch (winSelect)
			{
				case WindowSelect.stats:
					UnselectAll();
					statsTab.image.color = selectedColor;
					panelStats.SetActive(true);
					break;
				case WindowSelect.itemList:
					UnselectAll();
					ItemListTab.image.color = selectedColor;
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
			ItemListTab.image.color = unselectColor;
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

		public void Button_Item_List()
		{
			SelectWindow(WindowSelect.itemList);
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

		/// <summary>
		///     Check if the Item List Tab needs to be updated or closed
		/// </summary>
		public static void CheckItemListTab()
		{
			if (!Instance.itemListTabExists)
			{
				return;
			}

			UITileList.UpdateItemPanelList();
			if (!PlayerManager.LocalPlayerScript.IsInReach(UITileList.GetListedItemsLocation()))
			{
				HideItemListTab();
			}
		}

		/// <summary>
		///     Displays the Objects tab
		/// </summary>
		/// <param name="objects">List of GameObjects to include in the Item List Tab</param>
		/// <param name="tile">Tile to include in the Item List Tab</param>
		/// <param name="position">Position of objects</param>
		public static void ShowItemListTab(IEnumerable<GameObject> objects, LayerTile tile, Vector3 position)
		{
			//If window exists, player is perhaps alt-clicking at another tile. Only slide tabs if Item List Tab doesn't already exist.
			if (Instance.itemListTabExists)
			{
				UITileList.ClearItemPanel();
			}
			else
			{
				SlideOptionsAndMoreTabs(Vector3.right);
			}

			UITileList.UpdateTileList(objects, tile, position);

			if (!UITileList.IsEmpty())
			{
				Instance.ItemListTab.GetComponentInChildren<Text>().text = tile ? tile.name : "Objects";
				Instance.ItemListTab.gameObject.SetActive(true);
				Instance.Button_Item_List();
				Instance.itemListTabExists = true;
			}
		}

		/// <summary>
		///     Hides the Item List Tab
		/// </summary>
		public static void HideItemListTab()
		{
			if (!Instance.itemListTabExists)
			{
				return;
			}

			SlideOptionsAndMoreTabs(Vector3.left);

			Instance.ItemListTab.gameObject.SetActive(false);
			Instance.Button_Stats();
			Instance.itemListTabExists = false;
			Instance.ItemListTab.GetComponentInChildren<Text>().text = "Objects";
			UITileList.ClearItemPanel();
		}

		//TODO: Perhaps implement a more robust solution that can arbitrarily insert tabs in any position? Would require refactor for tabs to be indexed.
		private static void SlideOptionsAndMoreTabs(Vector3 direction)
		{
			float width = Instance.ItemListTab.GetComponent<RectTransform>().rect.width;
			RectTransform optionsRect = Instance.optionsTab.GetComponent<RectTransform>();
			RectTransform moreRect = Instance.moreTab.GetComponent<RectTransform>();

			optionsRect.localPosition += direction * (width / 2f);
			moreRect.localPosition += direction * (width / 2f);
		}

		private enum WindowSelect
		{
			stats,
			itemList,
			options,
			more
		}
	}
}