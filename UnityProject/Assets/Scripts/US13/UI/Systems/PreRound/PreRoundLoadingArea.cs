using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.Managers.UpdateManager;

namespace US13.UI.Systems.PreRound
{
	public class PreRoundLoadingArea : MonoBehaviour
	{
		[SerializeField] private Scrollbar loadingBar = null;
		[SerializeField] private TMP_Text loadingTitle = null;
		[SerializeField] private TMP_Text loadingSubject = null;
		[SerializeField] private TMP_Text loadingTooLongWarning = null;
		[SerializeField] private TMP_Text loadingTextDetailed = null;

		private int loadingTooLongWarningCount = 0;

		private void OnEnable()
		{
			UpdateManager.Add(UpdateMe, 1f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			loadingTooLongWarningCount = 0;
		}

		private void UpdateMe()
		{
			loadingTooLongWarningCount++;
			if (loadingTooLongWarningCount > 8)
			{
				loadingTooLongWarning?.gameObject.SetActive(true);
			}
			else
			{
				loadingTooLongWarning?.gameObject.SetActive(false);
			}
		}

		public void UpdateLoadingBar(string title, string subject, float loadedAmt, string detail = "")
		{
			if (title == "" && subject == "") return;
			if (gameObject.activeSelf == false)
			{
				gameObject.SetActive(true);
			}
			loadingTitle.text = title;
			loadingSubject.text = loadingTextDetailed == null && !string.IsNullOrEmpty(detail)
				? $"{subject}\n{detail}" : subject;
			loadingBar.size = Mathf.Clamp01(loadedAmt);
			if (loadingTextDetailed != null) loadingTextDetailed.text = detail;
		}
	}
}
