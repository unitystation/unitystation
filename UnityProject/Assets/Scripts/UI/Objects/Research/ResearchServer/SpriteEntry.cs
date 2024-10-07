using TMPro;
using UI.Core.Net.Elements;
using UI.Core.NetUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Objects.Research
{
	public class SpriteEntry : DynamicEntry, IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField] private NetSpriteHandler spriteHandler;
		[SerializeField] private GameObject toolTip;
		[SerializeField] private NetText_label toolTipText;

		private bool toolTipActive = false;

		public void Initialise(SpriteDataSO spriteData, string toolTipText)
		{
			spriteHandler.MasterSetValue(spriteData.SetID);
			this.toolTipText.MasterSetValue(toolTipText);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			toolTip.SetActive(true);
		
			toolTipActive = true;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			toolTip.SetActive(false);
			toolTipActive = false;
		}


		private void OnDisable()
		{
			if (toolTipActive == true)
			{
				toolTip.SetActive(false);
				toolTipActive = false;
			}
		}

	}
}
