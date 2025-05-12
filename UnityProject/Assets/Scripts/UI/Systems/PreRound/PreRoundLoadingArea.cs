using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.PreRound
{
	public class PreRoundLoadingArea : MonoBehaviour
	{
		[SerializeField] private Scrollbar loadingBar = null;
		[SerializeField] private TMP_Text loadingTitle = null;
		[SerializeField] private TMP_Text loadingSubject = null;
		[SerializeField] private TMP_Text loadingTooLongWarning = null;
		[SerializeField] private TMP_Text loadingTextDetailed = null;

		public void UpdateLoadingBar(string title, string subject, float loadedAmt)
		{
			if (loadedAmt >= 1f)
			{
				loadingBar.size = 1f;
				gameObject.SetActive(false);
			}
			if (gameObject.activeSelf == false)
			{
				gameObject.SetActive(true);
			}
			loadingTitle.text = title;
			loadingSubject.text = subject;
			loadingBar.size = loadedAmt;
		}
	}
}