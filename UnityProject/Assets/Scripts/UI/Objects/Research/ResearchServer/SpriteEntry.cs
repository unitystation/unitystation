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
		[SerializeField] private TMP_Text toolTipText;
		[SerializeField] private NetClientSyncString syncString;

		private bool toolTipActive = false;

		public void Initialise(SpriteDataSO spriteData, string toolTipText)
		{
			spriteHandler.MasterSetValue(spriteData.SetID);
			syncString.MasterSetValue(toolTipText);
			
			toolTip.SetActive(false);
		}

		public void Awake()
		{
			toolTip.SetActive(false);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			toolTip.SetActive(true);
			toolTipText.text = syncString.Value;
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
