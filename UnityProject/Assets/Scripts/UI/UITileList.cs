using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Tiles;
using PlayGroup;

public class UITileList : MonoBehaviour
{

    public GameObject tileItemPanel;

    private List<GameObject> listedObjects;
	private LayerTile listedTile;
	private Vector3 listedTilePosition;
    private static UITileList uiTileList;

    private void Awake()
    {
        listedObjects = new List<GameObject>();
    }

    public static UITileList Instance
    {
        get
        {
            if (!uiTileList)
            {
                uiTileList = FindObjectOfType<UITileList>();
            }

            return uiTileList;
        }
    }

    /// <summary>
    /// Returns GameObjects eligible to be displayed in the Item List Tab
    /// </summary>
    /// <param name="position">Position where to look for items</param>
    public static List<GameObject> GetItemsAtPosition(Vector3 position)
    {
        LayerMask layerMaskWithFloors = LayerMask.GetMask("Default", "Furniture", "Walls", "Windows", "Machines",
			"Items", "Door Open", "Door Closed", "WallMounts", "HiddenWalls");
        var hits = Physics2D.RaycastAll(position, Vector2.zero, 2f, layerMaskWithFloors);
        List<GameObject> tiles = new List<GameObject>();

		foreach (var hit in hits)
        {
            tiles.Add(hit.collider.gameObject);
        }

        return tiles;
    }

	/// <summary>
	/// Returns LayerTile eligible to be displayed in the Item List Tab
	/// </summary>
	/// <param name="position">Position where to look for tile</param>
	public static LayerTile GetTileAtPosition(Vector3 position)
	{
		MetaTileMap metaTileMap = PlayerManager.LocalPlayerScript.gameObject.GetComponentInParent<MetaTileMap>();
		Vector3Int tilePosition = new Vector3Int(
			Mathf.FloorToInt(position.x),
			Mathf.FloorToInt(position.y), 
			Mathf.FloorToInt(position.z)
		);

		foreach (LayerType layer in LayerType.GetValues(typeof(LayerType))) {
			LayerTile tile = metaTileMap.GetTile(tilePosition, layer);
			if (tile != null) {
				return tile;
			}

		}

		return null;
	}

	/// <summary>
	/// Returns world location of the items, currently being displayed in Item List Tab. Returns vector3(0f,0f,-100f) on failure.
	/// </summary>
	public static Vector3 GetListedItemsLocation()
    {
        if (Instance.listedObjects.Count == 0) {
			return new Vector3(0f, 0f, -100f);
        }

		if (Instance.listedTile) {
			return Instance.listedTilePosition;
		}

		UITileListItem item = Instance.listedObjects[0].GetComponent<UITileListItem>();
        return item.Item.transform.position;
    }

    /// <summary>
    /// Adds game object to be displayed in Item List Tab.
    /// </summary>
    /// <param name="gameObject">The GameObject to be displayed</param>
    public static void AddObjectToItemPanel(GameObject gameObject)
    {
        //Instantiate new item panel
        GameObject tilePanel = GameObject.Instantiate(Instance.tileItemPanel, Instance.transform);
        UITileListItem uiTileListItem = tilePanel.GetComponent<UITileListItem>();
        uiTileListItem.Item = gameObject;

        //Add new panel to the list
        Instance.listedObjects.Add(tilePanel);
        UpdateItemPanelSize();
    }

	/// <summary>
	/// Adds tile to be displayed in Item List Tab.
	/// </summary>
	/// <param name="tile">The LayerTile to be displayed</param>
	/// <param name="position">The Position of the LayerTile to be displayed</param>
	public static void AddTileToItemPanel(LayerTile tile, Vector3 position)
	{
		//Instantiate new item panel
		GameObject tilePanel = GameObject.Instantiate(Instance.tileItemPanel, Instance.transform);
		UITileListItem uiTileListItem = tilePanel.GetComponent<UITileListItem>();
		uiTileListItem.Tile = tile;

		//Add new panel to the list
		Instance.listedObjects.Add(tilePanel);
		Instance.listedTile = tile;
		Instance.listedTilePosition = position;
		UpdateItemPanelSize();
	}

	/// <summary>
	/// Updates Item List Tab to match what the current item stack holds
	/// </summary>
	public static void UpdateItemPanelList()
    {
        Vector3 position = GetListedItemsLocation();

		if(position == new Vector3(0f, 0f, -100f)) {
			ClearItemPanel();
			return;
		}

		List<GameObject> newList = GetItemsAtPosition(position);
        List<GameObject> oldList = new List<GameObject>();

        foreach (GameObject gameObject in Instance.listedObjects) {
			GameObject item = gameObject.GetComponent<UITileListItem>().Item;
			//We don't want to add the TileLayer in listedObjects
			if(item != null) {
				oldList.Add(item);
			}
			
        }

		LayerTile newTile =  GetTileAtPosition(position);

		//If item stack has changed, redo the itemList tab
		if (!EnumerableExt.AreEquivalent<GameObject>(newList, oldList) || newTile.name != Instance.listedTile.name) {
            ClearItemPanel();
			AddTileToItemPanel(newTile, position);
			foreach (GameObject gameObject in newList) {
                AddObjectToItemPanel(gameObject);
            }
        }
    }

    /// <summary>
    /// Removes all itemList from the Item List Tab
    /// </summary>
    public static void ClearItemPanel()
    {
        foreach (GameObject gameObject in Instance.listedObjects)
        {
            Destroy(gameObject);
        }
        Instance.listedObjects.Clear();
		Instance.listedTile = null;
		Instance.listedTilePosition = new Vector3(0f, 0f, -100f);
        UpdateItemPanelSize();
    }

    /// <summary>
    /// Removes a specific object from the Item List Tab
    /// </summary>
    /// <param name="tileListItemObject">The GameObject to be removed</param>
    public static void RemoveTileListItem(GameObject tileListItemObject)
    {
        if (!Instance.listedObjects.Contains(tileListItemObject))
        {
            Debug.LogError("Attempted to remove tileListItem not on list");
            return;
        }

        Instance.listedObjects.Remove(tileListItemObject);
        Destroy(tileListItemObject);
        UpdateItemPanelSize();
    }

    private static void UpdateItemPanelSize()
    {
        //Since the content to this tab is added dynamically, we need to update the panel on any changes
        float height = Instance.tileItemPanel.GetComponent<RectTransform>().rect.height;
        int count = Instance.listedObjects.Count;

        LayoutElement layoutElement = Instance.gameObject.GetComponent<LayoutElement>();
        VerticalLayoutGroup verticalLayoutGroup = Instance.gameObject.GetComponent<VerticalLayoutGroup>();

        layoutElement.minHeight = (height * count) + (verticalLayoutGroup.spacing * count);
    }

}
