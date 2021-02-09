using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace IngameDebugConsole
{
	/// <summary>
	/// Manager class for the debug popup
	/// </summary>
	public class DebugLogPopup : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		private RectTransform popupTransform;

		/// <summary>
		/// Dimensions of the popup divided by 2
		/// </summary>
		private Vector2 halfSize;

		/// <summary>
		/// Background image that will change color to indicate an alert
		/// </summary>
		public Image backgroundImage;

		/// <summary>
		/// Canvas group to modify visibility of the popup
		/// </summary>
		private CanvasGroup canvasGroup;

		[SerializeField]
		private DebugLogManager debugManager = null;

		[SerializeField]
		private Text newInfoCountText = null;
		[SerializeField]
		private Text newWarningCountText = null;
		[SerializeField]
		private Text newErrorCountText = null;

		/// <summary>
		/// Number of new debug entries since the log window has been closed
		/// </summary>
		private int newInfoCount = 0, newWarningCount = 0, newErrorCount = 0;

		private Color normalColor;

		// Ignore default color warning
#pragma warning disable CS0649
		[SerializeField]
		private Color alertColorInfo;
		[SerializeField]
		private Color alertColorWarning;
		[SerializeField]
		private Color alertColorError;
#pragma warning disable CS0649

		public bool isLogPopupVisible = false;
		private bool isPopupBeingDragged = false;

		/// <summary>
		/// Coroutines for simple code-based animations
		/// </summary>
		private IEnumerator moveToPosCoroutine = null;

		void Awake()
		{
			popupTransform = (RectTransform) transform;
			canvasGroup = GetComponent<CanvasGroup>();

			normalColor = backgroundImage.color;
		}

		void Start()
		{
			halfSize = popupTransform.sizeDelta * 0.5f * popupTransform.root.localScale.x;
		}

		public void OnViewportDimensionsChanged()
		{
			halfSize = popupTransform.sizeDelta * 0.5f * popupTransform.root.localScale.x;
			OnEndDrag( null );
		}

		public void NewInfoLogArrived()
		{
			newInfoCount++;
			newInfoCountText.text = newInfoCount.ToString();

			if( newWarningCount == 0 && newErrorCount == 0 )
				backgroundImage.color = alertColorInfo;
		}

		public void NewWarningLogArrived()
		{
			newWarningCount++;
			newWarningCountText.text = newWarningCount.ToString();

			if( newErrorCount == 0 )
				backgroundImage.color = alertColorWarning;
		}

		public void NewErrorLogArrived()
		{
			newErrorCount++;
			newErrorCountText.text = newErrorCount.ToString();

			backgroundImage.color = alertColorError;
		}

		private void Reset()
		{
			newInfoCount = 0;
			newWarningCount = 0;
			newErrorCount = 0;

			newInfoCountText.text = "0";
			newWarningCountText.text = "0";
			newErrorCountText.text = "0";

			backgroundImage.color = normalColor;
		}

		/// <summary>
		/// A simple smooth movement animation
		/// </summary>
		/// <param name="targetPos">3D location to move animation to</param>
		private IEnumerator MoveToPosAnimation( Vector3 targetPos )
		{
			float modifier = 0f;
			Vector3 initialPos = popupTransform.position;

			while( modifier < 1f )
			{
				modifier += 4f * Time.unscaledDeltaTime;
				popupTransform.position = Vector3.Lerp( initialPos, targetPos, modifier );

				yield return null;
			}
		}

		/// <summary>
		/// Popup is clicked
		/// </summary>
		public void OnPointerClick( PointerEventData data )
		{
			// Hide the popup and show the log window
			if(isPopupBeingDragged == false)
			{
				debugManager.Show();
				Hide();
			}
		}

		/// <summary>
		/// Show the popup
		/// </summary>
		public void Show()
		{
			canvasGroup.interactable = true;
			canvasGroup.blocksRaycasts = true;
			canvasGroup.alpha = 1f;

			isLogPopupVisible = true;

			// Reset the counters
			Reset();

			// Update position in case resolution changed while hidden
			OnViewportDimensionsChanged();
		}

		/// <summary>
		/// Show the popup without resetting the counter
		/// </summary>
		/// <remarks>
		///	This is needed to prevent the counter from being reset after hitting F5 to hide the popup
		/// </remarks>
		public void ShowWithoutReset()
		{
			canvasGroup.interactable = true;
			canvasGroup.blocksRaycasts = true;
			canvasGroup.alpha = 1f;

			isLogPopupVisible = true;

			// Update position in case resolution changed while hidden
			OnViewportDimensionsChanged();
		}

		/// <summary>
		/// Hide the popup
		/// </summary>
		public void Hide()
		{
			canvasGroup.interactable = false;
			canvasGroup.blocksRaycasts = false;
			canvasGroup.alpha = 0f;

			isLogPopupVisible = false;
		}

		public void OnBeginDrag( PointerEventData data )
		{
			isPopupBeingDragged = true;

			// If a smooth movement animation is in progress, cancel it
			if( moveToPosCoroutine != null )
			{
				StopCoroutine( moveToPosCoroutine );
				moveToPosCoroutine = null;
			}
		}

		/// <summary>
		/// Reposition the popup
		/// </summary>
		/// <param name="data"></param>
		public void OnDrag( PointerEventData data )
		{
			popupTransform.position = data.position;
		}

		/// <summary>
		/// Smoothly translate the popup to the nearest edge
		/// </summary>
		public void OnEndDrag( PointerEventData data )
		{
			int screenWidth = Screen.width;
			int screenHeight = Screen.height;

			Vector3 pos = popupTransform.position;

			// Find distances to all four edges
			float distToLeft = pos.x;
			float distToRight = Mathf.Abs( pos.x - screenWidth );

			float distToBottom = Mathf.Abs( pos.y );
			float distToTop = Mathf.Abs( pos.y - screenHeight );

			float horDistance = Mathf.Min( distToLeft, distToRight );
			float vertDistance = Mathf.Min( distToBottom, distToTop );

			// Find the nearest edge's coordinates
			if( horDistance < vertDistance )
			{
				if( distToLeft < distToRight )
					pos = new Vector3( halfSize.x, pos.y, 0f );
				else
					pos = new Vector3( screenWidth - halfSize.x, pos.y, 0f );

				pos.y = Mathf.Clamp( pos.y, halfSize.y, screenHeight - halfSize.y );
			}
			else
			{
				if( distToBottom < distToTop )
					pos = new Vector3( pos.x, halfSize.y, 0f );
				else
					pos = new Vector3( pos.x, screenHeight - halfSize.y, 0f );

				pos.x = Mathf.Clamp( pos.x, halfSize.x, screenWidth - halfSize.x );
			}

			// If another smooth movement animation is in progress, cancel it
			if( moveToPosCoroutine != null )
				StopCoroutine( moveToPosCoroutine );

			// Smoothly translate the popup to the specified position
			moveToPosCoroutine = MoveToPosAnimation( pos );
			StartCoroutine( moveToPosCoroutine );

			isPopupBeingDragged = false;
		}
	}
}