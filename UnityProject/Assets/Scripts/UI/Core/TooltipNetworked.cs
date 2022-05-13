using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Core
{
	public class TooltipNetworked : NetworkBehaviour
	{
		const float TOOLTIP_INTERVAL = 1.0f;
		private float enterTime = 0;
		[SerializeField] private GameObject tooltipObject = null;
		[SyncVar] public string TooltipText = "No description setup yet.";
		private Text tooltipTextUI;

		void Awake()
		{
			tooltipTextUI = tooltipObject.GetComponentInChildren<Text>();
			tooltipTextUI.text = TooltipText;
			tooltipObject.SetActive(false);
		}

		private void OnEnable()
		{
			if(CustomNetworkManager.IsHeadless) return;
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			if(CustomNetworkManager.IsHeadless) return;
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		void UpdateMe()
		{
			if (tooltipObject.gameObject.activeSelf)
			{
				// Move tooltip to mouse
				tooltipObject.transform.position = CommonInput.mousePosition - new Vector3(0, 20, 0);
			}

			if (enterTime == 0 || Time.realtimeSinceStartup - enterTime < TOOLTIP_INTERVAL) return;
			// Move tooltip above all other layers. We do it now so new objects wont hide it.
			tooltipObject.transform.SetAsLastSibling();
			tooltipObject.SetActive(true);
			UpdateOnTextChange();
			tooltipObject.transform.position = CommonInput.mousePosition - new Vector3(0, 20, 0);
		}

		private void UpdateOnTextChange()
		{
			Debug.Log(TooltipText);
			tooltipTextUI.text = TooltipText;
		}


		public void PointerEnter()
		{
			enterTime = Time.realtimeSinceStartup;
		}

		public void PointerExit()
		{
			enterTime = 0;

			tooltipObject.SetActive(false);
		}
	}
}