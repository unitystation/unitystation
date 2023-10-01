using Doors;
using Items;
using Messages.Client.VariableViewer;
using Objects.Wallmounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Logs;
using SecureStuff;
using Shared.Managers;
using Tiles;
using UI;
using UI.Core.RightClick;
using UnityEngine;
using UnityEngine.EventSystems;
using Util;

/// <summary>
/// Main logic for managing right click behavior.
///
/// There are 2 approaches for defining right click options.
/// 1. A component can implement IRightClickable to define what options should be shown based on its current state.
/// 2. (For development only) Add the RightClickMethod attribute to a method on a component.
///
/// Refer to documentation at https://github.com/unitystation/unitystation/wiki/Right-Click-Menu
/// </summary>
public class RightClickManager : SingletonManager<RightClickManager>
{
	public static readonly Color ButtonColor = new Color(0.3f, 0.55f, 0.72f, 0.7f);

	private static readonly RadialWorldPosition RadialWorldPosition = new RadialWorldPosition();

	private static readonly RadialScreenPosition RadialScreenPosition = new RadialScreenPosition();

	[SerializeField]
	private RightClickRadialOptions singleRingConfig = default;

	[SerializeField]
	private RightClickRadialOptions dualRingConfig = default;

	public RightClickRadialOptions SingleRingConfig =>
		this.VerifyNonChildReference(singleRingConfig, "single ring options SO");

	public RightClickRadialOptions DualRingConfig =>
		this.VerifyNonChildReference(dualRingConfig, "dual ring options SO");

	[SerializeField]
	private ScriptableObjects.RightClickOptionsList rightClickOptions = default;
	public RightClickOption[] RightClickOptions => rightClickOptions.RightClickOptions;

	[Tooltip("Ordering to use for right click options.")]
	public RightClickOptionOrder rightClickOptionOrder;

	[Tooltip("Having it on every item is Pretty cool so best place to put it")]
	public RightClickOption VariableViewerOption;

	/// saved reference to lighting sytem, for checking FOV occlusion
	private LightingSystem lightingSystem;

	// cached methods attributed with RightClickMethod
	private static List<RightClickAttributedComponent> attributedTypes = new List<RightClickAttributedComponent>();
	private List<RaycastResult> raycastResults = new List<RaycastResult>();


	//defines a particular component that has one or more methods which have been attributed with RightClickMethod. Cached
	// in the above list to avoid expensive lookup at click-time.
	private class RightClickAttributedComponent
	{
		public Type ComponentType;
		public List<MethodInfoAndRightClick> AttributedMethods;
	}

	public class MethodInfoAndRightClick
	{
		public MethodInfo MethodInfo;
		public RightClickMethod RightClickMethod;
	}

	[SerializeField]
	private RightClickMenuController menuControllerPrefab = default;
	[SerializeField]
	private GameObject legacyMenuControllerPrefab = default;

	[SerializeField]
	private GameObject quickSelectMenuControllerPrefab = default;

	private RightClickMenuController menuController;
	private IRightClickMenu legacyMenuController;

	private IRightClickMenu quickSelectMenuController;


	public static Dictionary<string, PreferenceRightClickOption> AvailableRightClickOptions =
		new Dictionary<string, PreferenceRightClickOption>()
		{
			{PreferenceRightClickOption.Radial.ToString(), PreferenceRightClickOption.Radial},
			{PreferenceRightClickOption.DropDown.ToString(), PreferenceRightClickOption.DropDown},
			{PreferenceRightClickOption.QuickRadial.ToString(), PreferenceRightClickOption.QuickRadial},

		};


	private PreferenceRightClickOption CurrentPreference = PreferenceRightClickOption.Radial;

	public IRightClickMenu MenuController
	{
		get
		{
			if (menuController == null)
			{
				menuController = Instantiate(menuControllerPrefab, transform);
			}

			if (legacyMenuController == null)
			{
				var legacy = Instantiate(legacyMenuControllerPrefab, transform);
				legacyMenuController = legacy.GetComponent<IRightClickMenu>();
			}

			if (quickSelectMenuController == null)
			{
				var legacy = Instantiate(quickSelectMenuControllerPrefab, transform);
				quickSelectMenuController = legacy.GetComponent<IRightClickMenu>();
			}

			switch (CurrentPreference)
			{
				case PreferenceRightClickOption.Radial:
					return menuController;
				case PreferenceRightClickOption.DropDown:
					return legacyMenuController;
				case PreferenceRightClickOption.QuickRadial:
					return quickSelectMenuController;
				default:
					return menuController;
			}
		}
	}

	public override void Awake()
	{
		base.Awake();
		// cache all known usages of the RightClickMethod annotation
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
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		GetRightClickPreference(save: true);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	public static string GetRightClickPreference(bool save = false)
	{
		var Prefere=  PlayerPrefs.GetString("RightClickPreference", AvailableRightClickOptions.Keys.First());
		if (AvailableRightClickOptions.ContainsKey(Prefere) == false)
		{
			Prefere = AvailableRightClickOptions.Keys.First();
			SetRightClickPreference(Prefere);
		}

		if (save)
		{
			SetRightClickPreference(Prefere);
		}

		return Prefere;
	}

	public static void SetRightClickPreference(string Preference)
	{
		PlayerPrefs.SetString("RightClickPreference", Preference);
		Instance.CurrentPreference = AvailableRightClickOptions[Preference];
	}

	private void GetRightClickAttributedMethods()
	{
		var result = new List<RightClickAttributedComponent>();

		var data = AllowedReflection.GetFunctionsWithAttribute<RightClickMethod>();

		foreach (var MonoBehaviourAndMethods in data)
		{
			if (MonoBehaviourAndMethods.Value.Count > 0)
			{
				RightClickAttributedComponent component = new RightClickAttributedComponent
				{
					ComponentType = MonoBehaviourAndMethods.Key,
					AttributedMethods = MonoBehaviourAndMethods.Value.Select(x => new MethodInfoAndRightClick()
					{
						MethodInfo = x.MethodInfo,
						RightClickMethod = x.Attribute

					}).ToList()

				};
				result.Add(component);
			}
		}

		attributedTypes = result;
	}

	void UpdateMe()
	{
		// Get right mouse click
		if (CommonInput.GetMouseButtonDown(1) == false) return;

		var mousePos = CommonInput.mousePosition;

		var objects = GetGameObjects(mousePos, out var isUI);
		//Generates menus
		var options = Generate(objects);

		if (options is null || options.Count <= 0) return;

		var radialOptions = DualRingConfig;

		// If there's only one object, use the inner ring as the action ring.
		if (options.Count == 1)
		{
			options = options[0].SubMenus;
			radialOptions = SingleRingConfig;
		}

		IRadialPosition radialPosition = RadialScreenPosition.SetPosition(mousePos);

		if (isUI == false)
		{
			var tile = objects.Select(o => o.RegisterTile()).FirstOrDefault();
			if (tile)
			{
				radialPosition = RadialWorldPosition.SetTile(tile);
			}
		}

		MenuController.SetupMenu(options, radialPosition, radialOptions);
	}

	private List<GameObject> GetGameObjects(Vector3 position, out bool isUI)
	{
		var pointerData = new PointerEventData(EventSystem.current)
		{
			pointerId = -1, // Mouse
			position = position
		};
		EventSystem.current.RaycastAll(pointerData, raycastResults);

		isUI = false;
		foreach (var result in raycastResults)
		{
			var go = result.gameObject;
			var itemSlot = go.GetComponent<UI_ItemSlot>();
			// Checking if the user has clicked on any ui element, so don't change if it is already true.
			isUI |= itemSlot != null;
			// Try searching for UI_ItemSwap instead for the larger hitbox.
			if (itemSlot == null)
			{
				var itemSwap = go.GetComponent<UI_ItemSwap>();
				itemSlot = itemSwap.OrNull()?.GetComponentInChildren<UI_ItemSlot>();
				// It doesn't matter if there is anything in the itemSlot, we just need to know if the ItemSwap was clicked on.
				isUI |= itemSwap != null;
			}
			var slotObject = itemSlot.OrNull()?.ItemObject;
			if (slotObject != null)
			{
				return new List<GameObject> { slotObject };
			}
		}

		// If the user has clicked an empty UI element, don't return the items that are underneath the UI.
		return isUI ? null : GetRightClickableObjects(position);
	}

	private List<GameObject> GetRightClickableObjects(Vector3 mousePosition)
	{
		if (lightingSystem.enabled && !lightingSystem.IsScreenPointVisible(mousePosition))
		{
			return null;
		}

		var position = MouseUtils.MouseToWorldPos();
		var objects = UITileList.GetItemsAtPosition(position);

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
		LayerTile tile = UITileList.GetTileAtPosition(obj.AssumedWorldPosServer());

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
					subMenus.Add(VariableViewerOption.AsMenu(() =>
					{
						if (UIManager.Instance.LibraryUI.Roots.Count == 0)
						{
							RequestBookshelfNetMessage.Send(curObject, true);
						}
						else
						{
							RequestBookshelfNetMessage.Send(curObject, false);
						}

					}));
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
			.Select(m => CreateSubMenuOption(m.MethodInfo, actualComponent, m.RightClickMethod));
	}

	private RightClickMenuItem CreateSubMenuOption(MethodInfo forMethod, Component actualComponent,  RightClickMethod rightClickMethod)
	{
		return rightClickMethod.AsMenu(forMethod, actualComponent);
	}

	//creates the top-level menu item for this object. If object has a RightClickAppearance, uses that to
	//define the appearance. Otherwise sticks to defaults. Doesn't populate the sub menus though.
	public static RightClickMenuItem CreateObjectMenu(GameObject forObject, List<RightClickMenuItem> subMenus, Action action = null)
	{
		RightClickAppearance rightClickAppearance = forObject.GetComponent<RightClickAppearance>();
		if (rightClickAppearance != null)
		{
			//use right click menu to determine appearance
			return rightClickAppearance.AsMenu(subMenus);
		}
		// else use defaults:

		var label = forObject.ExpensiveName();

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

		// See if this object has an AirLockAnimator then try to get the sprite from that, otherwise try to get the sprite from the first renderer we find
		var airLockAnimator = forObject.GetComponentInChildren<AirLockAnimator>();
		var spriteRenderer = airLockAnimator != null ? airLockAnimator.doorbase : forObject.GetComponentInChildren<SpriteRenderer>();

		Sprite sprite = null;
		if (spriteRenderer != null)
		{
			sprite = spriteRenderer.sprite;
		}
		else
		{
			Loggy.LogWarningFormat("Could not determine sprite to use for right click menu" +
					" for object {0}. Please manually configure a sprite in a RightClickAppearance component" +
					" on this object.", Category.UserInput, forObject.name);
		}

		return new RightClickMenuItem(sprite, spriteRenderer.color, null, ButtonColor,
			label, subMenus, action, null, palette, false);
	}
}

public enum PreferenceRightClickOption
{
	Radial,
	DropDown,
	QuickRadial
}
