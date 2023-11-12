using System.Collections.Generic;
using System.Linq;
using Logs;
using Objects.Disposals;
using Objects.Other;
using TileManagement;
using UnityEngine;
using UnityEngine.UI;
using Tiles;

namespace UI
{
	public class UITileList : MonoBehaviour
	{
		private static UITileList uiTileList;

		private List<GameObject> listedObjects = new List<GameObject>();
		private LayerTile listedTile;
		private Vector3 listedTilePosition;
		public GameObject tileItemPanel;

		public static UITileList Instance
		{
			get
			{
				if (!uiTileList)
				{
					uiTileList = FindObjectOfType<UITileList>(); //bad practice
				}

				return uiTileList;
			}
			set { uiTileList = value; }
		}

		private void Awake()
		{
			listedObjects = new List<GameObject>();
		}

		/// <summary>
		///     Returns GameObjects eligible to be displayed in the Item List Tab
		/// </summary>
		/// <param name="position">Position where to look for items</param>
		public static List<GameObject> GetItemsAtPosition(Vector3 position)
		{
			var matrix = MatrixManager.AtPoint(Vector3Int.RoundToInt(position), CustomNetworkManager.Instance._isServer).Matrix;
			if (matrix == false)
			{
				return new List<GameObject>();
			}

			position = matrix.transform.InverseTransformPoint(position);
			Vector3Int tilePosition = Vector3Int.FloorToInt(position);

			var registerTiles = matrix.Get<RegisterTile>(tilePosition, false);

			var result = registerTiles.Select(x => x.gameObject).Distinct().ToList();
			var Count = result.Count;

			for (int i = 0; i < Count; i++)
			{
				if (result.Count >= i) continue;

				var possibleGhost = result[i];
				if (possibleGhost.HasComponent<GhostMove>()
				    || possibleGhost.HasComponent<DisposalVirtualContainer>()
				    || possibleGhost.HasComponent<CrawlingVirtualContainer>()) result.Remove(possibleGhost);
			}

			//include interactable tiles
			var interactableTiles = matrix.GetComponentInParent<InteractableTiles>();
			if (interactableTiles != null)
			{
				result.Add(interactableTiles.gameObject);
			}

			return result;
		}

		/// <summary>
		///     Returns LayerTile eligible to be displayed in the Item List Tab
		/// </summary>
		/// <param name="position">Position where to look for tile</param>
		public static LayerTile GetTileAtPosition(Vector3 position)
		{
			MetaTileMap metaTileMap = MatrixManager.AtPoint(position, CustomNetworkManager.IsServer).MetaTileMap;

			position = metaTileMap.transform.InverseTransformPoint(position);
			Vector3Int tilePosition = Vector3Int.FloorToInt(position);

			return metaTileMap.GetTile(tilePosition);
		}

		/// <summary>
		///     Returns world location of the items, currently being displayed in Item List Tab. Returns vector3(0f,0f,-100f) on
		///     failure.
		/// </summary>
		public static Vector3 GetListedItemsLocation()
		{
			if (Instance.listedObjects.Count == 0)
			{
				return new Vector3(0f, 0f, -100f);
			}

			if (Instance.listedTile)
			{
				return Instance.listedTilePosition;
			}

			UITileListItem item = Instance.listedObjects[0].GetComponent<UITileListItem>();
			return item.Item.transform.position;
		}

		/// <summary>
		///     Adds game object to be displayed in Item List Tab.
		/// </summary>
		/// <param name="gameObject">The GameObject to be displayed</param>
		public static void AddObjectToItemPanel(GameObject gameObject)
		{
			//Instantiate new item panel
			GameObject tilePanel = Instantiate(Instance.tileItemPanel, Instance.transform);
			UITileListItem uiTileListItem = tilePanel.GetComponent<UITileListItem>();
			uiTileListItem.Item = gameObject;

			//Add new panel to the list
			Instance.listedObjects.Add(tilePanel);
			UpdateItemPanelSize();
		}

		/// <summary>
		///     Adds tile to be displayed in Item List Tab.
		/// </summary>
		/// <param name="tile">The LayerTile to be displayed</param>
		/// <param name="position">The Position of the LayerTile to be displayed</param>
		public static void AddTileToItemPanel(LayerTile tile, Vector3 position)
		{
			//Instantiate new item panel
			GameObject tilePanel = Instantiate(Instance.tileItemPanel, Instance.transform);
			UITileListItem uiTileListItem = tilePanel.GetComponent<UITileListItem>();
			uiTileListItem.Tile = tile;

			//Add new panel to the list
			Instance.listedObjects.Add(tilePanel);
			Instance.listedTile = tile;
			Instance.listedTilePosition = position;
			UpdateItemPanelSize();
		}

		/// <summary>
		///     Updates Item List Tab to match what the current item stack holds
		/// </summary>
		public static void UpdateItemPanelList()
		{
			Vector3 position = GetListedItemsLocation();

			if (position == new Vector3(0f, 0f, -100f))
			{
				ClearItemPanel();
				return;
			}

			IEnumerable<GameObject> newList = GetItemsAtPosition(position);
			LayerTile newTile = GetTileAtPosition(position);

			List<GameObject> oldList = new List<GameObject>();

			foreach (GameObject gameObject in Instance.listedObjects)
			{
				GameObject item = gameObject.GetComponent<UITileListItem>().Item;
				//We don't want to add the TileLayer in listedObjects
				if (item != null)
				{
					oldList.Add(item);
				}
			}

			//If item stack has changed, redo the itemList tab
			if (!newList.AreEquivalent(oldList) || newTile != Instance.listedTile)
			{
				UpdateTileList(newList, newTile, position);
			}
		}

		public static void UpdateTileList(IEnumerable<GameObject> objects, LayerTile tile, Vector3 position)
		{
			ClearItemPanel();

			if (tile != null)
			{
				AddTileToItemPanel(tile, position);
			}

			foreach (GameObject itemObject in objects)
			{
				AddObjectToItemPanel(itemObject);
			}
		}

		/// <summary>
		///     Removes all itemList from the Item List Tab
		/// </summary>
		public static void ClearItemPanel()
		{

			foreach (GameObject gameObject in Instance.listedObjects)
			{
				Destroy(gameObject);
			}
			Instance.listedObjects.Clear();
			Instance.listedTile = null;
			Instance.listedTilePosition = TransformState.HiddenPos;
			UpdateItemPanelSize();
		}

		public static bool IsEmpty()
		{
			return Instance.listedObjects.Count == 0 && Instance.listedTile == null;
		}

		/// <summary>
		///     Removes a specific object from the Item List Tab
		/// </summary>
		/// <param name="tileListItemObject">The GameObject to be removed</param>
		public static void RemoveTileListItem(GameObject tileListItemObject)
		{
			if (!Instance.listedObjects.Contains(tileListItemObject))
			{
				Loggy.LogError("Attempted to remove tileListItem not on list", Category.NetUI);
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

			layoutElement.minHeight = height * count + verticalLayoutGroup.spacing * count;
		}
	}
}