using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UI{
	/// <summary>
	/// Custom ResizePanel for the PANEL_Right UI element
	/// </summary>
	public class RightPanelResize : ResizePanel {

		public RectTransform hudRight;
		float hudRight_dist;

		public override void OnPointerDown(PointerEventData data){
			hudRight_dist = transform.position.x - hudRight.position.x;
			base.OnPointerDown(data);
		}
		//TODO handle max size on x and hide panel when below min x (showing the transparent chatbox)
		public override void OnDrag(PointerEventData data){
			if (panelRectTransform == null)
				return;

			Vector2 localPointerPosition;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, data.position, data.pressEventCamera, out localPointerPosition);
			Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;

			Vector2 sizeDelta = originalSizeDelta + new Vector2(-offsetToOriginal.x, 0f);

			if (sizeDelta.x < maxSize.x) {

				panelRectTransform.sizeDelta = sizeDelta;
			} else {
				sizeDelta.x = maxSize.x;
				panelRectTransform.sizeDelta = sizeDelta;
			}

			Vector3 newHudRight_Pos = hudRight.position;
			newHudRight_Pos.x = transform.position.x - hudRight_dist;
			hudRight.position = newHudRight_Pos;

		}
}
}
