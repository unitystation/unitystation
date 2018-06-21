using System.Collections;
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

		private static GameObject FingerPrefab;
		private static GameObject TabHeaderPrefab;

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

		private bool itemListTabExists => ClientTabs.ContainsKey( ClientTabType.ItemList ) && !ClientTabs[ClientTabType.ItemList].Hidden;

		private TabHeaderButton[] Headers => HeaderStorage.GetComponentsInChildren<TabHeaderButton>();

		private List<Tab> ActiveTabs {
			get {
				var list = new List<Tab>();
				var tabs = TabStorage.GetComponentsInChildren<Tab>( true );
				for ( var i = 0; i < tabs.Length; i++ ) {
					Tab tab = tabs[i];
					if ( !tab.Hidden ) {
						list.Add( tab );
					}
				}
				return list;
			}
		} //^^^vvv can be generified to something with a Func, but I'm feeling lazy
		private List<Tab> HiddenTabs {
			get {
				var list = new List<Tab>();
				var tabs = TabStorage.GetComponentsInChildren<Tab>( true );
				for ( var i = 0; i < tabs.Length; i++ ) {
					Tab tab = tabs[i];
					if ( tab.Hidden ) {
						list.Add( tab );
					}
				}
				return list;
			}
		}

		///Includes hidden (inactive) ones
		private Dictionary<ClientTabType, ClientTab> ClientTabs {
			get {
				var toReturn = new Dictionary<ClientTabType, ClientTab>();
				var foundTabs = TabStorage.GetComponentsInChildren<ClientTab>(true);
				for ( int i = 0; i < foundTabs.Length; i++ ) {
					toReturn.Add(foundTabs[i].Type, foundTabs[i]);
				}
				return toReturn;
			}
		}

		public Dictionary<NetTabDescriptor, NetTab> HiddenNetTabs {
			get {
				var netTabs = new Dictionary<NetTabDescriptor, NetTab>();
				for ( var i = 0; i < HiddenTabs.Count; i++ ) {
					NetTab tab = HiddenTabs[i] as NetTab;
					if ( tab != null ) {
						netTabs.Add( tab.NetTabDescriptor, tab );
					}
				}
				return netTabs;
			}
		}

		public Dictionary<NetTabDescriptor, NetTab> OpenedNetTabs {
			get {
				var netTabs = new Dictionary<NetTabDescriptor, NetTab>();
				for ( var i = 0; i < ActiveTabs.Count; i++ ) {
					NetTab tab = ActiveTabs[i] as NetTab;
					if ( tab != null ) {
						netTabs.Add( tab.NetTabDescriptor, tab );
					}
				}
				return netTabs;
			}
		}

		private void Start() {
			HeaderStorage = transform.GetChild( 0 );
			TabStorage = transform.GetChild( 1 );
			FingerPrefab = Resources.Load<GameObject>( "PokeFinger" );
			TabHeaderPrefab = Resources.Load<GameObject>( "TabHeader" );
			RefreshTabHeaders();
			Instance.HideTab(ClientTabType.ItemList);

			SelectTab(ClientTabType.Stats);
		}

		//for every non-hidden tab: create new header and set its index value according to the tab index.
		//select header if tab is active
		private void RefreshTabHeaders() {
			var approvedHeaders = new HashSet<TabHeaderButton>();

			for ( var i = 0; i < ActiveTabs.Count; i++ ) {
				Tab tab = ActiveTabs[i];

				var headerButton = HeaderForTab( tab );
				if ( !headerButton ) {
					headerButton = Instantiate( TabHeaderPrefab, HeaderStorage, false ).GetComponent<TabHeaderButton>();
				}

				headerButton.Value = tab.transform.GetSiblingIndex();
				string tabName = tab.name.Replace( "Tab", "" ).Replace( "_", " " ).Trim();
				headerButton.gameObject.name = tabName;
				headerButton.GetComponentInChildren<Text>().text = tabName;

				SetHeaderPosition( headerButton.gameObject, i );
				
				approvedHeaders.Add( headerButton );
			}
			
			//Killing extra headers
			for ( var i = 0; i < Headers.Length; i++ ) {
				var header = Headers[i];
				if ( !approvedHeaders.Contains( header ) ) {
					Destroy( header.gameObject );
				}
			}
		}

		private TabHeaderButton HeaderForTab( Tab tab ) {
			if ( tab.Hidden ) {
//				Debug.LogWarning( $"Tab {tab} is hidden, no header will be provided" );
				return null;
			}
			for ( var i = 0; i < Headers.Length; i++ ) {
				var header = Headers[i];
				if ( header.Value == tab.transform.GetSiblingIndex() ) {
					return header;
				}
			}
//			Debug.LogError( $"No headers found for {tab}, wtf?" );
			return null;
		}

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
			Tab tab = TabStorage.GetChild( index )?.GetComponent<Tab>();
			if ( !tab ) {
				Debug.LogWarning( $"No tab found with index {index}!" );
				return;
			}
			tab.gameObject.SetActive( true );
			HeaderForTab(tab).Select();

			if ( click ) {
				SoundManager.Play("Click01");		
			}
		}

		private void UnselectAll() {
			for ( var i = 0; i < ActiveTabs.Count; i++ ) {
				var tab = ActiveTabs[i];
				HeaderForTab( tab ).Unselect();
				tab.gameObject.SetActive( false );
			}
		}

		private void UnhideTab( Tab tab ) {
			tab.Hidden = false;
			RefreshTabHeaders();
			SelectTab( tab.gameObject );
		}

		private void UnhideTab( ClientTabType type ) {
			if ( ClientTabs.ContainsKey( type ) ) {
				UnhideTab(ClientTabs[type]);
			}
		}

		private void HideTab( Tab tab ) {
			tab.Hidden = true;
			tab.gameObject.SetActive( false );
			RefreshTabHeaders();
			SelectTab( ClientTabType.Stats, false );
		}

		private void HideTab( ClientTabType type ) {
			if ( ClientTabs.ContainsKey( type ) ) {
				HideTab( ClientTabs[type] );
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
			var tab = Instance.ClientTabs[ClientTabType.ItemList];
			
			if ( !UITileList.Instance ) {
				UITileList.Instance = tab.GetComponentsInChildren<UITileList>( true )[0];
			}
			
			//If window exists, player is perhaps alt-clicking at another tile. Only slide tabs if Item List Tab doesn't already exist.
			if ( !Instance.itemListTabExists ) {
				Instance.UnhideTab( ClientTabType.ItemList );
			}

			UITileList.ClearItemPanel();
			UITileList.UpdateTileList(objects, tile, position);

			if (!UITileList.IsEmpty()) {
				tab.GetComponentInChildren<Text>().text = tile ? tile.name : "Objects";
				Instance.SelectTab( ClientTabType.ItemList );
			}
		}
		
		///     Check if the Item List Tab needs to be updated or closed
		public static void CheckItemListTab()
		{
			if (!Instance.itemListTabExists){
				return;
			}
			if (!PlayerManager.LocalPlayerScript || !PlayerManager.LocalPlayerScript.IsInReach(UITileList.GetListedItemsLocation())){
				Instance.HideTab(ClientTabType.ItemList);
				return;
			}
			UITileList.UpdateItemPanelList();
		}

		public static void ShowTab( NetTabType type, GameObject tabProvider, ElementValue[] elementValues ) {
			var openedTab = new NetTabDescriptor( tabProvider, type );
			//try to dig out a hidden tab with matching parameters and enable it:
			if ( Instance.HiddenNetTabs.ContainsKey( openedTab ) ) {
//				Debug.Log( $"Yay, found an old hidden {openedTab} tab. Unhiding it" );
				Instance.UnhideTab( Instance.HiddenNetTabs[openedTab] );
			}
			if ( !Instance.OpenedNetTabs.ContainsKey( openedTab ) ) {
				NetTab tabInfo = openedTab.Spawn();
				GameObject tabObject = tabInfo.gameObject;

				//putting into the right place
				var rightPanelParent = UIManager.Instance.GetComponentInChildren<ControlTabs>().transform.GetChild( 1 );
				tabObject.transform.SetParent(rightPanelParent);
				tabObject.transform.localScale = Vector3.one;
				var rect = tabObject.GetComponent<RectTransform>();
				rect.offsetMin = new Vector2(15, 15);
				rect.offsetMax = -new Vector2(15, 50);
				
				Instance.RefreshTabHeaders();
			}

			NetTab tab = Instance.OpenedNetTabs[openedTab];
			tab.ImportValues( elementValues );
			Instance.SelectTab( tab.gameObject, false );
		}

		public static void CloseTab( NetTabType type, GameObject tabProvider ) {
			var tabDesc = new NetTabDescriptor( tabProvider, type );
			if ( Instance.OpenedNetTabs.ContainsKey( tabDesc ) ) {
				var openedTab = Instance.OpenedNetTabs[tabDesc];
				Instance.HideTab( openedTab );
			}
		}

		public static void UpdateTab( NetTabType type, GameObject tabProvider, ElementValue[] values, bool touched = false ) {
			var lookupTab = new NetTabDescriptor(tabProvider, type);
			if ( Instance.OpenedNetTabs.ContainsKey( lookupTab ) ) {
				var tabInfo = Instance.OpenedNetTabs[lookupTab];

				var touchedElement = tabInfo.ImportValues( values );

				if ( touched && touchedElement != null ) {
					Instance.ShowFinger( tabInfo.gameObject, touchedElement.gameObject );
				}
			}
		}
		
		///for client.
		///Close tabs if you're out of interaction radius
		public static void CheckTabClose() {
			var toClose = new List<NetTab>();
			var playerScript = PlayerManager.LocalPlayerScript;
			
			foreach ( NetTab tab in Instance.OpenedNetTabs.Values ) {
				if (playerScript.canNotInteract() || !playerScript.IsInReach( tab.Provider ))
				{
					toClose.Add( tab );
				}
			}
			foreach ( NetTab tab in toClose ) {
				Instance.HideTab( tab );
			}

			CheckItemListTab();
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
				Destroy( finger ); //could probably pool and deactivate and whatever instead
			} else {
				Instance.StartCoroutine( FingerDecay( finger ) );
			}
		}

	}
}