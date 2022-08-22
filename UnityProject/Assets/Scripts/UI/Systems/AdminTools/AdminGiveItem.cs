using System;
using System.Collections;
using System.Collections.Generic;
using AdminCommands;
using AdminTools;
using TMPro;
using UI.Systems.AdminTools.DevTools.Search;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.AdminTools
{
	public class AdminGiveItem : MonoBehaviour
	{
		private SpawnerSearch spawnerSearch;
		public string selectedPlayerId;

		[SerializeField] private List<GameObject> recommendedGameObjects;

		[SerializeField] private InputField messageInput;
		[SerializeField] private InputField countInput;
		[SerializeField] private TMP_InputField searchBox;
		[SerializeField] private GameObject normalList;
		[SerializeField] private GameObject recommendedList;
		[SerializeField] private GameObject itemEntry;
		[SerializeField] private GUI_AdminTools adminTools;

		private void Start()
		{
			if (recommendedGameObjects.Count > 0)
			{
				spawnerSearch = SpawnerSearch.ForPrefabs(recommendedGameObjects);
				Search(recommendedList);
			}
			spawnerSearch = SpawnerSearch.ForPrefabs(Spawn.SpawnablePrefabs());
			Search(normalList);
		}

		private void OnDisable()
		{
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}

		private void OnEnable()
		{
			UIManager.IsInputFocus = true;
			UIManager.PreventChatInput = true;
		}

		public void OnSearchBoxChanged()
		{
			Search(normalList);
		}

		public void Search(GameObject list)
		{
			if (list == recommendedList)
			{
				foreach (var doc in spawnerSearch.documents)
				{
					CreateListItem(doc, list);
				}
				return;
			}

			if (searchBox.text.Length < 2) return;

			// delete previous results
			foreach (Transform child in normalList.transform)
			{
				Destroy(child.gameObject);
			}

			var docs = spawnerSearch.Search(searchBox.text);

			// display new results
			foreach (var doc in docs)
			{
				CreateListItem(doc, list);
			}
		}

		private void CreateListItem(DevSpawnerDocument doc, GameObject list)
		{
			GameObject listItem = Instantiate(itemEntry, list.transform);
			listItem.GetComponent<AdminGiveItemEntry>().Initialize(doc, this);
			listItem.SetActive(true);
		}


		public void GiveItem(DevSpawnerDocument selectedPrefab)
		{
			var count = Convert.ToInt32(countInput.text);
			AdminCommandsManager.Instance.CmdGivePlayerItem(selectedPlayerId, selectedPrefab.Prefab.name, count, messageInput.text);
		}

		public void GoBack()
		{
			adminTools.ShowPlayerManagePage();
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}
	}
}