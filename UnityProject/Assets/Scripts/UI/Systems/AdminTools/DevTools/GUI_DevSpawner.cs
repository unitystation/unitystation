using System;
using System.Collections;
using Logs;
using UI.Chat_UI;
using UI.Systems.AdminTools.DevTools.Search;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main logic for the dev spawner menu
/// </summary>
public class GUI_DevSpawner : MonoBehaviour
{

	public static GUI_DevSpawner Instance;

	[Tooltip("Prefab that should be used for each list item")]
	public GameObject listItemPrefab;
	[Tooltip("content panel into which the list items should be placed")]
	public GameObject contentPanel;
	public InputField searchBox;

	public InputField StackAmountBox;

	public Toggle DEBUGToggle;

	public int StackAmount
	{
		get
		{

			if (int.TryParse(StackAmountBox.text, out var number))
			{
				return number;
			}
			else
			{
				return -1;
			}
		}
	}

	[Tooltip("Always perform a wildcard search, with wildcard being added at the end")]
	public bool alwaysWildcard = true;
	[Tooltip("Update search results as you type into the search box.")]
	public bool searchWhileTyping = false;
	[Tooltip("If searchWhileTyping is turned on, don't start searching until at least this many" +
	         " characters are entered.")]
	public int minCharactersForSearch = 2;

	// search index
	private SpawnerSearch spawnerSearch;

	private bool isFocused;

	public void Awake()
	{
		Instance = this;
	}

	void Start()
    {
	    spawnerSearch = SpawnerSearch.ForPrefabs(Spawn.SpawnablePrefabs());
    }

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

    /// <summary>
    /// There is no event for focusing input, so we must check for it manually in Update
    /// </summary>
    void UpdateMe()
    {
	    if (searchBox.isFocused && isFocused == false)
	    {
		    InputFocus();
	    }
	    else if (searchBox.isFocused == false && isFocused)
	    {
		    InputUnfocus();
	    }
    }

    private void InputFocus()
    {
	    // disable keyboard commands while input is focused
	    isFocused = true;
	    UIManager.IsInputFocus = true;
	    UIManager.PreventChatInput = true;
    }

    private void InputUnfocus()
    {
	    // disable keyboard commands while input is focused
	    isFocused = false;
	    UIManager.IsInputFocus = false;
	    UIManager.PreventChatInput = false;

	    //Note: this is what stops the chat box from opening when pressing enter
	    ChatUI.Instance.StartWindowCooldown();
    }

    public void OnSearchBoxChanged()
    {
	    if (searchWhileTyping)
	    {
		    Search();
	    }
    }

    public void Search()
    {
	    if (searchBox.text.Length < minCharactersForSearch) return;

		// delete previous results
	    foreach (Transform child in contentPanel.transform)
	    {
		    Destroy(child.gameObject);
	    }

	    var docs = spawnerSearch.Search(searchBox.text, DEBUGToggle.isOn);

	    // display new results
	    foreach (var doc in docs)
	    {
		    CreateListItem(doc);
	    }
    }

    // add a list item to the content panel for spawning the specified result
    private void CreateListItem(DevSpawnerDocument doc)
    {
	    GameObject listItem = Instantiate(listItemPrefab, contentPanel.transform);
	    listItem.GetComponent<DevSpawnerListItemController>().Initialize(doc);
	    listItem.transform.localScale = Vector3.one;
    }

	public void Open()
	{
		_ = SoundManager.Play(CommonSounds.Instance.Click01);
		Loggy.Log("Opening dev spawner menu", Category.NetUI);
		transform.GetChild(0).gameObject.SetActive(true);
		transform.SetAsLastSibling();
	}

	public void Close()
	{
		_ = SoundManager.Play(CommonSounds.Instance.Click01);
		Loggy.Log("Closing dev spawner menu", Category.NetUI);
		transform.GetChild(0).gameObject.SetActive(false);
	}
}
