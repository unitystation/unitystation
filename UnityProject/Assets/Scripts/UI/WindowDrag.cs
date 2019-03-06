using UnityEngine;


	public class WindowDrag : MonoBehaviour
	{
		public bool resetPositionOnDisable = false;
		private float offsetX;
		private float offsetY;
		private Vector3 startPositon;
		private RectTransform rectTransform;

		void Start () {
			// Save initial window start positon
			startPositon = gameObject.transform.position;
			rectTransform = GetComponent<RectTransform>();
		}
		void OnDisable () {
			// Reset window to start position
			if (resetPositionOnDisable)
			{
				gameObject.transform.position = startPositon;
			}
		}

		/// <summary>
		/// Sets the windowDrag fields offsetX and offsetY from the window position and the mouse position.
        /// The fields offsetX and offsetY are the mouse position's offset from the window's top-left corner.
		/// In onDrag(), these offsets are used to "hook" the window to the cursor as it is dragged.
		/// </summary>
		public void BeginDrag()
		{
			var windowTransformPosition = transform.position;

			offsetX = windowTransformPosition.x - Input.mousePosition.x;
			offsetY = windowTransformPosition.y - Input.mousePosition.y;
		}

		/// <summary>
		/// Moves the window with the cursor within the screen bounds when called.
		/// </summary>
		public void OnDrag()
		{
			var windowSize = rectTransform.sizeDelta;
			var windowScale = rectTransform.lossyScale;

			var windowWidth = windowSize.x;
			var windowHeight = windowSize.y;

			var widthScale = windowScale.x;
			var heightScale = windowScale.y;

			transform.position = new Vector3(
				Mathf.Clamp(offsetX + Input.mousePosition.x,
					windowWidth * widthScale / 2f,
					Screen.width - windowWidth * widthScale / 2f),
				Mathf.Clamp(offsetY + Input.mousePosition.y,
					windowHeight * heightScale / 2f,
					Screen.height - windowHeight * heightScale / 2f));
		}
	}
