using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using Tilemaps.Tiles;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UI
{
	public class OpenedTab
	{
		public GameObject Provider;
		public TabType Type;
		public GameObject Reference;
	}

	public class ControlTabs : MonoBehaviour
	{
		private static ControlTabs controlTabs;
		public Button ItemListTab;

		private bool itemListTabExists;
		public Button moreTab;
		public Button optionsTab;
		//public GameObject panelItemList;
		public GameObject panelMore;
		public GameObject panelOptions;

		public GameObject panelStats;
		public Color selectedColor;
		public Button statsTab;

		public Color unselectColor;

		public Dictionary<NetworkTab, NetworkTabInfo> openedTabs = new Dictionary<NetworkTab, NetworkTabInfo>();
		private static GameObject FingerPrefab;

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

		private void Start() {
			FingerPrefab = Resources.Load<GameObject>( "PokeFinger" );
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

		//TODO: Perhaps implement a more robust solution that can arbitrarily insert tabs in any position? Would require refactor for tabs to be indexed.
		private static void SlideOptionsAndMoreTabs(Vector3 direction)
		{
			float width = Instance.ItemListTab.GetComponent<RectTransform>().rect.width;
			RectTransform optionsRect = Instance.optionsTab.GetComponent<RectTransform>();
			RectTransform moreRect = Instance.moreTab.GetComponent<RectTransform>();

			optionsRect.localPosition += direction * (width/* / 2f*/);
			moreRect.localPosition += direction * (width/* / 2f*/);
		}
	public static void ShowTab( TabType type, GameObject tabProvider, ElementValue[] elementValues ) {
			var openedTab = new NetworkTab( tabProvider, type );
			if ( !Instance.openedTabs.ContainsKey( openedTab ) ) {
				NetworkTabInfo tabInfo = openedTab.Spawn();
				GameObject tabObject = tabInfo.Reference;

				//putting into the right place
				var rightPanelParent = UIManager.Instance.GetComponentInChildren<ControlTabs>().transform.GetChild( 1 );
				tabObject.transform.SetParent(rightPanelParent);
				tabObject.transform.localScale = Vector3.one;
				var rect = tabObject.GetComponent<RectTransform>();
				rect.offsetMin = new Vector2(15, 15);
				rect.offsetMax = -new Vector2(15, 50);
				
				tabObject.SetActive( true );
				
				Instance.UnselectAll();
				
				tabInfo.ImportValues( elementValues );
				
				Instance.openedTabs.Add( openedTab, tabInfo );
			}
		}

		public static void HideTab( TabType type, GameObject tabProvider ) {
			var tabDesc = new NetworkTab( tabProvider, type );
			if ( Instance.openedTabs.ContainsKey( tabDesc ) ) {
			// consider deactivating?
				GameObject openedTab = Instance.openedTabs[tabDesc].Reference;
				Destroy(openedTab);
				Instance.openedTabs.Remove( tabDesc );
				Instance.SelectWindow( WindowSelect.stats );
			}
		}

		public static void UpdateTab( TabType type, GameObject tabProvider, ElementValue[] values, bool touched = false ) {
			var lookupTab = new NetworkTab(tabProvider, type);
			if ( Instance.openedTabs.ContainsKey( lookupTab ) ) {
				var tabInfo = Instance.openedTabs[lookupTab];
				
				tabInfo.ImportValues( values );
				
				for ( var i = 0; i < values.Length; i++ ) {
					string elementId = values[i].Id;
					string value = values[i].Value;
					var netElement = tabInfo[elementId];
					netElement.Value = value; //FIXME: NRE when another player touches self delete button 
					if ( touched ) {
						Instance.ShowFinger( tabInfo.Reference, netElement.gameObject );
					}
				}
			}
		}

		public static void UpdateTab( TabType type, GameObject tabProvider, string elementId, string value, bool touched = false ) {
			UpdateTab( type, tabProvider, new[] {new ElementValue {Id = elementId, Value = value}}, touched );
		}
		
		private void ShowFinger( GameObject tab, GameObject element ) {
			GameObject fingerObject = Instantiate( FingerPrefab, tab.transform );
			fingerObject.transform.position =
				element.transform.position + new Vector3( Random.Range( -5f, 5f ), Random.Range( -5f, 5f ), 0 );
			Instance.StartCoroutine(FingerDecay(fingerObject.GetComponent<Image>()));
		}

		private IEnumerator FingerDecay( Image finger ) {
			yield return new WaitForSeconds( 0.05f );
			var c = finger.color;
			finger.color = new Color( c.r, c.g, c.b, Mathf.Clamp( c.a - 0.03f, 0, 1 ) );
			if ( finger.color.a <= 0 ) {
				Destroy( finger );
			} else {
				Instance.StartCoroutine( FingerDecay( finger ) );
			}
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