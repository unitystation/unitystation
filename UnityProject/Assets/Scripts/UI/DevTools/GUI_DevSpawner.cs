using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Documents;
using Lucene.Unity;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Main logic for the dev spawner menu
/// </summary>
public class GUI_DevSpawner : MonoBehaviour
{

	[Tooltip("Prefab that should be used for each list item")]
	public GameObject listItemPrefab;
	[Tooltip("content panel into which the list items should be placed")]
	public GameObject contentPanel;
	public InputField searchBox;

	[Tooltip("Always perform a wildcard search, with wildcard being added at the end")]
	public bool alwaysWildcard = true;
	[Tooltip("Update search results as you type into the search box.")]
	public bool searchWhileTyping = false;
	[Tooltip("If searchWhileTyping is turned on, don't start searching until at least this many" +
	         " characters are entered.")]
	public int minCharactersForSearch = 3;

	// search index
	private Lucene3D lucene;

	private bool isFocused;

	void Start()
    {
	    ConfigureLucene();

	    //get prefabs
	    var toIndex = PoolManager.SpawnablePrefabs.Select(DevSpawnerDocument.ForPrefab).ToList();

	    //get clothing
	    var data = Resources.FindObjectsOfTypeAll<ClothingData>();
	    //force clothfactory to load all clothing data
	    foreach (var clothData in data)
	    {
		    if (!ClothFactory.Instance.ClothingStoredData.ContainsKey(clothData.name))
		    {
			    ClothFactory.Instance.ClothingStoredData.Add(clothData.name, clothData);
		    }

		    toIndex.Add(DevSpawnerDocument.ForClothing(clothData));
	    }

	    //start indexing
	    StartCoroutine(lucene.IndexCoroutine(toIndex));
    }

    /// <summary>
    /// There is no event for focusing input, so we must check for it manually in Update
    /// </summary>
    void Update()
    {
	    if (searchBox.isFocused && !isFocused)
	    {
		    InputFocus();
	    }
	    else if (!searchBox.isFocused && isFocused)
	    {
		    InputUnfocus();
	    }
    }

    private void InputFocus()
    {
	    //disable keyboard commands while input is focused
	    isFocused = true;
	    UIManager.IsInputFocus = true;
    }

    private void InputUnfocus()
    {
	    //disable keyboard commands while input is focused
	    isFocused = false;
	    UIManager.IsInputFocus = false;
    }

    private void OnLuceneProgress(object sender, LuceneProgressEventArgs e)
    {
	    Logger.LogFormat("Lucene indexing progress title: {0}, total: {1}", Category.ItemSpawn, e.Title, e.Total);
    }

    public void OnSearchBoxChanged()
    {
	    if (searchWhileTyping)
	    {
		    if (searchBox.text.Length >= minCharactersForSearch)
		    {
			    Search();
		    }
	    }
    }

    public void Search()
    {
	    //delete previous results
	    foreach (Transform child in contentPanel.transform)
	    {
		    Destroy(child.gameObject);
	    }

	    var search = searchBox.text;
	    if (alwaysWildcard)
	    {
		    search = "*" + search + "*";
	    }

	    var docs = lucene.Search(search, true);

	    //display new results
	    foreach (var doc in docs)
	    {
		    CreateListItem(doc);
	    }
    }

    private void ConfigureLucene()
    {

	    //dont reindex when reopening the spawner
	    if (lucene == null)
	    {
		    lucene = new Lucene3D(deleteIfExists: true);

		    lucene.Progress += OnLuceneProgress;

		    lucene.DefineIndexField<DevSpawnerDocument>("id", doc => doc.Name, IndexOptions.PrimaryKey);
		    lucene.DefineIndexField<DevSpawnerDocument>("name", doc => doc.Name, IndexOptions.IndexTermsAndStore);
		    lucene.DefineIndexField<DevSpawnerDocument>("isClothing", doc => doc.Type == SpawnableType.CLOTHING_DATA ? "1" : "0", IndexOptions.StoreOnly);
	    }
    }



    //add a list item to the content panel for spawning the specified result
    private void CreateListItem(Document doc)
    {
	    GameObject listItem = Instantiate(listItemPrefab);
	    listItem.GetComponent<DevSpawnerListItemController>().Initialize(doc);
	    listItem.transform.SetParent(contentPanel.transform);
	    listItem.transform.localScale = Vector3.one;
    }

	public void Open()
	{
		SoundManager.Play("Click01");
		Logger.Log("Opening dev spawner menu", Category.UI);
		transform.GetChild(0).gameObject.SetActive(true);
		transform.SetAsLastSibling();
	}

	public void Close()
	{
		SoundManager.Play("Click01");
		Logger.Log("Closing dev spawner menu", Category.UI);
		transform.GetChild(0).gameObject.SetActive(false);
	}
}
