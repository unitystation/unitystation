using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Put this on an object to allow it to be dragged by the mouse in the game world. This does
/// not mean that any sort of interaction will occur when dropping the object on something,
/// it merely allows the object to be dragged by the mouse.
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
	[Tooltip("Layers onto which this object can be dropped.")]
	public LayerMask dropLayers;

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
	private IInteractable<MouseDrop>[] mouseDrops;

	void Start()
	{
		mouseDrops = GetComponents<IInteractable<MouseDrop>>();
		shadowPrefab = Resources.Load<GameObject>("MouseDragShadow");
		if (shadow == null)
		{
			shadow = GetComponentInChildren<SpriteRenderer>()?.sprite;
			if (shadow == null)
			{
				Logger.LogWarning("No drag shadow sprite was set and no sprite renderer found for " + name +
				                  " so there will be no drag shadow for this object.");
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
		//shadowObject.transform.localScale -= new Vector3(0.5f,0.5f, 0);
	}

	private void Update()
	{
		if (shadowObject == null)
		{
			return;
		}

		shadowObject.transform.position = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);

		if (CommonInput.GetMouseButtonUp(0))
		{
			OnDragEnd();
		}
	}

	private void OnDragEnd()
	{
		UIManager.IsMouseInteractionDisabled = false;
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
			MouseUtils.GetOrderedObjectsUnderMouse(dropLayers);

		//go through the stack of objects and call any drop components we find
		foreach (GameObject dropTarget in dropTargets)
		{
			MouseDrop info = MouseDrop.ByLocalPlayer( gameObject, dropTarget.gameObject);
			//call this object's mousedrop interaction methods if it has any, for each object we are dropping on
			foreach (IInteractable<MouseDrop> mouseDrop in mouseDrops)
			{
				var result = mouseDrop.Interact(info);
				if (result.StopProcessing)
				{
					//we're done checking, something happened
					return;
				}
			}

			//call the mousedrop interaction methods on the dropped-on object if it has any
			foreach (IInteractable<MouseDrop> mouseDropTarget in dropTarget.GetComponents<IInteractable<MouseDrop>>())
			{
				var result = mouseDropTarget.Interact(info);
				if (result.StopProcessing)
				{
					//something happened, done checking
					return;
				}
			}
		}
	}

	/// <summary>
	/// Checks if the drag can be performed by this dragger
	/// </summary>
	/// <param name="dragger">player attempting the drag</param>
	/// <returns></returns>
	public bool CanBeginDrag(GameObject dragger)
	{
		return CanApply.Validate(dragger, gameObject, allowDragWhileSoftCrit,
			//always client side
			NetworkSide.CLIENT,
			draggerMustBeAdjacent ? ReachRange.STANDARD : ReachRange.UNLIMITED) == ValidationResult.SUCCESS;
	}
}
