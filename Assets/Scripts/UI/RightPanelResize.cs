using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UI{
	/// <summary>
	/// Custom ResizePanel for the PANEL_Right UI element
	/// FIXME: Currently also controlling game screen responsiveness
	/// FIXME: move screen responsive beaviour to display manager (when it is finished)
	/// </summary>
	public class RightPanelResize : ResizePanel {

		public RectTransform hudRight;
		public RectTransform hudBottom;
		public RectTransform panelRight;
		public GameObject returnPanelButton;
		public RectTransform overlayCrit;
		float hudRight_dist;
		float leftRange;
		float rightRange;
		float cacheWidth;
		Vector2 originalHudSize;
		float hudAspect;
		bool monitorWindow;
		float screenWidthCache;
		float screenHeightCache;
		float cacheHudAnchor;
		bool isFullScreen = false;

		void Start(){
			cacheWidth = panelRectTransform.sizeDelta.x;
			leftRange = maxSize.x - cacheWidth;
			rightRange = cacheWidth - minSize.x;
			originalHudSize = hudBottom.sizeDelta;
			originalHudSize.x = 1920f - Mathf.Abs(originalHudSize.x);
			hudAspect = originalHudSize.x / originalHudSize.y;
			cacheHudAnchor = Mathf.Abs(hudBottom.anchoredPosition.x);
			StartCoroutine(WaitForDisplay());
		}

		IEnumerator WaitForDisplay(){
			yield return new WaitForSeconds(0.2f);
			screenWidthCache = Screen.width;
			screenHeightCache = Screen.height;
			AdjustHudBottom(panelRectTransform.sizeDelta);
			monitorWindow = true;
		}

		private void Update()
		{
			//Check if window has changed and adjust the bottom hud
			if(monitorWindow){
				if(screenWidthCache != Screen.width ||
				   screenHeightCache != Screen.height){
					Invoke("AdjustHudBottomDelay", 0.1f);
					screenWidthCache = Screen.width;
					screenHeightCache = Screen.height;
				}

				if (isFullScreen != Screen.fullScreen){
					isFullScreen = Screen.fullScreen;
					Invoke("AdjustHudBottomDelay", 0.1f);
				}
			}
		}

		//It takes some time for the screen to redraw, wait for 0.1f
		void AdjustHudBottomDelay(){
			AdjustHudBottom(panelRectTransform.sizeDelta);
		}


		public override void OnPointerDown(PointerEventData data){
			hudRight_dist = transform.position.x - hudRight.position.x;
			base.OnPointerDown(data);
		}
		//TODO showing the transparent chatbox when panel is hidden
		public override void OnDrag(PointerEventData data){
			if (panelRectTransform == null || !isDragging)
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

			if (sizeDelta.x < minSize.x) {
				returnPanelButton.SetActive(true);
				sizeDelta.x = -2f;
				panelRectTransform.sizeDelta = sizeDelta;
				isDragging = false;
				Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
				//this part below ensures that the hudBottom is at the correct size
				//it fixes a bug where the user resizes rightpanel super fast and it misses the width adjustment
				//of hudBottom
				Vector2 hudBottomSizeDelta = hudBottom.sizeDelta;
				hudBottomSizeDelta.x = -cacheWidth;
				hudBottom.sizeDelta = hudBottomSizeDelta;
			}

			AdjustHudRight();
			AdjustHudBottom(sizeDelta);
		}

		void AdjustHudRight(){
			Vector3 newHudRight_Pos = hudRight.position;
			newHudRight_Pos.x = transform.position.x - hudRight_dist;
			hudRight.position = newHudRight_Pos;
		}

		void AdjustHudBottom(Vector2 panelRightSizeDelta){
			Vector2 hudBottomSizeDelta = hudBottom.sizeDelta;
			//This is for pulling the rightpanel to the right of the screen from default position
			if (panelRectTransform.sizeDelta.x < cacheWidth) {
				//Calculate the new anchor point for hudBottom in the right direction of scale
				float panelRightProgress = (cacheWidth - panelRight.rect.width) / cacheWidth;
				float newAnchorPos = Mathf.Lerp(cacheHudAnchor, 0f, panelRightProgress);
				Vector2 anchoredPos = hudBottom.anchoredPosition;
				anchoredPos.x = -newAnchorPos;
				hudBottom.anchoredPosition = anchoredPos;

			} else { // this is for the left direction from the default position
				float panelRightProgress = (cacheWidth - panelRight.rect.width) / cacheWidth;
				float newAnchorPos = Mathf.Lerp(cacheHudAnchor, 562f, Mathf.Abs(panelRightProgress));
				Vector2 anchoredPos = hudBottom.anchoredPosition;
				anchoredPos.x = -newAnchorPos;
				hudBottom.anchoredPosition = anchoredPos;
				hudBottomSizeDelta.x = -panelRightSizeDelta.x;
				hudBottom.sizeDelta = hudBottomSizeDelta;
			}
			//KEEP ASPECT RATIO:
			hudBottomSizeDelta.y = (hudBottom.rect.width) * originalHudSize.y / originalHudSize.x;
			hudBottom.sizeDelta = hudBottomSizeDelta;
			UIManager.DisplayManager.SetCameraFollowPos(returnPanelButton.activeSelf);
			UIManager.PlayerHealthUI.overlayCrits.AdjustOverlayPos();
		}

		/// <summary>
		/// To restore the RightPanel by clicking the arrow button in the top right of the screen
		/// </summary>
		public void RestoreRightPanel(){
			SoundManager.Play("Click01");
			returnPanelButton.SetActive(false);
			Vector2 sizeDelta = panelRectTransform.sizeDelta;
			sizeDelta.x = cacheWidth;
			panelRectTransform.sizeDelta = sizeDelta;
			AdjustHudRight();
			AdjustHudBottom(sizeDelta);
		}
}
}
