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
		private ObjectiveAttribute currentAttribute;
		public ObjectiveAttribute Attribute => currentAttribute;
		private Objective obj;

		[SerializeField] private TMP_InputField inputNumber;
		[SerializeField] private GameObject inputNumberBackground;
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
			currentAttribute = attributeToSet;
			obj = objective;
			currentAttribute.index = objective.GetAttributeIndex(currentAttribute);
			SetUpAttributes();
			UpdateAttribute();
		}

		private void SetUpAttributes()
		{
			if (currentAttribute.type == ObjectiveAttributeType.ObjectiveAttributeNumber)
			{
				inputNumberBackground.SetActive(true);
				inputNumber.gameObject.SetActive(true);
				inputPlayer.gameObject.SetActive(false);
				inputItem.gameObject.SetActive(false);
				SetUpNumbers();
			}
			else if (currentAttribute.type == ObjectiveAttributeType.ObjectiveAttributePlayer)
			{
				inputNumber.gameObject.SetActive(false);
				inputPlayer.gameObject.SetActive(true);
				inputItem.gameObject.SetActive(false);
				SetUpPlayers();
			}
			else if (currentAttribute.type == ObjectiveAttributeType.ObjectiveAttributeItem)
			{
				inputNumber.gameObject.SetActive(false);
				inputPlayer.gameObject.SetActive(false);
				inputItem.gameObject.SetActive(true);
				SetUpItems();
			}
			else if (currentAttribute.type == ObjectiveAttributeType.ObjectiveAttributeItemTrait)
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
			if (currentAttribute.type == ObjectiveAttributeType.ObjectiveAttributeNumber)
			{
				if (int.TryParse(inputNumber.text, out var result) && (result > 0 || result == -1))
					currentAttribute.Number = result;
				else
				{
					inputNumber.text = "1";
					currentAttribute.Number = 1;
				}
			}
			else if (currentAttribute.type == ObjectiveAttributeType.ObjectiveAttributePlayer)
			{
				currentAttribute.PlayerID = players[inputPlayer.value].PlayerData.uid;
			}
			else if (currentAttribute.type == ObjectiveAttributeType.ObjectiveAttributeItem && items[inputItem.value].TryGetComponent<PrefabTracker>(out var itemTracker))
			{
				currentAttribute.ItemID = itemTracker.ForeverID;
			}
			else if (currentAttribute.type == ObjectiveAttributeType.ObjectiveAttributeItemTrait)
			{
				currentAttribute.ItemTraitIndex = CommonTraits.Instance.GetIndex(itemTraits[inputItem.value]);
			}
		}
	}
}