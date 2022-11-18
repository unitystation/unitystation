using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.Tooltips.HoverTooltips
{
	public class HoverTooltipUI : MonoBehaviour
	{
		[SerializeField] private CanvasGroup content;
		[SerializeField] private Transform interactionList;
		[SerializeField] private Transform interactionPrefab;
		[SerializeField] private TMP_Text nameText;
		[SerializeField] private TMP_Text descText;
		[SerializeField] private Image iconTarget;

		private GameObject targetObject;
		private bool detailsModeEnabled = false;

		private const float MOUSE_OFFSET_Y = -105f;
		private const float MOUSE_OFFSET_X = -125f;
		private const float ANIM_TIME = 0.2f;
		private const float ICON_SIZE_HEIGHT_DEFAULT = 45f;


		private void Start()
		{
			UpdateManager.Add(CallbackType.FIXED_UPDATE, UpdatePosition);
			UpdateManager.Add(CallbackType.UPDATE, CheckForInput);
			ResetTool();
		}

		private void UpdatePosition()
		{
			var newPosition = new Vector3(Input.mousePosition.x + MOUSE_OFFSET_X, Input.mousePosition.y + MOUSE_OFFSET_Y,
				Input.mousePosition.z);
			transform.position = newPosition;
		}

		private void CheckForInput()
		{
			var lastState = detailsModeEnabled;
			detailsModeEnabled = Input.GetKeyDown(KeyCode.LeftShift);
			if (lastState != detailsModeEnabled && detailsModeEnabled && targetObject != null) SetupTooltip(targetObject);
		}

		public void SetupTooltip(GameObject hoverObject)
		{
			targetObject = hoverObject;
			ResetTool();
			if (hoverObject == null) return;
			UpdateMainInfo(hoverObject);
			CaptureIconFromSpriteHandler(hoverObject);
			if(detailsModeEnabled) UpdateDetailedView(hoverObject);
			// Don't show if the description/name is empty.
			if (String.IsNullOrEmpty(nameText.text) || String.IsNullOrEmpty(descText.text)) return;

			content.LeanAlpha(1f, ANIM_TIME);
		}

		private void CaptureIconFromSpriteHandler(GameObject target)
		{
			var imageObj = targetObject.GetComponentInChildren<SpriteHandler>();
			if (imageObj != null)
			{
				iconTarget.sprite = imageObj.CurrentSprite;
			}
		}

		private void UpdateIconSprite(IHoverTooltip target)
		{
			if (target.CustomIcon() == null) return;
			iconTarget.sprite = target.CustomIcon();
		}

		private void UpdateMainInfo(GameObject target)
		{
			if (target.TryGetComponent<Attributes>(out var attribute) == false) return;
			nameText.text = attribute.ArticleName;
			descText.text = attribute.ArticleDescription;
		}

		private void UpdateDetailedView(GameObject target)
		{
			var tips = target.GetComponents<IHoverTooltip>();
			foreach (var data in tips)
			{
				if (String.IsNullOrEmpty(data.CustomTitle()) == false) nameText.text = data.CustomTitle();
				if (String.IsNullOrEmpty(data.HoverTip())) continue;
				descText.text += $"\n \n{data.HoverTip()}";
				UpdateIconSprite(data);
			}
		}

		private void ResetTool()
		{
			nameText.text = string.Empty;
			descText.text = string.Empty;
			iconTarget.sprite = null;
			ResetInteractionsList();
			content.LeanAlpha(0f, ANIM_TIME);
		}

		private void ResetInteractionsList()
		{
			if (interactionList.childCount == 0) return;
			for (int i = interactionList.childCount; i > interactionList.childCount; i--)
			{
				Destroy(interactionList.GetChild(i));
			}
		}
	}
}