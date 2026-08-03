using System.Collections.Generic;
using Logs;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using US13.Core.Input_System;
using US13.Managers.UpdateManager;
using US13.UI.Systems;
using US13.UI.Systems.Tooltips.HoverTooltips;

namespace US13.UI.Core
{
	public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,IHoverTooltip
	{
		[SerializeField]
		private GameObject tooltipTemplate = null;
		const float TOOLTIP_INTERVAL = 1.0f;
		private float enterTime = 0;
		private GameObject tooltipObject = null;
		[SerializeField]
		private string tooltipText = "";

		private string TitleOfTip = "";


		void Start()
		{
			if (tooltipTemplate == null) return;
			tooltipObject = Instantiate(tooltipTemplate, Vector3.zero, Quaternion.identity);
			var text = tooltipObject.GetComponentInChildren<Text>();
			if (text != null)
			{
				text.text = tooltipText;
			}

			var TMP_Text = tooltipObject.GetComponentInChildren<TMP_Text>();
			if (TMP_Text != null)
			{
				TMP_Text.text = tooltipText;
			}
			// While the tooltip exists, we place it under the canvas so it'll be in the top layer
			tooltipObject.transform.SetParent(this.GetComponentInParent<Canvas>().transform);
			tooltipObject.SetActive(false);
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		void UpdateMe()
		{
			if (tooltipObject == null) return;
			if (tooltipObject.activeSelf) {
				// Move tooltip to mouse
				tooltipObject.transform.position = CommonInput.mousePosition - new Vector3(0, 20, 0);
			} else if (enterTime != 0 && Time.realtimeSinceStartup - enterTime > TOOLTIP_INTERVAL) {
				// Move tooltip above all other layers. We do it now so new objects wont hide it.
				tooltipObject.transform.SetAsLastSibling();
				tooltipObject.SetActive(true);
				tooltipObject.transform.position = CommonInput.mousePosition - new Vector3(0, 20, 0);
			}
		}

		public void SetText(string newText) {
			tooltipText = newText;
			tooltipObject.GetComponentsInChildren<Text>(false)[0].text = tooltipText;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			enterTime = Time.realtimeSinceStartup;
			UIManager.Instance.HoverTooltipUI.SetupTooltip(gameObject);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			enterTime = 0;

			tooltipObject?.SetActive(false);
			UIManager.SetHoverToolTip = null;
		}


		public string HoverTip()
		{
			return  tooltipText;
		}


		public string CustomTitle()
		{
			if (string.IsNullOrEmpty(TitleOfTip))
			{
				return gameObject.name;
			}
			else
			{
				return TitleOfTip;
			}
		}


		public Sprite CustomIcon() => null;


		public List<Sprite> IconIndicators() => null;

		public List<TextColor> InteractionsStrings() => null;
	}
}
