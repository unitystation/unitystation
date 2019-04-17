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
/// When dropped, any MouseDrop components on the dropped object will be fired and any MouseDropTarget components
/// on the topmost object under the mouse will be fired as well.
///
/// </summary>
public class MouseDraggable : MonoBehaviour
{
	[Tooltip("Layers onto which this object can be dropped.")]
	public LayerMask dropLayers;

	[Tooltip("Sprite to draw under mouse when dragging. If unspecified, will use the first " +
	         "SpriteRenderer encountered on this object or its children")]
	public Sprite shadow;

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
		//check what we dropped on, which may or may not have mousedrop interaction components
		//can only drop on things that have a RegisterTile
		var dropTargets =
			MouseUtils.GetOrderedObjectsUnderMouse(dropLayers, go => go.GetComponent<RegisterTile>() != null)
				//get the root gameobject of the dropped-on sprite renderer
				.Select(sr => sr.GetComponentInParent<RegisterTile>().gameObject)
				//only want distinct game objects even if we hit multiple renderers on one object.
				.Distinct();

		//go through the stack of objects and call any drop components we find
		foreach (GameObject dropTarget in dropTargets)
		{
			MouseDrop info = new MouseDrop(PlayerManager.LocalPlayer, gameObject, dropTarget.gameObject);
			//call this object's mousedrop interaction methods if it has any, for each object we are dropping on
			foreach (IInteractable<MouseDrop> mouseDrop in mouseDrops)
			{
				var result = mouseDrop.Interact(info);
				if (result.SomethingHappened)
				{
					//we're done checking, something happened
					return;
				}
			}

			//call the mousedrop interaction methods on the dropped-on object if it has any
			foreach (IInteractable<MouseDrop> mouseDropTarget in dropTarget.GetComponents<IInteractable<MouseDrop>>())
			{
				var result = mouseDropTarget.Interact(info);
				if (result.SomethingHappened)
				{
					//something happened, done checking
					return;
				}
			}
		}
	}
}
