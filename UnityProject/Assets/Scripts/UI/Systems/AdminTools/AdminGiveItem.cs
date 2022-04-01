using System;
using System.Collections;
using System.Collections.Generic;
using AdminTools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.AdminTools
{
	public class AdminGiveItem : MonoBehaviour
	{
		private SpawnerSearch spawnerSearch;
		public ConnectedPlayer selectedPlayer;

		[SerializeField] private List<GameObject> recommendedGameObjects;

		[SerializeField] private InputField messageInput;
		[SerializeField] private InputField countInput;
		[SerializeField] private TMP_InputField searchBox;
		[SerializeField] private GameObject normalList;
		[SerializeField] private GameObject recommendedList;
		[SerializeField] private GameObject itemEntry;
		[SerializeField] private GUI_AdminTools adminTools;

		private  void Start()
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
			if(selectedPlayer == null || selectedPrefab.Prefab == null) return;
			var count = Convert.ToInt32(countInput.text);
			var item = Spawn.ServerPrefab(selectedPrefab.Prefab, selectedPlayer.Script.mind.body.gameObject.AssumedWorldPosServer());
			var slot = selectedPlayer.Script.DynamicItemStorage.GetBestHandOrSlotFor(item.GameObject);
			if (item.GameObject.TryGetComponent<Stackable>(out var stackable) && stackable.MaxAmount <= count)
			{
				stackable.ServerSetAmount(count);
			}
			if (slot != null)
			{
				Inventory.ServerAdd(item.GameObject, slot);
			}
			if(messageInput.text.Contains("") == false) Chat.AddExamineMsg(selectedPlayer.Script.gameObject, messageInput.text);
			Chat.AddExamineMsg(PlayerManager.LocalPlayer, $"You have given {selectedPlayer.Script.visibleName} : {item.GameObject.ExpensiveName()}");
		}

		public void GoBack()
		{
			adminTools.ShowPlayerManagePage();
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}
	}
}