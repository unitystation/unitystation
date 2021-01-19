using UnityEngine;
using UnityEngine.EventSystems;


/// <summary>
	///     Resize a UI element, requires an Image to define draggable area
	///     Add to a child gObj of the element you want to resize
	/// </summary>
	public class ResizePanel : MonoBehaviour, IPointerDownHandler,
		IPointerUpHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
	{
		public Vector2 maxSize = new Vector2(400, 400);
		public Vector2 minSize = new Vector2(100, 100);
		public Texture2D resizeCursor;

		public RectTransform panelRectTransform { get; set; }
		public RectTransform thisRectTransform { get; set; }
		public Vector2 originalLocalPointerPosition { get; set; }
		public Vector2 originalSizeDelta { get; set; }
		public bool isDragging { get; set; }

		public virtual void OnDrag(PointerEventData data)
		{
			if (panelRectTransform == null)
			{
				return;
			}

			Vector2 localPointerPosition;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, data.position,
				data.pressEventCamera, out localPointerPosition);
			Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;

			Vector2 sizeDelta = originalSizeDelta + new Vector2(-offsetToOriginal.x, offsetToOriginal.y);

			sizeDelta = new Vector2(
				Mathf.Clamp(sizeDelta.x, minSize.x, maxSize.x),
				Mathf.Clamp(sizeDelta.y, minSize.y, maxSize.y)
			);


			panelRectTransform.sizeDelta = sizeDelta;
		}

		public virtual void OnPointerDown(PointerEventData data)
		{
			originalSizeDelta = panelRectTransform.sizeDelta;
			Vector2 getLocalPos;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, data.position,
				data.pressEventCamera, out getLocalPos);
			originalLocalPointerPosition = getLocalPos; //So child classes can use as you cannot 'out' to a property
			isDragging = true;
		}

		public void OnPointerEnter(PointerEventData data)
		{
			//TODO corner cursor textures?
			Cursor.SetCursor(resizeCursor, Vector2.zero, CursorMode.Auto);
		}

		public void OnPointerExit(PointerEventData data)
		{
			if (isDragging == false)
			{
				Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
			}
		}

		public virtual void OnPointerUp(PointerEventData data)
		{
			isDragging = false;
			Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
		}

		private void Awake()
		{
			panelRectTransform = transform.parent.GetComponent<RectTransform>();
			thisRectTransform = transform.GetComponent<RectTransform>();
		}
	}
