using TMPro;
using UnityEngine;

namespace Learning
{
	public class ProtipListUI : MonoBehaviour
	{
		[SerializeField] private Transform entryToSpawn;
		[SerializeField] private Transform entryList;
		[SerializeField] private Transform mainPage;


		private void Awake()
		{
			RefreshList();
		}

		public void CloseUI()
		{
			gameObject.SetActive(false);
		}
		public void OnPressWikiButton()
		{
			//TODO : ADD THE WIKI IN-GAME PAGE THAT LETS YOU SEARCH ARTICLES TO OPEN LIKE SS13

		}

		public void OnPressCatalougePage()
		{
			HideAllPages();
			mainPage.SetActive(true);
		}

		private void HideAllPages()
		{
			mainPage.SetActive(false);
		}

		private void RefreshList()
		{
			if (entryList.childCount > 0)
			{
				for (int i = 0; i < entryList.childCount - 1; i++)
				{
					Destroy(entryList.GetChild(i).gameObject);
				}
			}
			foreach (var tipSO in ProtipManager.Instance.RecordedProtips)
			{
				var newEntry = Instantiate(entryToSpawn, entryList);
				newEntry.GetComponent<ProtipListEntry>().Setup(tipSO);
			}
		}
	}
}