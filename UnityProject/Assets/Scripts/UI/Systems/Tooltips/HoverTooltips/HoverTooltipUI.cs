using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.Tooltips.HoverTooltips
{
	public class HoverTooltipUI : MonoBehaviour
	{
		[SerializeField] private Transform content;
		[SerializeField] private TMP_Text nameText;
		[SerializeField] private TMP_Text descText;
		[SerializeField] private Image iconTarget;

		private GameObject targetObject;

		private const float offsety = 90f;
		private const float offsetx = 105f;


		private void Start()
		{
			UpdateManager.Add(CallbackType.FIXED_UPDATE, UpdatePosition);
		}

		private void UpdatePosition()
		{
			if (targetObject == null) return;
			var newPosition = new Vector3(Input.mousePosition.x + offsetx, Input.mousePosition.y + offsety,
				Input.mousePosition.z);
			transform.position = newPosition;
		}

		public void SetupTooltip(GameObject hoverObject)
		{
			targetObject = hoverObject;
			ResetTool();
			if (hoverObject == null) return;
			if (hoverObject.TryGetComponent<Attributes>(out var attribute))
			{
				nameText.text = attribute.ArticleName;
				descText.text = attribute.ArticleDescription;
			}
			var imageObj = hoverObject.GetComponentInChildren<SpriteHandler>();
			if (imageObj != null)
			{
				iconTarget.sprite = imageObj.CurrentSprite;
			}
			var tips = hoverObject.GetComponents<IHoverTooltip>();
			foreach (var data in tips)
			{
				if (String.IsNullOrEmpty(data.CustomTitle()) == false) nameText.text = data.CustomTitle();
				if (String.IsNullOrEmpty(data.HoverTip())) continue;
				descText.text += $"\n \n{data.HoverTip()}";
			}
			content.SetActive(true);
		}

		private void ResetTool()
		{
			nameText.text = string.Empty;
			descText.text = string.Empty;
			iconTarget.sprite = null;
			content.SetActive(false);
		}
	}
}