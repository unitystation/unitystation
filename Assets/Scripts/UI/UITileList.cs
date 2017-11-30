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

	public static void addObjectToPanel(GameObject gameObject)
	{
		//Add new item to the list
		GameObject tilePanel = GameObject.Instantiate(Instance.tileItemPanel, Instance.transform);

		//Set the GameObject, Image and Text values for the display panel
		UITileListItem uiTileListItem = tilePanel.GetComponent<UITileListItem>();
		SpriteRenderer spriteRenderer = gameObject.transform.GetComponentInChildren<SpriteRenderer>(false);
		uiTileListItem.tileItem = gameObject;
		uiTileListItem.image.sprite = spriteRenderer.sprite;
		uiTileListItem.text.text = gameObject.name;

		Instance.listedObjects.Add(gameObject);
		UpdateItemPanelSize();
	}

	private static void UpdateItemPanelSize()
	{
		float height = Instance.tileItemPanel.GetComponent<RectTransform>().rect.height;
		Instance.gameObject.GetComponent<LayoutElement>().minHeight = height * Instance.listedObjects.Count;
	}

}
