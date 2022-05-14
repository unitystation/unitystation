using UnityEngine;
using UnityEngine.UI;

namespace UI.Core
{
	/// <summary>
	/// Copies the text from a target and displays it in a tooltip. (Useful for NetUI)
	/// </summary>
	public class TooltipTextCopier : MonoBehaviour
	{
		const float TOOLTIP_INTERVAL = 1.0f;
		private float enterTime = 0;
		[SerializeField] private GameObject tooltipObject = null;
		private Text tooltipTextUI;
		[SerializeField] private Text TextToCopy;

		void Awake()
		{
			tooltipTextUI = tooltipObject.GetComponentInChildren<Text>();
			tooltipTextUI.text = "No description set yet";
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
			tooltipTextUI.text = TextToCopy.text;
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