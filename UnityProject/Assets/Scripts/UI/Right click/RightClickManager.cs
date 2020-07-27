using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.EventSystems;
using YamlDotNet.Core.Tokens;

/// <summary>
/// Main logic for managing right click behavior.
///
/// There are 2 approaches for defining right click options.
/// 1. A component can implement IRightClickable to define what options should be shown based on its current state.
/// 2. (For development only) Add the RightClickMethod attribute to a method on a component.
///
/// Refer to documentation at https://github.com/unitystation/unitystation/wiki/Right-Click-Menu
/// </summary>
public class RightClickManager : MonoBehaviour
{
	[Tooltip("Ordering to use for right click options.")]
	public RightClickOptionOrder rightClickOptionOrder;

	[Tooltip("Having it on every item is Pretty cool so best place to put it")]
	public RightClickOption VariableViewerOption;

	/// saved reference to lighting sytem, for checking FOV occlusion
	private LightingSystem lightingSystem;

	//cached methods attributed with RightClickMethod
	private static List<RightClickAttributedComponent> attributedTypes = new List<RightClickAttributedComponent>();
	private List<RaycastResult> raycastResults = new List<RaycastResult>();

	//defines a particular component that has one or more methods which have been attributed with RightClickMethod. Cached
	// in the above list to avoid expensive lookup at click-time.
	private class RightClickAttributedComponent
	{
		public Type ComponentType;
		public List<MethodInfo> AttributedMethods;
	}

	private void Awake()
	{
		//cache all known usages of the RightClickMethod annotation
		if (attributedTypes.Count == 0)
		{
			new Task(GetRightClickAttributedMethods).Start();
		}

		// Will be enabled by ControlDisplays when needed
		gameObject.SetActive(false);
	}

	private void OnEnable()
	{
		lightingSystem = Camera.main.GetComponent<LightingSystem>();
	}

	private void GetRightClickAttributedMethods()
	{
		var result = new List<RightClickAttributedComponent>();

		var allComponentTypes = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(s => s.GetTypes())
			.Where(s => typeof(MonoBehaviour).IsAssignableFrom(s));

		foreach (var componentType in allComponentTypes)
		{
			var attributedMethodsForType = componentType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy)
				.Where(m => m.GetCustomAttribute<RightClickMethod>(true) != null)
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

		attributedTypes = result;
	}

	void Update()
	{
		// Get right mouse click
		if (CommonInput.GetMouseButtonDown(1))
		{
			List<GameObject> objects = null;
			// Check if mouse point occluded by FoV system.
			if (!lightingSystem.enabled || lightingSystem.IsScreenPointVisible(CommonInput.mousePosition))
			{
				// Gets Items on the position of the mouse that are able to be right clicked
				objects = GetRightClickableObjects();
			}
			else
			{
				objects = new List<GameObject>();
			}

			// Gets UI elements
			var pointerData = new PointerEventData(EventSystem.current)
			{
				pointerId = -1, // Mouse
				position = CommonInput.mousePosition,
			};
			EventSystem.current.RaycastAll(pointerData, raycastResults);
#pragma warning disable UEA0005 // Ignore warning about using GetComponent() inside Update()
			// Searching for UI_ItemSwap instead of UI_ItemSlot for the larger and more consistent hitbox.
			objects.AddRange(raycastResults.Select(rc => {
				// Verbose workaround since you should not use null propagation on Unity objects. Thanks, Unity.
				var itemSwap = rc.gameObject.GetComponent<UI_ItemSwap>();
				var itemSlot = itemSwap == null ? null : itemSwap.GetComponentInChildren<UI_ItemSlot>();
				return itemSlot == null ? null : itemSlot.ItemObject;
				}).Where(go => go != null));
#pragma warning restore UEA0005

			//Generates menus
			var options = Generate(objects);
			//Logger.Log ("yo", Category.UI);
			if (options != null && options.Count > 0)
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

		// Layer 22 is the 'Floor' layer
		return (tile != null && tile.LayerType != LayerType.Base && obj.layer == 22);
	}

	private List<RightClickMenuItem> Generate(List<GameObject> objects)
	{
		if (objects == null || objects.Count == 0)
		{
			return null;
		}
		var result = new List<RightClickMenuItem>();
		foreach (var curObject in objects)
		{
			var subMenus = new List<RightClickMenuItem>();

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
			subMenus.AddRange(rightClickableResult.AsOrderedMenus(rightClickOptionOrder));

			//check for any components that have an attributed method. These are added to the end in whatever order,
			//doesn't matter since it's only for development.
			if (KeyboardInputManager.Instance.CheckKeyAction(KeyAction.ShowAdminOptions, KeyboardInputManager.KeyEventType.Hold))
			{
				foreach (var attributedType in attributedTypes)
				{
					var components = curObject.GetComponents(attributedType.ComponentType);
					foreach (var component in components)
					{
						//only add the item if the concrete type matches
						if (component.GetType() == attributedType.ComponentType)
						{
							//create menu items for these components
							subMenus.AddRange(CreateSubMenuOptions(attributedType, component));
						}
					}
				}

				if (!string.IsNullOrEmpty(PlayerList.Instance.AdminToken))
				{
					Action VVAction = () => RequestBookshelfNetMessage.Send(curObject, ServerData.UserID, PlayerList.Instance.AdminToken);
					subMenus.Add(VariableViewerOption.AsMenu(VVAction));
				}
			}

			if (subMenus.Count > 0)
			{
				result.Add(CreateObjectMenu(curObject, subMenus));
			}
		}

		return result;
	}

	//creates sub menu items for the specified component, hooking them up the the corresponding method on the
	//actual component
	private IEnumerable<RightClickMenuItem> CreateSubMenuOptions(RightClickAttributedComponent attributedType, Component actualComponent)
	{
		return attributedType.AttributedMethods
			.Select(m => CreateSubMenuOption(m, actualComponent));
	}

	private RightClickMenuItem CreateSubMenuOption(MethodInfo forMethod, Component actualComponent)
	{
		var rightClickMethod = forMethod.GetCustomAttribute<RightClickMethod>(true);
		return rightClickMethod.AsMenu(forMethod, actualComponent);
	}

	//creates the top-level menu item for this object. If object has a RightClickAppearance, uses that to
	//define the appearance. Otherwise sticks to defaults. Doesn't populate the sub menus though.
	private RightClickMenuItem CreateObjectMenu(GameObject forObject, List<RightClickMenuItem> subMenus)
	{
		RightClickAppearance rightClickAppearance = forObject.GetComponent<RightClickAppearance>();
		if (rightClickAppearance != null)
		{
			//use right click menu to determine appearance
			return rightClickAppearance.AsMenu(subMenus);
		}

		//use defaults
		var label = forObject.name.Replace("(clone)","");
		SpriteRenderer firstSprite = forObject.GetComponentInChildren<SpriteRenderer>();
		Sprite sprite = null;

		// check if is a paletted item
		ItemAttributesV2 item = forObject.GetComponent<ItemAttributesV2>();
		List<Color> palette = null;
		if (item != null)
		{
			if (item.ItemSprites.IsPaletted)
			{
				palette = item.ItemSprites.Palette;
			}
		}

		if (firstSprite != null)
		{
			sprite = firstSprite.sprite;
		}
		else
		{
			Logger.LogWarningFormat("Could not determine sprite to use for right click menu" +
			                        " for object {0}. Please manually configure a sprite in a RightClickAppearance component" +
			                        " on this object.", Category.UI, forObject.name);
		}

		return RightClickMenuItem.CreateObjectMenuItem(Color.gray, sprite, null, label, subMenus, firstSprite.color, palette);
	}
}
