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
		SceneManager.sceneLoaded += OnLevelFinishedLoading;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnLevelFinishedLoading;
	}

	private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
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
			var list = new List<Tab>();
			var tabs = TabStorage.GetComponentsInChildren<Tab>(true);
			for (var i = 0; i < tabs?.Length; i++)
			{
				Tab tab = tabs[i];
				if (!tab.Hidden)
				{
					list.Add(tab);
				}
			}
			tabs = TabStoragePopOut.GetComponentsInChildren<Tab>(true);
			for (var i = 0; i < tabs?.Length; i++)
			{
				Tab tab = tabs[i];
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
			var tabs = TabStorage.GetComponentsInChildren<Tab>(true);
			for (var i = 0; i < tabs?.Length; i++)
			{
				Tab tab = tabs[i];
				if (tab.Hidden)
				{
					list.Add(tab);
				}
			}
			tabs = TabStoragePopOut.GetComponentsInChildren<Tab>(true);
			for (var i = 0; i < tabs?.Length; i++)
			{
				Tab tab = tabs[i];
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
			var netTabs = new Dictionary<NetTabDescriptor, NetTab>();
			var activeTabs = ActiveTabs;
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

		SelectTab(ClientTabType.Stats);
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

			((RectTransform)headerButton.transform).anchoredPosition = Vector2.right * (i * 40);
			//need to wait till the next frame to set proper position
			StartCoroutine(SetHeaderPosition(headerButton.gameObject, i));

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

	private IEnumerator SetHeaderPosition(GameObject header, int index = 0)
	{
		yield return YieldHelper.EndOfFrame;

		if (header != null)
		{
			((RectTransform)header.transform).anchoredPosition = Vector3.right * GetHeaderOffset(index);
		}
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
		tab.gameObject.SetActive(false);
		RefreshTabHeaders();
		SelectTab(ClientTabType.Stats, false);
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

		bool isPopOut = false;

		//Add the popout types here:
		if ((type == NetTabType.Paper) || (type == NetTabType.ChemistryDispenser))
		{
			isPopOut = true;
		}

		if (!Instance.rolledOut && !isPopOut)
		{
			Instance.StartCoroutine(Instance.AnimTabRoll());
		}

		var openedTab = new NetTabDescriptor(tabProvider, type);
		//try to dig out a hidden tab with matching parameters and enable it:
		if (Instance.HiddenNetTabs.ContainsKey(openedTab))
		{
			//				Logger.Log( $"Yay, found an old hidden {openedTab} tab. Unhiding it" );
			Instance.UnhideTab(Instance.HiddenNetTabs[openedTab]);
		}
		if (!Instance.OpenedNetTabs.ContainsKey(openedTab))
		{
			Transform newParent = !isPopOut ? Instance.TabStorage : Instance.TabStoragePopOut;
			NetTab tabInfo = openedTab.Spawn(newParent);
			GameObject tabObject = tabInfo.gameObject;

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
				tabInfo.isPopOut = true;
			}

			Instance.RefreshTabHeaders();
		}

		NetTab tab = Instance.OpenedNetTabs[openedTab];
		tab.ImportValues(elementValues);
		if (!isPopOut)
		{
			Instance.SelectTab(tab.gameObject, false);
		}
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
		if (Instance.OpenedNetTabs.ContainsKey(lookupTab))
		{
			var tabInfo = Instance.OpenedNetTabs[lookupTab];

			var touchedElement = tabInfo.ImportValues(values);

			if (touched && touchedElement != null)
			{
				Instance.ShowFinger(tabInfo.gameObject, touchedElement.gameObject);
			}
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
		var playerScript = PlayerManager.LocalPlayerScript;

		foreach (NetTab tab in Instance.OpenedNetTabs.Values)
		{
			if (playerScript.canNotInteract() ||
				!playerScript.IsInReach(tab.Provider))
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

		CheckItemListTab();
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
		yield return new WaitForSeconds(0.05f);
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

	//For hiding or showing the tab window
	public void ToggleTabRollOut()
	{
		StartCoroutine(AnimTabRoll());
	}

	//Tab roll in and out animation
	IEnumerator AnimTabRoll()
	{
		if (!preventRoll)
		{
			preventRoll = true;
		}
		else
		{
			yield break;
		}
		Vector3 currentPos = transform.position;
		Vector3 targetPos = currentPos;
		if (rolledOut)
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
			yield return new WaitForEndOfFrame();
		}
		yield return new WaitForEndOfFrame();
		Vector3 newScale = rolloutIcon.localScale;
		newScale.y = -newScale.y;
		rolloutIcon.localScale = newScale;
		rolledOut = !rolledOut;
		preventRoll = false;
	}

}