using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Main logic for managing right click behavior.
/// </summary>
public class RightclickManager : MonoBehaviour
{
	public List<Menu> options = new List<Menu>();

	[Tooltip("Ordering to use for right click options.")]
	public RightClickOptionOrder rightClickOptionOrder;

	/// saved reference to lighting sytem, for checking FOV occlusion
	private LightingSystem lightingSystem;


	/// <summary>
	/// Encapsulates all the info for a single radial menu item - sub menus as well as top-level menu items
	/// </summary>
	public class Menu
	{
		public Color colour;
		public Sprite sprite;
		public string title;

		public GameObject Item;
		public List<Menu> SubMenus = new List<Menu>();
		public Action Action;
	}

	private void Start()
	{
		lightingSystem = Camera.main.GetComponent<LightingSystem>();
	}

	void Update()
	{
		// Get right mouse click and check if mouse point occluded by FoV system.
		if (CommonInput.GetMouseButtonDown(1) &&  (!lightingSystem.enabled || lightingSystem.IsScreenPointVisible(CommonInput.mousePosition)))
		{
			//gets Items on the position of the mouse that are able to be right clicked
			List<GameObject> objects = GetRightClickableObjects();
			//Generates menus
			Generate(objects);
			//Logger.Log ("yo", Category.UI);
			if (options.Count > 0)
			{
				RadialMenuSpawner.ins.SpawnRadialMenu(options);
			}
		}
	}

	private List<GameObject> GetRightClickableObjects()
	{
		Vector3 position = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);
		position.z = 0f;
		List<GameObject> objects = UITileList.GetItemsAtPosition(position);

		//special case, remove wallmounts that are transparent
		objects.RemoveAll(IsHiddenWallmount);

		//Objects that are under a floor tile should not be available
		objects.RemoveAll(IsUnderFloorTile);

		return objects;
	}

	private bool IsHiddenWallmount(GameObject obj)
	{
		WallmountBehavior wallmountBehavior = obj.GetComponent<WallmountBehavior>();
		if (wallmountBehavior == null)
		{
			//not a wallmount
			return false;
		}

		return wallmountBehavior.IsHiddenFromLocalPlayer();
	}

	private bool IsUnderFloorTile(GameObject obj)
	{
		LayerTile tile = UITileList.GetTileAtPosition(obj.WorldPosClient());

		if (tile.LayerType != LayerType.Base && obj.layer < 1)
		{
			return true;
		}
		return false;
	}

	private void Generate(List<GameObject> objects)
	{
		options = new List<Menu>();
		for (int i = 0; i < objects.Count; i++)
		{
			//get the right click menu options for current object from their RightClickMenu
			//component
			RightClickMenu rightClickMenu = objects[i].GetComponent<RightClickMenu>();

			if (rightClickMenu == null)
			{
				//no right click behavior on this object, go to the next object
				continue;
			}

			options.Add(rightClickMenu.AsMenu(rightClickOptionOrder));
		}
	}
}