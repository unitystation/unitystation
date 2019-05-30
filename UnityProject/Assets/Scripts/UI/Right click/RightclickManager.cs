using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Main logic for managing right click behavior.
///
/// There are 2 approaches for defining right click options.
/// 1. A component can implement IRightClickable to define what options should be shown based on its current state.
/// 2. (For development only) Add the RightClickMethod attribute to a method on a component.
///
/// Refer to documentation at https://github.com/unitystation/unitystation/wiki/Right-Click-Menu-(Interaction-Framework-2)
/// </summary>
public class RightclickManager : MonoBehaviour
{
	public List<Menu> options = new List<Menu>();

	[Tooltip("Ordering to use for right click options.")]
	public RightClickOptionOrder rightClickOptionOrder;

	/// saved reference to lighting sytem, for checking FOV occlusion
	private LightingSystem lightingSystem;

	//cached methods attributed with RightClickMethod
	private List<RightClickAttributedComponent> attributedTypes;

	//defines a particular component that has one or more methods which have been attributed with RightClickMethod. Cached
	// in the above list to avoid expensive lookup at click-time.
	private class RightClickAttributedComponent
	{
		public Type ComponentType;
		public List<MethodInfo> AttributedMethods;
	}


	/// <summary>
	/// Encapsulates all the info for a single radial menu item - sub menus as well as top-level menu items
	/// </summary>
	/// TODO: Definitely refactor to its own class and create static factories
	public class Menu
	{
		public Color Colour;
		public Sprite Sprite;
		public string Label;

		public List<Menu> SubMenus = new List<Menu>();
		public Action Action;
	}

	private void Start()
	{
		lightingSystem = Camera.main.GetComponent<LightingSystem>();

		//cache all known usages of the RightClickMethod annotation
		attributedTypes = GetRightClickAttributedMethods();
	}

	private  List<RightClickAttributedComponent> GetRightClickAttributedMethods()
	{
		var result = new List<RightClickAttributedComponent>();

		var allComponentTypes = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(s => s.GetTypes())
			.Where(s => typeof(MonoBehaviour).IsAssignableFrom(s));

		foreach (var componentType in allComponentTypes)
		{
			var attributedMethodsForType = componentType.GetMethods()
				.Where(m => m.GetCustomAttributes(typeof(RightClickMethod), false).Length > 0)
				.ToList();
			if (attributedMethodsForType.Count > 0)
			{
				RightClickAttributedComponent component = new RightClickAttributedComponent
				{
					ComponentType = componentType, AttributedMethods = attributedMethodsForType
				};
				result.Add(component);
			}
		}

		return result;
	}

	void Update()
	{
		// Get right mouse click and check if mouse point occluded by FoV system.
		if (CommonInput.GetMouseButtonDown(1) &&  (!lightingSystem.enabled || lightingSystem.IsScreenPointVisible(CommonInput.mousePosition)))
		{
			//gets Items on the position of the mouse that are able to be right clicked
			List<GameObject> objects = GetRightClickableObjects();
			//Generates menus
			options = Generate(objects);
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

	//TODO: Clean up all this code, it's a bit messy, sprawling. Possibly refactor Menu into an actual class

	private List<Menu> Generate(List<GameObject> objects)
	{
		var result = new List<Menu>();
		foreach (var curObject in objects)
		{
			Menu objectMenu = CreateObjectMenu(curObject);

			//check for any IRightClickable components and gather their options
			var rightClickables = curObject.GetComponents<IRightClickable>();
			var rightClickableResult = new RightClickableResult();
			if (rightClickables != null)
			{
				foreach (var rightClickable in rightClickables)
				{
					rightClickableResult.AddElements(rightClickable.GenerateRightClickOptions());
				}
			}
			//add the menu items generated so far
			objectMenu.SubMenus.AddRange(rightClickableResult.AsOrderedMenus(rightClickOptionOrder));

			//check for any components that have an attributed method. These are added to the end in whatever order,
			//doesn't matter since it's only for development.
			foreach (var attributedType in attributedTypes)
			{
				var components = curObject.GetComponents(attributedType.ComponentType);
				foreach (var component in components)
				{
					//create menu items for these components
					objectMenu.SubMenus.AddRange(CreateSubMenuOptions(attributedType, component));
				}
			}

			if (objectMenu.SubMenus.Count > 0)
			{
				result.Add(objectMenu);
			}
		}

		return result;
	}

	//creates a sub menu option based on the value in the dict returned from the IRightClickable
	private Menu CreateSubMenuOption(RightClickOption forOption, Action action)
	{
		var menu = forOption.AsMenu();
		menu.Action = action;

		return menu;
	}


	//creates sub menu items for the specified component, hooking them up the the corresponding method on the
	//actual component
	private IEnumerable<Menu> CreateSubMenuOptions(RightClickAttributedComponent attributedType, Component actualComponent)
	{
		return attributedType.AttributedMethods
			.Select(m => CreateSubMenuOption(m, actualComponent));
	}

	private Menu CreateSubMenuOption(MethodInfo forMethod, Component actualComponent)
	{
		var rightClickMethod = forMethod.GetCustomAttribute<RightClickMethod>();
		var menu = rightClickMethod.AsMenu();
		//hook up the component action
		menu.Action = (Action) Delegate.CreateDelegate(typeof(Action), actualComponent, forMethod);
		return menu;
	}

	//creates the top-level menu item for this object. If object has a RightClickMenu, uses that to
	//define the appeareance. Otherwise sticks to defaults. Doesn't populate the sub menus though.
	private Menu CreateObjectMenu(GameObject forObject)
	{
		RightClickAppearance rightClickAppearance = forObject.GetComponent<RightClickAppearance>();
		if (rightClickAppearance != null)
		{
			//use right click menu to determine appearance
			return rightClickAppearance.AsMenu();
		}

		//use defaults
		var menu = new Menu();
		menu.Colour = Color.gray;
		menu.Label = forObject.name.Replace("(clone)","");
		SpriteRenderer firstSprite = forObject.GetComponentInChildren<SpriteRenderer>();
		if (firstSprite != null)
		{
			menu.Sprite = firstSprite.sprite;
		}
		else
		{
			Logger.LogWarningFormat("Could not determine sprite to use for right click menu" +
			                        " for object {0}. Please specify a sprite in the RightClickMenu component" +
			                        " for this object.", Category.UI, forObject.name);
		}

		return menu;
	}
}