using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WindowDragUIIinteract : WindowDrag
{
	public class EventRaycastResults : UnityEvent<List<RaycastResult>> { }

	public EventRaycastResults OnDropTarget = new EventRaycastResults();

	public override void DragEnd()
	{
		List<RaycastResult> resultAppendList = new List<RaycastResult>();
		var GraphicRaycaster = UIManager.Instance.gameObject.GetComponent<GraphicRaycaster>();
		GraphicRaycaster.Raycast(new PointerEventData(null){position = new Vector2(CommonInput.mousePosition.x, CommonInput.mousePosition.y) }, resultAppendList);
		OnDropTarget.Invoke(resultAppendList);
	}
}
