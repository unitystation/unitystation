using System.Collections;
using Items.Bureaucracy;
using TMPro;
using UnityEngine;

namespace UI.Items
{
	public class GUI_Book : NetTab
	{
		[SerializeField] private TMP_Text txt;
		[SerializeField] private TMP_Text pageNumber;
		[SerializeField] private Transform nextPage;
		[SerializeField] private Transform previousPage;
		private BookWritable book;
		private int currentPage = 0;

		public override void OnEnable()
		{
			base.OnEnable();
			StartCoroutine(WaitForProvider());
			UIManager.IsInputFocus = true;
			UIManager.PreventChatInput = true;
		}

		public void OnDisable()
		{
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}

		public void ClosePaper()
		{
			ControlTabs.CloseTab(Type, Provider);
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			RefreshText();
		}

		private void RefreshText()
		{
			if (Provider == null || Provider.TryGetComponent<BookWritable>(out var paper) == false) return;
			book = Provider.GetComponent<BookWritable>();
			txt.text = book.Pages[currentPage].PageContent;
			pageNumber.text = book.Pages[currentPage].PageNumber.ToString();
			ShowButtons();
		}

		public void NextPage()
		{
			currentPage++;
			if (currentPage >= book.Pages.Count - 1)
			{
				currentPage = book.Pages.Count - 1;
			}

			RefreshText();
		}

		public void PreviousPage()
		{
			currentPage--;
			if (currentPage <= 0)
			{
				currentPage = 0;
			}
			RefreshText();
		}

		private void ShowButtons()
		{
			book.PlayPageTurnSound();
			if (book.Pages.Count <= 1)
			{
				previousPage.SetActive(false);
				nextPage.SetActive(false);
				return;
			}

			if (currentPage >= book.Pages.Count - 1)
			{
				nextPage.SetActive(false);
			}
			else
			{
				nextPage.SetActive(true);
			}

			if (currentPage <= 0)
			{
				previousPage.SetActive(false);
			}
			else
			{
				previousPage.SetActive(true);
			}
		}
	}
}