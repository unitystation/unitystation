using System.Collections;
using System.Collections.Generic;
using PlayGroup;
using Tilemaps.Tiles;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ControlTabs : MonoBehaviour
	{
		private static ControlTabs controlTabs;

		private bool itemListTabExists => ClientTabs.ContainsKey( ClientTabType.itemList ) && ClientTabs[ClientTabType.itemList].activeInHierarchy;

		public Color selectedColor;
		public Color unselectColor;
		
		public List<GameObject> TabHeaders {
			get {
				var headers = new List<GameObject>();
				for ( int i = 0; i < HeaderStorage.childCount; i++ ) {
					headers.Add(HeaderStorage.GetChild( i ).gameObject);
				}
				return headers;
			}
		}
		///Includes hidden (inactive) ones
		private Dictionary<ClientTabType, GameObject> ClientTabs {
			get {
				var toReturn = new Dictionary<ClientTabType, GameObject>();
				var foundTabs = TabStorage.GetComponentsInChildren<ClientTab>(true);
				for ( int i = 0; i < foundTabs.Length; i++ ) {
					toReturn.Add(foundTabs[i].Type, foundTabs[i].gameObject);
				}
				return toReturn;
			}
		}

		private List<KeyValuePair<Button, GameObject>> AllTabs {
			get {
				var list = new List<KeyValuePair<Button, GameObject>>();
				int headerCount = HeaderStorage.childCount;
				int tabCount = TabStorage.childCount;
				if ( headerCount != tabCount ) {
					Debug.LogError( $"There are {tabCount} tabs and {headerCount} tab headers, ouch" );
					return list;
				}
				for ( int i = 0; i < headerCount; i++ ) {
					list.Add( new KeyValuePair<Button, GameObject>(
						HeaderStorage.GetChild( i )?.gameObject.GetComponent<Button>(),
						TabStorage.GetChild( i )?.gameObject
					) );
				}
				return list;
			}
		}

		private KeyValuePair<Button, GameObject> HeaderTabPair(int index) {
			return new KeyValuePair<Button, GameObject>( 
				HeaderStorage.GetChild( index )?.GetComponent<Button>(),
				TabStorage.GetChild( index )?.gameObject
			);
		}

		public Dictionary<NetworkTab, NetworkTabInfo> OpenedNetTabs = new Dictionary<NetworkTab, NetworkTabInfo>();

		private static GameObject FingerPrefab;

		public static GameObject TabHeaderPrefab;

		private Transform HeaderStorage;

		private Transform TabStorage;

		public static ControlTabs Instance {
			get {
				if ( !controlTabs ) {
					controlTabs = FindObjectOfType<ControlTabs>();
				}
				return controlTabs;
			}
		}

		private void Start() {
			HeaderStorage = transform.GetChild( 0 );
			TabStorage = transform.GetChild( 1 );
			FingerPrefab = Resources.Load<GameObject>( "PokeFinger" );
			TabHeaderPrefab = Resources.Load<GameObject>( "TabHeader" );
			RefreshTabHeaders();
			Instance.HideClientTab(ClientTabType.itemList);

			SelectTab(ClientTabType.stats);
		}

		private void RefreshTabHeaders() {
			ClearTabHeaders();
			for ( int i = 0; i < TabStorage.childCount; i++ ) {
				var foundTab = TabStorage.GetChild( i ).gameObject;
				var newTabHeader = Instantiate( TabHeaderPrefab, HeaderStorage, false );
				string tabName = foundTab.name.Replace( "PANEL_", "" );
				newTabHeader.name = tabName;
				newTabHeader.GetComponentInChildren<Text>().text = tabName;
				SetHeaderPosition( newTabHeader, i );
			}
		}

		private void ClearTabHeaders() { //fixme: headers stay undestroyed and things go crazy
			for ( var i = 0; i < TabHeaders.Count; i++ ) {
				var tabHeader = TabHeaders[i];
				Destroy( tabHeader ); //.SetActive( false );
			}
		}

//		/// Need to run this on tab count change to ensure no gaps are present
//		public void RefreshHeaderPositions() {
//			for ( var i = 0; i < TabHeaders.Count; i++ ) {
//				SetHeaderPosition( TabHeaders[i], i );
//			}
//		}

		private void SetHeaderPosition( GameObject header, int index = 0 ) {
			RectTransform rect = header.GetComponent<RectTransform>();
			rect.anchoredPosition = Vector3.right * rect.rect.width * index;
		}

		///Find client tab of given type and get its index
		private void SelectTab( ClientTabType type, bool click = true)
		{
			if ( ClientTabs.ContainsKey( type ) ) {
				int index = ClientTabs[type].gameObject.transform.GetSiblingIndex();
				SelectTab( index, click );
			}
		}

		private void SelectTab( GameObject tabObject, bool click = true ) {
			SelectTab( tabObject.transform.GetSiblingIndex(), click );
		}

		/// This one is called when tab header is clicked on
		public void SelectTab( int index, bool click = true ) 
		{
			Debug.Log( $"Selecting tab #{index}" );
			
			UnselectAll();
			var tab = HeaderTabPair( index );

			tab.Key.image.color = selectedColor;
			tab.Value.SetActive( true );

			if ( click ) {
				SoundManager.Play("Click01");		
			}
		}

		private void UnselectAll() {
			for ( var i = 0; i < AllTabs.Count; i++ ) {
				var tabPair = AllTabs[i];
				tabPair.Key.image.color = unselectColor;
				tabPair.Value.SetActive( false );
			}
		}

		///     Check if the Item List Tab needs to be updated or closed
		public static void CheckItemListTab()
		{
			if (!Instance.itemListTabExists)
			{
				return;
			}
			if (!PlayerManager.LocalPlayerScript || !PlayerManager.LocalPlayerScript.IsInReach(UITileList.GetListedItemsLocation()))
			{
				Instance.HideClientTab(ClientTabType.itemList);
				return;
			}
			UITileList.UpdateItemPanelList();
		}

		private void UnhideClientTab( ClientTabType type ) {
			if ( ClientTabs.ContainsKey( type ) ) {
				ClientTabs[type].gameObject.SetActive( true );
			}
			RefreshTabHeaders();
			SelectTab( type );
		}

		private void HideClientTab( ClientTabType type ) {
			if ( ClientTabs.ContainsKey( type ) ) {
				ClientTabs[type].gameObject.SetActive( false );
			}
			RefreshTabHeaders();
			SelectTab( 0, false );
//			UITileList.ClearItemPanel();
		}

		/// <summary>
		///     Displays the Objects tab
		/// </summary>
		/// <param name="objects">List of GameObjects to include in the Item List Tab</param>
		/// <param name="tile">Tile to include in the Item List Tab</param>
		/// <param name="position">Position of objects</param>
		public static void ShowItemListTab(IEnumerable<GameObject> objects, LayerTile tile, Vector3 position)
		{ //fixme: does weird things
//			var tab = Instance.ClientTabs[ClientTabType.itemList];
//			
//			if ( !UITileList.Instance ) {
//				UITileList.Instance = tab.GetComponentsInChildren<UITileList>( true )[0];
//			}
//			
//			//If window exists, player is perhaps alt-clicking at another tile. Only slide tabs if Item List Tab doesn't already exist.
//			if (Instance.itemListTabExists){
//				UITileList.ClearItemPanel();
//			} else {
//				Instance.UnhideClientTab( ClientTabType.itemList );
////				SlideOptionsAndMoreTabs(Vector3.right);
//			}
//
//			UITileList.UpdateTileList(objects, tile, position);
//
//			if (!UITileList.IsEmpty()) {
//				tab.GetComponentInChildren<Text>().text = tile ? tile.name : "Objects";
//				Instance.SelectTab( ClientTabType.itemList );
//			}
		}

//		private static void SlideOptionsAndMoreTabs(Vector3 direction)
//		{
//			float width = Instance.ItemListTab.GetComponent<RectTransform>().rect.width;
//			RectTransform optionsRect = Instance.optionsTab.GetComponent<RectTransform>();
//			RectTransform moreRect = Instance.moreTab.GetComponent<RectTransform>();

//			optionsRect.localPosition += direction * (width/* / 2f*/);
//			moreRect.localPosition += direction * (width/* / 2f*/);
//		}


		public static void ShowTab( TabType type, GameObject tabProvider, ElementValue[] elementValues ) {
			var openedTab = new NetworkTab( tabProvider, type );
			if ( !Instance.OpenedNetTabs.ContainsKey( openedTab ) ) {
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
				Instance.RefreshTabHeaders();
				Instance.SelectTab( tabObject, false );
				
				tabInfo.ImportValues( elementValues );
				
				Instance.OpenedNetTabs.Add( openedTab, tabInfo );
			}
		}

		public static void CloseTab( TabType type, GameObject tabProvider ) {
			var tabDesc = new NetworkTab( tabProvider, type );
			if ( Instance.OpenedNetTabs.ContainsKey( tabDesc ) ) {
			// consider deactivating?
				GameObject openedTab = Instance.OpenedNetTabs[tabDesc].Reference;
				Destroy(openedTab);
				Instance.OpenedNetTabs.Remove( tabDesc );
				Instance.RefreshTabHeaders();
				Instance.SelectTab( 0 );
			}
		}

		public static void UpdateTab( TabType type, GameObject tabProvider, ElementValue[] values, bool touched = false ) {
			var lookupTab = new NetworkTab(tabProvider, type);
			if ( Instance.OpenedNetTabs.ContainsKey( lookupTab ) ) {
				var tabInfo = Instance.OpenedNetTabs[lookupTab];

				var touchedElement = tabInfo.ImportValues( values );

				if ( touched && touchedElement != null ) {
					Instance.ShowFinger( tabInfo.Reference, touchedElement.gameObject );
				}
			}
		}

		private void ShowFinger( GameObject tab, GameObject element ) {
			GameObject fingerObject = Instantiate( FingerPrefab, tab.transform );
			fingerObject.transform.position =
				element.transform.position + new Vector3( Random.Range( -5f, 5f ), Random.Range( -5f, 5f ), 0 ); //todo randomize V3 extension method
			Instance.StartCoroutine(FingerDecay(fingerObject.GetComponent<Image>()));
		}

		private IEnumerator FingerDecay( Image finger ) {
			yield return new WaitForSeconds( 0.05f );
			var c = finger.color;
			finger.color = new Color( c.r, c.g, c.b, Mathf.Clamp( c.a - 0.03f, 0, 1 ) );
			if ( finger.color.a <= 0 ) {
				Destroy( finger ); //could probably pool and deactivate and whatever instead
			} else {
				Instance.StartCoroutine( FingerDecay( finger ) );
			}
		}

	}
}