using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITileList : MonoBehaviour {

	public GameObject tileItemPanel;

	private List<GameObject> listedObjects;

	private static UITileList uiTileList;

	private void Awake()
	{
		listedObjects = new List<GameObject>();
	}

	public static UITileList Instance
	{
		get {
			if (!uiTileList) {
				uiTileList = FindObjectOfType<UITileList>();
			}

			return uiTileList;
		}
	}

	public static List<GameObject> GetItemsAtPosition(Vector3 position)
	{
		LayerMask layerMaskWithFloors = LayerMask.GetMask("Default", "Furniture", "Walls", "Windows", "Machines",
			"Players", "Items", "Door Open", "Door Closed", "WallMounts", "HiddenWalls");
		var hits = Physics2D.RaycastAll(position, Vector2.zero, 10f, layerMaskWithFloors);
		List<GameObject> tiles = new List<GameObject>();

		foreach (var hit in hits)
		{
			tiles.Add(hit.collider.gameObject);
		}

		//So that deepest objects (floors) appear first
		tiles.Reverse();

		return tiles;
	}

	public static Vector3 GetListedItemsLocation()
	{
		if(Instance.listedObjects.Count == 0) {
			return Vector3.zero;
		}

		UITileListItem item = Instance.listedObjects[0].GetComponent<UITileListItem>();
		return item.TileItem.transform.position;
	}

	public static void AddObjectToItemPanel(GameObject gameObject)
	{
		//Instantiate new item panel
		GameObject tilePanel = GameObject.Instantiate(Instance.tileItemPanel, Instance.transform);
		UITileListItem uiTileListItem = tilePanel.GetComponent<UITileListItem>();
		uiTileListItem.TileItem = gameObject;
		
		//Add new panel to the list
		Instance.listedObjects.Add(tilePanel);
		UpdateItemPanelSize();
	}

	public static void UpdateItemPanelList()
	{
		Vector3 position = GetListedItemsLocation();
		List<GameObject> newList = GetItemsAtPosition(position);
		List<GameObject> oldList = new List<GameObject>();

		foreach(GameObject gameObject in Instance.listedObjects) {
			oldList.Add(gameObject.GetComponent<UITileListItem>().TileItem);
		}

		//If item stack has changed, redo the objects tab
		if (!EnumerableExt.AreEquivalent<GameObject>(newList, oldList)) {
			ClearItemPanel();
			foreach(GameObject gameObject in newList) {
				AddObjectToItemPanel(gameObject);
			}
		}
	}

	public static void ClearItemPanel() {
		foreach(GameObject gameObject in Instance.listedObjects) {
			Destroy(gameObject);
		} 
		Instance.listedObjects.Clear();
		UpdateItemPanelSize();
	}

	public static void RemoveTileListItem(GameObject tileListItemObject)
	{
		if (!Instance.listedObjects.Contains(tileListItemObject)){
			Debug.LogError("Attempted to remove tileListItem not on list");
			return;
		}

		Instance.listedObjects.Remove(tileListItemObject);
		Destroy(tileListItemObject);
		UpdateItemPanelSize();
	}

	private static void UpdateItemPanelSize()
	{
		float height = Instance.tileItemPanel.GetComponent<RectTransform>().rect.height;
		int count = Instance.listedObjects.Count;
		LayoutElement layoutElement = Instance.gameObject.GetComponent<LayoutElement>();
		VerticalLayoutGroup verticalLayoutGroup = Instance.gameObject.GetComponent<VerticalLayoutGroup>();

		layoutElement.minHeight = (height * count) + (verticalLayoutGroup.spacing * count);
	}

}
