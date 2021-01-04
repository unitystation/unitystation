using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiDropDownSelector : MonoBehaviour
{
	public List<string> options = new List<string>();

	public List<DropDownSelectorItem> OpenItems = new List<DropDownSelectorItem>();

	public GameObject PopulationArea;

	public DropDownSelectorItem Item;

	public List<bool> value = new List<bool>();

	public GameObject dropdown;

	public void AddOptions(List<string> Options)
	{
		foreach (var Itmem in OpenItems)
		{
			Destroy(Itmem);
		}

		options = Options;
		foreach (var STItem in options)
		{
			value.Add(false);
			var inItem = Instantiate(Item, PopulationArea.transform);
			OpenItems.Add(inItem);
			inItem.Setup(this, STItem);
		}
	}

	public void SetValues(List<bool> Toues)
	{
		value = new List<bool>(Toues);
		for (int i = 0; i < OpenItems.Count; i++)
		{
			OpenItems[i].SetValue(value[i]);
		}
	}

	public event Action<List<bool>> onValueChanged;

	public void ValueChanged()
	{
		for (int i = 0; i < options.Count; i++)
		{
			value[i] = OpenItems[i].IsActive();
		}

		onValueChanged?.Invoke(value);
	}


	public void Setvisible()
	{
		dropdown.SetActive(!dropdown.activeSelf);
	}
}