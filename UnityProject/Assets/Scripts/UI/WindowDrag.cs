using UnityEngine;


	public class WindowDrag : MonoBehaviour
	{
		public bool resetPositionOnDisable = false;
		private float offsetX;
		private float offsetY;
		private Vector3 startPositon;
		
		void Start () {
			// Save initial window start positon
			startPositon = gameObject.transform.position;
		}
		void OnDisable () {
			// Reset window to start position
			if (resetPositionOnDisable)
			{
				gameObject.transform.position = startPositon;
			}
		}
		public void BeginDrag()
		{
			offsetX = transform.position.x - Input.mousePosition.x;
			offsetY = transform.position.y - Input.mousePosition.y;
		}

		public void OnDrag()
		{
			transform.position = new Vector3(offsetX + Input.mousePosition.x, offsetY + Input.mousePosition.y);
		}
	}
