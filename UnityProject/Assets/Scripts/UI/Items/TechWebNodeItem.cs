using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Research;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

namespace UI.Items
{
	public class TechwebNodeItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		public Technology techData;
		public TechType type;
		public int cost;

		private UnityEvent onMouseHover;
		private UnityEvent onMouseLoseFocus;

		private GameObject background;
		private TMP_Text nodeTitle;
		private TMP_Text description;
		private TMP_Text costText;

		private void OnEnable()
		{
			onMouseHover.AddListener(ShowInfo);
			onMouseLoseFocus.AddListener(HideInfo);
		}

		private void OnDisable()
		{
			onMouseHover.RemoveListener(ShowInfo);
			onMouseLoseFocus.RemoveListener(HideInfo);
		}

		public void Setup(Technology technology)
		{
			techData = technology;
			nodeTitle.text = technology.DisplayName;
			description.text = technology.Description;
			costText.text = technology.ResearchCosts.ToString();
		}

		private void ShowInfo()
		{
			background.LeanAlpha(1, 0.2f);
		}
		private void HideInfo()
		{
			background.LeanAlpha(0, 0.2f);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			onMouseHover.Invoke();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			onMouseLoseFocus.Invoke();
		}
	}
}
