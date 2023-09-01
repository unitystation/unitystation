using AdminTools;
using Antagonists;
using Firebase.Firestore;
using Items;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveAttributesEntry : MonoBehaviour
{
	[SerializeField] private TMP_Text attributeName;
	private ObjectiveAttribute attribute;
	public ObjectiveAttribute Attribute => attribute;
	private Objective obj;

	[SerializeField] private TMP_InputField inputNumber;
	[SerializeField] private Dropdown inputPlayer;
	[SerializeField] private Dropdown inputItem;

	private Dictionary<int, AdminPlayerEntry> players = new();
	private Dictionary<int, GameObject> items = new();

	ObjectiveManagerPage antagManagerPage;

	public void Init(ObjectiveManagerPage antagManagerPageSet, ObjectiveAttribute attributeToSet, Objective objective)
	{
		antagManagerPage = antagManagerPageSet;
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
	}

	private void SetUpPlayers()
	{
		var newOptions = new List<Dropdown.OptionData>();
		players.Clear();

		List<AdminPlayerEntry> list = antagManagerPage.MainPage.GetPlayerEntries();
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
}
