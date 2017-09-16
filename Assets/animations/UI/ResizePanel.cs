using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
	public class ResizePanel : MonoBehaviour, IPointerDownHandler, IDragHandler
	{
		//TODO handle max size on x and hide panel when below min x (showing the transparent chatbox)
		public Vector2 minSize = new Vector2(100, 100);
		public Vector2 maxSize = new Vector2(400, 400);

		private RectTransform panelRectTransform;
		private RectTransform thisRectTransform;
		private Vector2 originalLocalPointerPosition;
		private Vector2 originalSizeDelta;
		private Rect originalRect;

		void Awake()
		{
			panelRectTransform = transform.parent.GetComponent<RectTransform>();
			thisRectTransform = transform.GetComponent<RectTransform>();
		}

		public void OnPointerDown(PointerEventData data)
		{
			originalSizeDelta = panelRectTransform.sizeDelta;
			originalRect = panelRectTransform.rect;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, data.position, data.pressEventCamera, out originalLocalPointerPosition);

		}

		public void OnDrag(PointerEventData data)
		{
			if (panelRectTransform == null)
				return;

			Vector2 localPointerPosition;
			RectTransformUtility.ScreenPointToLocalPointInRectangle (panelRectTransform, data.position, data.pressEventCamera, out localPointerPosition);
			Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;

			Vector2 sizeDelta = originalSizeDelta + new Vector2 (-offsetToOriginal.x, originalSizeDelta.y);
			//TODO handle the min and max actions
//			sizeDelta = new Vector2 (
//				Mathf.Clamp (sizeDelta.x, minSize.x, maxSize.x),
//				Mathf.Clamp (sizeDelta.y, minSize.y, maxSize.y)
//			);

			panelRectTransform.sizeDelta = sizeDelta;
		}
	}
}
