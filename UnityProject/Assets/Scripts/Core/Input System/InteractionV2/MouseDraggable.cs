using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;

/// <summary>
/// Put this on an object to allow it to be dragged by the mouse in the game world (while it's
/// not in inventory). This does
/// not mean that any sort of interaction will occur when dropping the object on something,
/// it merely allows the object to be dragged by the mouse. All objects in inventory can
/// also be dragged out into the world so this is not needed unless the object needs to be draggable
/// while it's outside of inventory.
///
/// Upon being dragged, a sprite is placed under the mouse during the drag action indicating the
/// object being dragged. It can then be released to "drop" the object on another object.
///
/// When dropped, any MouseDrop interaction components on the dropped object as well as the target object
/// will be invoked, stopping after the first one that returns InteractionResult.SOMETHING_HAPPENED.
///
/// </summary>
public class MouseDraggable : MonoBehaviour
{
	[Tooltip("Sprite to draw under mouse when dragging. If unspecified, will use the first " +
	         "SpriteRenderer encountered on this object or its children")]
	public Sprite shadow;

	[Tooltip("If true, the player attempting to drag must be adjacent to the this in order" +
	         " to be able to begin the drag")]
	public bool draggerMustBeAdjacent = true;

	[Tooltip("If true, drag may be initiated even when in soft crit. If false, player must be fully" +
	         " conscious in order to drag.")]
	public bool allowDragWhileSoftCrit = true;

	private LightingSystem lightingSystem;

	//the currently active shadow object
	private GameObject shadowObject;
	//prefab to use to create the shadow object
	private GameObject shadowPrefab;

	//cached list of MouseDrop interaction components on this object (may be empty)
	private IBaseInteractable<MouseDrop>[] mouseDrops;

	[SerializeField]
	private PlayerTypes allowedToMouseDrag = PlayerTypes.Normal;

	private bool BeingDragged = false;

	void Start()
	{
		mouseDrops = GetComponents<IBaseInteractable<MouseDrop>>();
		shadowPrefab = Resources.Load<GameObject>("MouseDragShadow");
		if (shadow == null)
		{
			shadow = GetComponentInChildren<SpriteRenderer>()?.sprite;
			if (shadow == null)
			{
				Loggy.LogWarning("No drag shadow sprite was set and no sprite renderer found for " + name +
				                  " so there will be no drag shadow for this object.", Category.Sprites);
			}
		}
		lightingSystem = Camera.main.GetComponent<LightingSystem>();
	}

	/// <summary>
	/// Invoked by MouseInputController when a drag starts on this object
	/// </summary>
	public void BeginDrag()
	{
		//no more mouse interaction
		UIManager.IsMouseInteractionDisabled = true;
		//create the shadow
		shadowObject = Instantiate(shadowPrefab);
		shadowObject.GetComponent<SpriteRenderer>().sprite = shadow;
		BeingDragged = true;
		//shadowObject.transform.localScale -= new Vector3(0.5f,0.5f, 0);
	}

	private void LateUpdate()
	{
		if(CustomNetworkManager.IsHeadless) return;

		if (BeingDragged == false)
		{
			return;
		}

		if (CommonInput.GetMouseButtonUp(0))
		{
			OnDragEnd();
		}

		if (shadowObject != null)
		{
			var transformPosition = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);
			transformPosition.z = 1;
			shadowObject.transform.position = transformPosition;
		}
	}

	private void OnDragEnd()
	{
		UIManager.IsMouseInteractionDisabled = false;
		BeingDragged = false;
		// Get the world position of the shadow object before destroying it.
		var shadowLoc = shadowObject.transform.position;


		Destroy(shadowObject);
		shadowObject = null;
		if (lightingSystem.enabled && !lightingSystem.IsScreenPointVisible(CommonInput.mousePosition))
		{
			//do nothing, the point is not visible.
			return;
		}
		//check what we dropped on, which may or may not have mousedrop interaction components
		//can only drop on things that have a RegisterTile
		var dropTargets =
			MouseUtils.GetOrderedObjectsUnderMouse();

		//go through the stack of objects and call any drop components we find
		foreach (GameObject dropTarget in dropTargets)
		{
			MouseDrop info = MouseDrop.ByLocalPlayer( gameObject, dropTarget.gameObject);
			info.ShadowWorldLocation = shadowLoc;
			//info.
			//call this object's mousedrop interaction methods if it has any, for each object we are dropping on
			if (InteractionUtils.ClientCheckAndTrigger(mouseDrops, info) != null) return;
			var targetComps = dropTarget.GetComponents<IBaseInteractable<MouseDrop>>()
				.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
			if (InteractionUtils.ClientCheckAndTrigger(targetComps, info) != null) return;
		}
	}

	/// <summary>
	/// Checks if the drag can be performed by this dragger
	/// </summary>
	/// <param name="dragger">player attempting the drag</param>
	/// <returns></returns>
	public bool CanBeginDrag(PlayerScript dragger)
	{
		return Validations.CanApply(dragger, gameObject, NetworkSide.Client, allowDragWhileSoftCrit,
			draggerMustBeAdjacent ? ReachRange.Standard : ReachRange.Unlimited, apt: allowedToMouseDrag);
	}

	public void OnDestroy()
	{
		if (BeingDragged)
		{
			OnDragEnd();
		}
	}

	public void OnDisable()
	{
		if (BeingDragged)
		{
			OnDragEnd();
		}
	}
}
