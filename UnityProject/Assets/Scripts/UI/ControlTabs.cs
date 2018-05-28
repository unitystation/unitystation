using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using Tilemaps.Tiles;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ControlTabs : MonoBehaviour
	{
		private static ControlTabs controlTabs;
		public Button ItemListTab;

		private bool itemListTabExists;
		private bool shuttleTabExists;
		public Button moreTab;
		public Button optionsTab;
		public Button ShuttleControlTab;
		public GameObject PanelItemList;
		
		public GameObject panelMore;
		public GameObject panelOptions;
		public GameObject panelStats;
		public GameObject panelShuttle;

		public ShuttleUIControl ShuttleControl;
		
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
			shuttleTabExists = false;
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
				case WindowSelect.shuttleControl:
					UnselectAll();
					moreTab.image.color = selectedColor;
					panelShuttle.SetActive(true);
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
			panelShuttle.SetActive(false);
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

		public void Button_Shuttle_Control()
		{
			SelectWindow(WindowSelect.shuttleControl);
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
			if (!PlayerManager.LocalPlayerScript || !PlayerManager.LocalPlayerScript.IsInReach(UITileList.GetListedItemsLocation()))
			{
				HideItemListTab();
				return;
			}
			UITileList.UpdateItemPanelList();
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

		/// <summary>
		/// Shows shuttle control tab and gives the shuttle UI component a reference to the matrix move that is going to be controlled.
		/// </summary>
		/// <param name="shuttleMatrixMove">A reference to a matrix move component</param>
		public static void ShowShuttleTab(MatrixMove shuttleMatrixMove)
		{
			if (!Instance.shuttleTabExists)
			{
				Instance.ShuttleControl.matrixMove = shuttleMatrixMove;
				SlideOptionsAndMoreTabs(Vector3.right);
				Instance.ShuttleControlTab.gameObject.SetActive(true);
				Instance.Button_Shuttle_Control();
				Instance.shuttleTabExists = true;
			}
			
			
		}

		/// <summary>
		/// Hides the shuttle control tab.
		/// </summary>
		public static void HideShuttleTab()
		{
			if (!Instance.shuttleTabExists)
			{
				return;
			}
			
			Instance.ShuttleControl.matrixMove = null;
			SlideOptionsAndMoreTabs(Vector3.left);
			Instance.ShuttleControlTab.gameObject.SetActive(false);
			Instance.Button_Stats();
			Instance.shuttleTabExists = false;
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
			more,
			shuttleControl
		}
	}
}