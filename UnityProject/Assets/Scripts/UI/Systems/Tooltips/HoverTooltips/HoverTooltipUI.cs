using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.Tooltips.HoverTooltips
{
	public class HoverTooltipUI : MonoBehaviour
	{
		[SerializeField] private CanvasGroup content;
		[SerializeField] private Transform interactionList;
		[SerializeField] private TMP_Text interactionPrefab;
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
			// Clean up everything for the upcoming data.
			ResetTool();
			// Don't do anything if there's no object to start with.
			if (hoverObject == null) return;

			//Setup the title and description.
			UpdateMainInfo(hoverObject);
			CaptureIconFromSpriteHandler(hoverObject);
			if(detailsModeEnabled) UpdateDetailedView(hoverObject);

			// Don't show if the description/name is empty.
			// (Max): It looks better and more intentional when there's no empty fields.
			// Also reduces hovertip presence on the screen when its not needed.
			if (IsDescOrTitleEmpty()) return;
			content.LeanAlpha(1f, ANIM_TIME);
		}

		/// <summary>
		/// As the name implies, grabs the icon for the hovertip from the gameObject's sprite handler.
		/// Will not do anything if the gameObject does not have one.
		/// </summary>
		private void CaptureIconFromSpriteHandler(GameObject target)
		{
			var imageObj = targetObject.GetComponentInChildren<SpriteHandler>();
			if (imageObj != null)
			{
				iconTarget.sprite = imageObj.CurrentSprite;
			}
		}

		/// <summary>
		/// Updates the icon to another sprite that an IHoverTooltip returns.
		/// </summary>
		private void UpdateIconSprite(IHoverTooltip target)
		{
			if (target.CustomIcon() == null) return;
			iconTarget.sprite = target.CustomIcon();
		}

		/// <summary>
		/// Grabs the item name and description from the Attributes base class which ItemAttributes and ObjectAttributes inherit from.
		/// </summary>
		private void UpdateMainInfo(GameObject target)
		{
			if (target.TryGetComponent<Attributes>(out var attribute) == false) return;
			nameText.text = attribute.ArticleName;
			descText.text = attribute.ArticleDescription;
		}

		/// <summary>
		/// The extra data to show when the player presses shift.
		/// </summary>
		private void UpdateDetailedView(GameObject target)
		{
			var tips = target.GetComponents<IHoverTooltip>();
			foreach (var data in tips)
			{
				if (String.IsNullOrEmpty(data.CustomTitle()) == false) nameText.text = data.CustomTitle();
				if (String.IsNullOrEmpty(data.HoverTip()) == false)
				{
					descText.text += $"\n \n{data.HoverTip()}";
				}
				UpdateIconSprite(data);
				// Only show interactions if there is a description or title in the tooltip.
				if (IsDescOrTitleEmpty() == false) UpdateInteractionsView(data.InteractionsStrings());
			}
		}

		private bool IsDescOrTitleEmpty()
		{
			return string.IsNullOrEmpty(descText.text) || string.IsNullOrEmpty(nameText.text);
		}

		private void UpdateInteractionsView(List<TextColor> newInteractions)
		{
			if (newInteractions == null) return;
			Debug.Log(newInteractions.Count);
			foreach (var interaction in newInteractions)
			{
				var textObj = Instantiate(interactionPrefab, interactionList, false);
				textObj.text = $"<color={interaction.Color.ToString()}>{interaction.Text}</color>";
				textObj.SetActive(true);
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