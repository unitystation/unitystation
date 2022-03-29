using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Core.Windows
{
	public class TeleportButtonSearchBar : MonoBehaviour
	{
		[SerializeField]
		private InputField searchText;

		private List<GameObject> HiddenButtons = new List<GameObject>();

		public void Search()
		{
			ClearSearchResults();

			var buttons = gameObject.transform.parent.GetComponent<TeleportWindow>().TeleportButtons;//Grabs fresh list of all the possible buttons

			for (int i = 0; i < buttons.Count; i++)
			{
				GameObject button = buttons[i];
				if (button == null) continue;

				if (button.GetComponent<TeleportButton>().myText.text.ToLower().Contains(searchText.text.ToLower()) | searchText.text.Length == 0)
				{
					continue;
				}

				HiddenButtons.Add(button);//non-results get hidden
				button.SetActive(false);
			}
		}

		private void ClearSearchResults()
		{
			foreach (GameObject x in HiddenButtons)//Hidden Buttons stores list of the hidden items which dont contain the search phrase
			{
				if (x != null)
				{
					x.SetActive(true);
				}
			}
			HiddenButtons.Clear();
		}

		public void ResetText()//resets search field text everytime window is closed
		{
			searchText.text = "";
		}
	}
}
