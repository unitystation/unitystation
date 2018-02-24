using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
	/// <summary>
	///     Custom ResizePanel for the PANEL_Right UI element
	/// </summary>
	public class RightPanelResize : ResizePanel
	{
		private float hudAspect;

		public RectTransform hudRight;
		private float hudRight_dist;
		private float leftRange;
		[HideInInspector] public Vector2 originalHudSize;
		public RectTransform panelRight;
		public ResponsiveUI responsiveControl;
		public GameObject returnPanelButton;
		private float rightRange;

		public float cacheHudAnchor { get; set; }

		private void Start()
		{
			leftRange = maxSize.x - responsiveControl.cacheWidth;
			rightRange = responsiveControl.cacheWidth - minSize.x;
			originalHudSize = responsiveControl.hudBottom.sizeDelta;
			originalHudSize.x = 1920f - Mathf.Abs(originalHudSize.x);
			hudAspect = originalHudSize.x / originalHudSize.y;
			cacheHudAnchor = Mathf.Abs(responsiveControl.hudBottom.anchoredPosition.x);
		}

		public override void OnPointerDown(PointerEventData data)
		{
			hudRight_dist = transform.position.x - hudRight.position.x;
			base.OnPointerDown(data);
		}

		//TODO showing the transparent chatbox when panel is hidden
		public override void OnDrag(PointerEventData data)
		{
			if (panelRectTransform == null || !isDragging)
			{
				return;
			}

			Vector2 localPointerPosition;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, data.position,
				data.pressEventCamera, out localPointerPosition);
			Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;

			Vector2 sizeDelta = originalSizeDelta + new Vector2(-offsetToOriginal.x, 0f);

			if (sizeDelta.x < maxSize.x)
			{
				panelRectTransform.sizeDelta = sizeDelta;
			}
			else
			{
				sizeDelta.x = maxSize.x;
				panelRectTransform.sizeDelta = sizeDelta;
			}

			if (sizeDelta.x < minSize.x)
			{
				returnPanelButton.SetActive(true);
				sizeDelta.x = -2f;
				panelRectTransform.sizeDelta = sizeDelta;
				isDragging = false;
				Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
				//this part below ensures that the hudBottom is at the correct size
				//it fixes a bug where the user resizes rightpanel super fast and it misses the width adjustment
				//of hudBottom
				Vector2 hudBottomSizeDelta = responsiveControl.hudBottom.sizeDelta;
				hudBottomSizeDelta.x = -responsiveControl.cacheWidth;
				responsiveControl.hudBottom.sizeDelta = hudBottomSizeDelta;
			}

			AdjustHudRight();
			responsiveControl.AdjustHudBottom(sizeDelta);
		}

		private void AdjustHudRight()
		{
			Vector3 newHudRight_Pos = hudRight.position;
			newHudRight_Pos.x = transform.position.x - hudRight_dist;
			hudRight.position = newHudRight_Pos;
		}

		/// <summary>
		///     To restore the RightPanel by clicking the arrow button in the top right of the screen
		/// </summary>
		public void RestoreRightPanel()
		{
			SoundManager.Play("Click01");
			returnPanelButton.SetActive(false);
			Vector2 sizeDelta = panelRectTransform.sizeDelta;
			sizeDelta.x = responsiveControl.cacheWidth;
			panelRectTransform.sizeDelta = sizeDelta;
			AdjustHudRight();
			responsiveControl.AdjustHudBottom(sizeDelta);
		}
	}
}