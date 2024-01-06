using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Chat_UI;
using UnityEngine;

namespace UISearchWithPreview
{
	public class SearchWithPreview : MonoBehaviour
	{
		public List<ISearchSpritePreview> SearchOptions = new List<ISearchSpritePreview>();

		public GameObject contentPanel;

		[Tooltip("If searchWhileTyping is turned on, don't start searching until at least this many" +
		         " characters are entered.")]
		public int minCharactersForSearch = 2;

		public TMP_InputField searchBox;

		public SearchWithPreviewItem listItemPrefab;

		public event Action<ISearchSpritePreview> ItemChosen;

		public void Awake()
		{
			searchBox.onEndEdit.AddListener(OnSearchBoxChanged);
		}

		public void Setup(List<ISearchSpritePreview> List)
		{
			SearchOptions = List;
			gameObject.SetActive(true);

		}

		public void OptionChosen(ISearchSpritePreview NewValue)
		{
			ItemChosen?.Invoke(NewValue);
			Close();
		}

		public void Search()
		{
			OnSearchBoxChanged(searchBox.text);
		}



		public void OnSearchBoxChanged(string data)
		{
			Search(data);
		}

		public void Search(string StringySearch)
		{
			if (StringySearch.Length < minCharactersForSearch) return;

			// delete previous results
			foreach (Transform child in contentPanel.transform)
			{
				Destroy(child.gameObject);
			}


			var ToUse = SearchOptions.Where(x => x.Name.Contains(StringySearch));


			foreach (var Item in ToUse)
			{
				CreateListItem(Item);
			}
		}

		// add a list item to the content panel for spawning the specified result
		private void CreateListItem(ISearchSpritePreview doc)
		{
			var listItem = Instantiate(listItemPrefab, contentPanel.transform);
			listItem.SetUp(doc, this);
			listItem.transform.localScale = Vector3.one;
		}

		public void Close()
		{
			gameObject.SetActive(false);
			// delete previous results
			foreach (Transform child in contentPanel.transform)
			{
				Destroy(child.gameObject);
			}
		}

	}


}

