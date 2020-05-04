using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ControlTabs : MonoBehaviour
{
	private static GameObject FingerPrefab;
	private static GameObject TabHeaderPrefab;
	public Transform TabStorage;
	public Transform TabStoragePopOut; //For popout windows
	public Transform HeaderStorage;
	public Transform rolloutIcon;
	public Canvas canvas;

	public bool rolledOut { get; private set; }
	private bool preventRoll = false;

	private static ControlTabs controlTabs;
	private Tab[] tabsCache;
	private Tab[] popoutTabsCache;

	public static ControlTabs Instance
	{
		get
		{
			if (controlTabs == null)
			{
				controlTabs = FindObjectOfType<ControlTabs>();
			}
			return controlTabs;
		}
	}
	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnLevelFinishedLoading;
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnLevelFinishedLoading;
	}

	private void OnLevelFinishedLoading(Scene oldScene, Scene newScene)
	{
		Instance.ResetControlTabs();
	}

	private void ResetControlTabs()
	{
		foreach (var tab in Instance.HiddenNetTabs)
		{
			Destroy(Instance.HeaderForTab(tab.Value)?.gameObject);
			Destroy(tab.Value.gameObject);
		}
		foreach (var tab in Instance.OpenedNetTabs)
		{
			Destroy(Instance.HeaderForTab(tab.Value)?.gameObject);
			Destroy(tab.Value.gameObject);
		}
		Instance.SelectTab(ClientTabType.Stats, false);
	}

	private bool itemListTabExists => ClientTabs.ContainsKey(ClientTabType.ItemList) && !ClientTabs[ClientTabType.ItemList].Hidden;

	private TabHeaderButton[] Headers => HeaderStorage.GetComponentsInChildren<TabHeaderButton>();

	private List<Tab> ActiveTabs
	{
		get
		{
			if (tabsCache == null)
			{
				tabsCache = TabStorage.GetComponentsInChildren<Tab>(true);
			}

			if (popoutTabsCache == null)
			{
				popoutTabsCache = TabStoragePopOut.GetComponentsInChildren<Tab>(true);
			}

			var list = new List<Tab>(tabsCache.Length + popoutTabsCache.Length);
			for (var i = 0; i < tabsCache?.Length; i++)
			{
				Tab tab = tabsCache[i];
				if (!tab.Hidden)
				{
					list.Add(tab);
				}
			}
			for (var i = 0; i < popoutTabsCache?.Length; i++)
			{
				Tab tab = popoutTabsCache[i];
				if (!tab.Hidden)
				{
					list.Add(tab);
				}
			}
			return list;
		}
	} //^^^vvv can be generified to something with a Func, but I'm feeling lazy
	private List<Tab> HiddenTabs
	{
		get
		{
			var list = new List<Tab>();

			if (tabsCache == null)
			{
				tabsCache = TabStorage.GetComponentsInChildren<Tab>(true);
			}

			if (popoutTabsCache == null)
			{
				popoutTabsCache = TabStoragePopOut.GetComponentsInChildren<Tab>(true);
			}

			for (var i = 0; i < tabsCache?.Length; i++)
			{
				Tab tab = tabsCache[i];
				if (tab.Hidden)
				{
					list.Add(tab);
				}
			}
			for (var i = 0; i < popoutTabsCache?.Length; i++)
			{
				Tab tab = popoutTabsCache[i];
				if (tab.Hidden)
				{
					list.Add(tab);
				}
			}
			return list;
		}
	}

	///Includes hidden (inactive) ones
	private Dictionary<ClientTabType, ClientTab> ClientTabs
	{
		get
		{
			var toReturn = new Dictionary<ClientTabType, ClientTab>();
			var foundTabs = TabStorage.GetComponentsInChildren<ClientTab>(true);
			for (int i = 0; i < foundTabs?.Length; i++)
			{
				toReturn.Add(foundTabs[i].Type, foundTabs[i]);
			}
			foundTabs = TabStoragePopOut.GetComponentsInChildren<ClientTab>(true);
			for (int i = 0; i < foundTabs?.Length; i++)
			{
				toReturn.Add(foundTabs[i].Type, foundTabs[i]);
			}
			return toReturn;
		}
	}

	private Dictionary<NetTabDescriptor, NetTab> HiddenNetTabs
	{
		get
		{
			var netTabs = new Dictionary<NetTabDescriptor, NetTab>();
			var hiddenTabs = HiddenTabs;
			for (var i = 0; i < hiddenTabs.Count; i++)
			{
				NetTab tab = hiddenTabs[i] as NetTab;
				if (tab != null)
				{
					netTabs.Add(tab.NetTabDescriptor, tab);
				}
			}
			return netTabs;
		}
	}

	private Dictionary<NetTabDescriptor, NetTab> OpenedNetTabs
	{
		get
		{
			var activeTabs = ActiveTabs;
			var netTabs = new Dictionary<NetTabDescriptor, NetTab>(activeTabs.Count);
			for (var i = 0; i < activeTabs.Count; i++)
			{
				NetTab tab = activeTabs[i] as NetTab;
				if (tab != null)
				{
					netTabs.Add(tab.NetTabDescriptor, tab);
				}
			}
			return netTabs;
		}
	}

	void Awake()
	{
		FingerPrefab = Resources.Load<GameObject>("PokeFinger");
		TabHeaderPrefab = Resources.Load<GameObject>("HeaderTab");
	}

	private void Start()
	{

		RefreshTabHeaders();
		Instance.HideTab(ClientTabType.ItemList);
		Instance.HideTab(ClientTabType.Admin);

		SelectTab(ClientTabType.Stats);
	}

	private void Update()
	{
		tabsCache = null;
		popoutTabsCache = null;
	}

	/// <summary>
	/// Show admin panel if this client has been flagged as admin by the server
	/// </summary>
	public void ToggleOnAdminTab()
	{
		Instance.UnhideTab(ClientTabType.Admin);
		Instance.SelectTab(ClientTabType.Stats);
	}

	//for every non-hidden tab: create new header and set its index value according to the tab index.
	//select header if tab is active
	private void RefreshTabHeaders()
	{
		var approvedHeaders = new HashSet<TabHeaderButton>();

		var activeTabs = ActiveTabs;
		for (var i = 0; i < activeTabs.Count; i++)
		{
			Tab tab = activeTabs[i];

			if (tab.transform.parent != TabStorage)
			{
				//Skip pop outs etc
				continue;
			}

			var headerButton = HeaderForTab(tab);
			if (!headerButton)
			{
				headerButton = Instantiate(TabHeaderPrefab, HeaderStorage, false).GetComponent<TabHeaderButton>();
			}

			headerButton.Value = tab.transform.GetSiblingIndex();
			string tabName = tab.name.Replace("Tab", "").Replace("_", " ").Replace("(Clone)", "").Trim();
			headerButton.gameObject.name = tabName;
			headerButton.GetComponentInChildren<Text>().text = tabName;

			approvedHeaders.Add(headerButton);
		}

		//Killing extra headers
		var headers = Headers;
		for (var i = 0; i < headers.Length; i++)
		{
			var header = headers[i];
			if (!approvedHeaders.Contains(header))
			{
				Destroy(header.gameObject);
			}
		}
	}

	private TabHeaderButton HeaderForTab(Tab tab)
	{
		if (tab.Hidden)
		{
			//				Logger.LogWarning( $"Tab {tab} is hidden, no header will be provided" );
			return null;
		}

		var headers = Headers;
		for (var i = 0; i < headers.Length; i++)
		{
			var header = headers[i];
			if (header.Value == tab.transform.GetSiblingIndex())
			{
				return header;
			}
		}
		//			Logger.LogError( $"No headers found for {tab}, wtf?" );
		return null;
	}

	private int GetHeaderOffset(int tabIndex)
	{
		int width = 0;
		var activeTabs = ActiveTabs;
		for (var i = 0; i < tabIndex; i++)
		{
			var header = HeaderForTab(activeTabs[i]);
			width += (int)((RectTransform)header.gameObject.transform).rect.width;
		}
		return width;
	}

	///Find client tab of given type and get its index
	private void SelectTab(ClientTabType type, bool click = true)
	{
		if (ClientTabs.ContainsKey(type))
		{
			int index = ClientTabs[type].gameObject.transform.GetSiblingIndex();
			SelectTab(index, click);
		}
	}

	private void SelectTab(GameObject tabObject, bool click = true)
	{
		SelectTab(tabObject.transform.GetSiblingIndex(), click);
	}

	/// This one is called when tab header is clicked on
	public void SelectTab(int index, bool click = true)
	{
		//			Logger.Log( $"Selecting tab #{index}" );
		UnselectAll();
		Tab tab = TabStorage.GetChild(index)?.GetComponent<Tab>();

		if (!tab)
		{
			Logger.LogWarning($"No tab found with index {index}!", Category.NetUI);
			return;
		}
		tab.gameObject.SetActive(true);
		HeaderForTab(tab)?.Select();

		if (click)
		{
			SoundManager.Play("Click01");
		}
	}

	private void UnselectAll()
	{
		for (var i = 0; i < ActiveTabs.Count; i++)
		{
			var tab = ActiveTabs[i];
			if (tab.isPopOut)
			{
				//Do not disable pop outs
				continue;
			}
			HeaderForTab(tab)?.Unselect();
			tab.gameObject.SetActive(false);
		}
	}

	private void UnhideTab(Tab tab)
	{
		tab.Hidden = false;
		if (!tab.isPopOut)
		{
			RefreshTabHeaders();
			SelectTab(tab.gameObject);
		}
		else
		{
			tab.gameObject.SetActive(true);
			var localPos = Vector3.zero;
			localPos.y += 20f;
			tab.transform.localPosition = localPos;
		}
	}

	private void UnhideTab(ClientTabType type)
	{
		if (ClientTabs.ContainsKey(type))
		{
			UnhideTab(ClientTabs[type]);
		}
	}

	private void HideTab(Tab tab)
	{
		tab.Hidden = true;
		bool wasActive = tab.gameObject.activeSelf;
		tab.gameObject.SetActive(false);
		RefreshTabHeaders();
		if(wasActive)
		{
			SelectTab(ClientTabType.Stats, false);
			var netTab = tab as NetTab;
			if (rolledOut && (netTab == null || !netTab.isPopOut))
			{
				CloseTabWindow();
			}
		}
	}

	private void HideTab(ClientTabType type)
	{
		if (ClientTabs.ContainsKey(type))
		{
			HideTab(ClientTabs[type]);
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

		if (!UITileList.Instance)
		{
			UITileList.Instance = tab.GetComponentsInChildren<UITileList>(true)[0];
		}

		if (!Instance.itemListTabExists)
		{
			Instance.UnhideTab(ClientTabType.ItemList);
		}

		UITileList.ClearItemPanel();
		UITileList.UpdateTileList(objects, tile, position);

		if (!UITileList.IsEmpty())
		{
			Instance.SelectTab(ClientTabType.ItemList);
		}
		if (!Instance.rolledOut)
		{
			Instance.OpenTabWindow();
		}
	}

	///     Check if the Item List Tab needs to be updated or closed
	public static void CheckItemListTab()
	{
		if (!Instance.itemListTabExists)
		{
			return;
		}
		if (!PlayerManager.LocalPlayerScript || !PlayerManager.LocalPlayerScript.IsInReach(UITileList.GetListedItemsLocation(), false))
		{
			Instance.HideTab(ClientTabType.ItemList);
			return;
		}
		UITileList.UpdateItemPanelList();
	}

	public static void ShowTab(NetTabType type, GameObject tabProvider, ElementValue[] elementValues)
	{
		if (tabProvider == null)
		{
			return;
		}

		// Need to spawn a local instance of the NetTab to obtain its isPopOut property first. Parent is changed later.
		var tabDescriptor = new NetTabDescriptor(tabProvider, type);
		NetTab tab = FindOrSpawn( tabProvider, tabDescriptor );

		//Make use of NetTab's fancy isPopOut bool instead of depending on the NetTabType.
		bool isPopOut = tab.isPopOut;


		if (!Instance.rolledOut && !isPopOut)
		{
			Instance.OpenTabWindow();
		}

		//try to dig out a hidden tab with matching parameters and enable it:
		var hiddenNetTabs = Instance.HiddenNetTabs;
		if (hiddenNetTabs.ContainsKey(tabDescriptor))
		{
			//				Logger.Log( $"Yay, found an old hidden {openedTab} tab. Unhiding it" );
			Instance.UnhideTab(hiddenNetTabs[tabDescriptor]);
		}

		if (!Instance.OpenedNetTabs.ContainsKey(tabDescriptor))
		{
			Transform newParent = !isPopOut ? Instance.TabStorage : Instance.TabStoragePopOut;
			tab.transform.SetParent(newParent, false);
			GameObject tabObject = tab.gameObject;

			//putting into the right place
			tabObject.transform.localScale = Vector3.one;
			var rect = tabObject.GetComponent<RectTransform>();

			if (!isPopOut)
			{
				rect.offsetMin = new Vector2(15, 15);
				rect.offsetMax = -new Vector2(15, 50);
			}
			else
			{
				//Center it:
				var localPos = Vector3.zero;
				localPos.y += 20f;
				rect.transform.localPosition = localPos;
				tab.isPopOut = true;
			}

			Instance.RefreshTabHeaders();
		}

		tab.ImportValues(elementValues);
		if (!isPopOut)
		{
			Instance.SelectTab(tab.gameObject, false);
		}
	}

	private static NetTab FindOrSpawn( GameObject tabProvider, NetTabDescriptor descriptor )
	{
		var hiddenNetTabs = Instance.HiddenNetTabs;
		if ( hiddenNetTabs.ContainsKey( descriptor ) )
		{
			return hiddenNetTabs[descriptor];
		}
		var openedNetTabs = Instance.OpenedNetTabs;
		if ( openedNetTabs.ContainsKey( descriptor ) )
		{
			return openedNetTabs[descriptor];
		}

		return descriptor.Spawn(tabProvider.transform);
	}

	public static void CloseTab(NetTabType type, GameObject tabProvider)
	{
		var tabDesc = new NetTabDescriptor(tabProvider, type);
		if (Instance.OpenedNetTabs.ContainsKey(tabDesc))
		{
			var openedTab = Instance.OpenedNetTabs[tabDesc];
			Instance.HideTab(openedTab);
		}
	}

	public static void UpdateTab(NetTabType type, GameObject tabProvider, ElementValue[] values, bool touched = false)
	{
		var lookupTab = new NetTabDescriptor(tabProvider, type);
		var openedNetTabs = Instance.OpenedNetTabs;

		if (!openedNetTabs.ContainsKey(lookupTab))
		{
			return;
		}

		var tabInfo = openedNetTabs[lookupTab];
		var touchedElement = tabInfo.ImportValues(values);
		if (touched && touchedElement != null)
		{
			Instance.ShowFinger(tabInfo.gameObject, touchedElement.gameObject);
		}
	}

	public static void RefreshTabs()
	{
		var activeTabs = Instance.ActiveTabs;
		for (int i = 0; i < activeTabs.Count; i++)
		{
			activeTabs[i].RefreshTab();
		}
	}

	///for client.
	///Close tabs if you're out of interaction radius
	public static void CheckTabClose()
	{
		var toClose = new List<NetTab>();
		var toDestroy = new List<NetTab>();
		var playerScript = PlayerManager.LocalPlayerScript;

		foreach (NetTab tab in Instance.OpenedNetTabs.Values)
		{

			if ( tab.Provider == null )
			{
				toDestroy.Add(tab);
			}
			else if ( !Validations.CanApply(PlayerManager.LocalPlayerScript, tab.Provider, NetworkSide.Client))
			{
				//Make sure the item is not in the players hands first:
				if (UIManager.Hands.CurrentSlot.Item != tab.Provider.gameObject &&
					UIManager.Hands.OtherSlot.Item != tab.Provider.gameObject)
				{
					toClose.Add(tab);
				}
			}
		}

		foreach (NetTab tab in toClose)
		{
			Instance.HideTab(tab);
		}

		foreach ( NetTab tab in toDestroy )
		{
			Instance.DestroyTab( tab );
		}

		CheckItemListTab();
	}

	private void DestroyTab( NetTab tab )
	{
		HideTab( tab );
		Destroy( tab.gameObject );
	}

	private void ShowFinger(GameObject tab, GameObject element)
	{
		GameObject fingerObject = Instantiate(FingerPrefab, tab.transform);
		fingerObject.transform.position =
			element.transform.position + new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
		Instance.StartCoroutine(FingerDecay(fingerObject.GetComponent<Image>()));
	}

	private IEnumerator FingerDecay(Image finger)
	{
		yield return WaitFor.Seconds(0.05f);
		var c = finger.color;
		finger.color = new Color(c.r, c.g, c.b, Mathf.Clamp(c.a - 0.03f, 0, 1));
		if (finger.color.a <= 0)
		{
			Destroy(finger);
		}
		else
		{
			Instance.StartCoroutine(FingerDecay(finger));
		}
	}

	public void OpenTabWindow()
	{
		StartCoroutine(AnimTabRoll(true, true));
	}
	public void CloseTabWindow()
	{
		StartCoroutine(AnimTabRoll(true, false));
	}
	// Used by UI to open/close tab window
	// Code should use Open/Close method to set expectation and wait for current anim to finish
	public void ToggleTabRollOut()
	{
		StartCoroutine(AnimTabRoll(false, null));
	}

	//Tab roll in and out animation
	IEnumerator AnimTabRoll(bool wait, bool? shouldOpen)
	{
		if (shouldOpen == true && rolledOut)
		{
			yield break;
		}
		else if (shouldOpen == false && !rolledOut)
		{
			yield break;
		}
		if (!preventRoll)
		{
			preventRoll = true;
		}
		else
		{
			if (wait)
			{
				while (preventRoll)
				{
					yield return WaitFor.Seconds(0.1f);
				}
				preventRoll = true;
			}
			else
			{
				yield break;
			}
		}
		rolledOut = !rolledOut;
		Vector3 currentPos = transform.position;
		Vector3 targetPos = currentPos;
		if (!rolledOut)
		{
			//go up
			targetPos.y += (382f * canvas.scaleFactor);
		}
		else
		{
			//go down
			targetPos.y -= (382f * canvas.scaleFactor);
		}
		float lerpTime = 0f;
		while (lerpTime < 0.96f)
		{
			lerpTime += Time.deltaTime * 4f;
			transform.position = Vector3.Lerp(currentPos, targetPos, lerpTime);
			yield return WaitFor.EndOfFrame;
		}
		yield return WaitFor.EndOfFrame;
		transform.position = targetPos;
		Vector3 newScale = rolloutIcon.localScale;
		newScale.y = -newScale.y;
		rolloutIcon.localScale = newScale;
		preventRoll = false;
	}

}