using Antagonists;
using Items;
using StationObjectives;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace AdminTools
{
	public class ObjectiveAttributesEntry : MonoBehaviour
	{
		[SerializeField] private TMP_Text attributeName;
		private ObjectiveAttribute attribute;
		public ObjectiveAttribute Attribute => attribute;
		private Objective obj;

		[SerializeField] private TMP_InputField inputNumber;
		[SerializeField] private Dropdown inputPlayer;
		[SerializeField] private Dropdown inputItem;

		private readonly Dictionary<int, AdminPlayerEntry> players = new();
		private readonly Dictionary<int, GameObject> items = new();
		private readonly Dictionary<int, ItemTrait> itemTraits = new();

		GUI_AdminTools mainPage;

		public void Init(GUI_AdminTools mainPageToSet, ObjectiveAttribute attributeToSet, Objective objective)
		{
			mainPage = mainPageToSet;
			attributeName.text = attributeToSet.name;
			attribute = attributeToSet;
			obj = objective;
			attribute.index = objective.GetAttributeIndex(attribute);

			if (attribute is ObjectiveAttributeNumber)
			{
				inputNumber.gameObject.SetActive(true);
				inputPlayer.gameObject.SetActive(false);
				inputItem.gameObject.SetActive(false);
				SetUpNumbers();
			}
			else if (attribute is ObjectiveAttributePlayer)
			{
				inputNumber.gameObject.SetActive(false);
				inputPlayer.gameObject.SetActive(true);
				inputItem.gameObject.SetActive(false);
				SetUpPlayers();
			}
			else if (attribute is ObjectiveAttributeItem)
			{
				inputNumber.gameObject.SetActive(false);
				inputPlayer.gameObject.SetActive(false);
				inputItem.gameObject.SetActive(true);
				SetUpItems();
			}
			else if (attribute is ObjectiveAttributeItemTrait)
			{
				inputNumber.gameObject.SetActive(false);
				inputPlayer.gameObject.SetActive(false);
				inputItem.gameObject.SetActive(true);
				SetUpItemsTraits();
			}
		}

		private void SetUpPlayers()
		{
			var newOptions = new List<Dropdown.OptionData>();
			players.Clear();

			List<AdminPlayerEntry> list = mainPage.GetPlayerEntries();
			for (int i = 0; i < list.Count; i++)
			{
				AdminPlayerEntry x = list[i];
				newOptions.Add(new Dropdown.OptionData(x.PlayerData.name));
				players.Add(i, x);
			}
			inputPlayer.ClearOptions();
			inputPlayer.value = 0;
			inputPlayer.AddOptions(newOptions);
		}

		private void SetUpNumbers()
		{
			inputNumber.text = $"{1}";
		}

		private void SetUpItems()
		{
			if (obj is Steal steal)
			{
				var newOptions = new List<Dropdown.OptionData>();
				items.Clear();
				var pool = steal.ItemPools;

				for (int i = 0; i < pool.Count; i++)
				{
					var item = pool.Keys.ElementAt(i);
					newOptions.Add(new Dropdown.OptionData(item.GetComponent<ItemAttributesV2>().InitialName));
					items.Add(i, item);
				}
				inputItem.ClearOptions();
				inputItem.value = 0;
				inputItem.AddOptions(newOptions);
			}
		}

		private void SetUpItemsTraits()
		{
			if (obj is ShipResources objective)
			{
				var newOptions = new List<Dropdown.OptionData>();
				items.Clear();
				var pool = objective.ItemPool;

				for (int i = 0; i < pool.Count; i++)
				{
					var item = pool.Keys.ElementAt(i);
					newOptions.Add(new Dropdown.OptionData(item.name));
					itemTraits.Add(i, item);
				}
				inputItem.ClearOptions();
				inputItem.value = 0;
				inputItem.AddOptions(newOptions);
			}
		}

		public void UpdateAttribute()
		{
			if (attribute is ObjectiveAttributeNumber number)
			{
				if (int.TryParse(inputNumber.text, out var result) && result > 0)
					number.number = result;
				else
				{
					inputNumber.text = "1";
					number.number = 1;
				}
			}
			else if (attribute is ObjectiveAttributePlayer player)
			{
				player.playerID = players[inputPlayer.value].PlayerData.uid;
			}
			else if (attribute is ObjectiveAttributeItem item && items[inputItem.value].TryGetComponent<PrefabTracker>(out var itemTracker))
			{
				item.itemID = itemTracker.ForeverID;
			}
			else if (attribute is ObjectiveAttributeItemTrait itemTrait)
			{
				itemTrait.itemTraitIndex = CommonTraits.Instance.GetIndex(itemTraits[inputItem.value]);
			}
		}
	}
}