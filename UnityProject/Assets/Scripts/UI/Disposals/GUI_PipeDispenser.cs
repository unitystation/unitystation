using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// TODO: Figure out toggling a toggle's interactable.
// TODO: Add hover box to dispensable objects, showing the object's
// name and description from its objectattributes component

public class GUI_PipeDispenser : NetTab
{
#pragma warning disable 0649
	[SerializeField] NetPageSwitcher categorySwitcher;
	[SerializeField] NetPageSwitcher dispensePageSwitcher;

	[SerializeField] List<NetToggle> layerToggles;
	[SerializeField] List<NetToggle> colorToggles;
	[SerializeField] List<NetPage> categoryPages;
#pragma warning restore 0649

	int currentCategoryNumber = 0;
	int[] previousCategoryPages;

	PipeDispenser.PipeLayer pipeLayer = PipeDispenser.PipeLayer.LayerTwo;
	Color pipeColor = Color.white;

	PipeDispenser pipeDispenser;

	#region Initialisation

	private void Awake()
	{
		// This assumes that each category page has only the toggles for children
		// and that the dispensePages are sequential with accordance to category.
		previousCategoryPages = new int[]
		{
			0,
			categoryPages[0].transform.childCount,
			categoryPages[0].transform.childCount + categoryPages[1].transform.childCount
		};

		StartCoroutine(WaitForProvider());
	}

	IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}

		pipeDispenser = Provider.GetComponent<PipeDispenser>();
	}

	#endregion Initialisation

	void EnableToggles(IEnumerable<NetToggle> toggles)
	{
		foreach (NetToggle toggle in toggles)
		{
			// TODO: Enable toggle's interactable
		}
	}

	void DisableToggles(IEnumerable<NetToggle> toggles)
	{
		foreach (NetToggle toggle in toggles)
		{
			// TODO: Disable toggle's interactable
		}
	}

	void EnableLayerAndColorToggles()
	{
		EnableToggles(layerToggles);
		EnableToggles(colorToggles);
	}

	void DisableLayerAndColorToggles()
	{
		layerToggles[1].SetValueServer("1"); // Engage the toggle of Layer 2.
		colorToggles[0].SetValueServer("1"); // Engage the toggle of Color Grey.
		DisableToggles(layerToggles);
		DisableToggles(colorToggles);
	}

	Color GetColorFromNumber(int colorNumber)
	{
		// These colors may need tweaking, as the objects are usually already
		// dark in nature and so may end up a bit darker than anticipated.
		switch (colorNumber)
		{
			case 0: return Color.white;
			case 1: return Color.red;
			case 2: return Color.green;
			case 3: return Color.blue;
			case 4: return Color.yellow;
			case 5: return Color.cyan;
			default: return Color.grey;
		}
	}

	PipeDispenser.PipeLayer GetLayerFromNumber(int layerNumber)
	{
		switch (layerNumber)
		{
			case 1: return PipeDispenser.PipeLayer.LayerOne;
			case 2: return PipeDispenser.PipeLayer.LayerTwo;
			case 3: return PipeDispenser.PipeLayer.LayerThree;
			default: return PipeDispenser.PipeLayer.LayerTwo;
		}
	}

	#region Buttons

	public void ServerSetCategory(int categoryNumber)
	{
		categorySwitcher.SetActivePage(categoryNumber);
		dispensePageSwitcher.SetActivePage(previousCategoryPages[categoryNumber]);

		switch (categoryNumber)
		{
			case 0: // Atmospherics
				EnableLayerAndColorToggles();
				break;
			case 1: // Disposals
				DisableLayerAndColorToggles();
				break;
			case 2: // TransitTubes
				DisableLayerAndColorToggles();
				break;
		}

		currentCategoryNumber = categoryNumber;
	}

	public void ServerSetLayer(int layerNumber)
	{
		pipeLayer = GetLayerFromNumber(layerNumber);
	}

	public void ServerSetColor(int colorNumber)
	{
		pipeColor = GetColorFromNumber(colorNumber);
	}

	public void ServerSetDispensePage(int pageNumber)
	{
		dispensePageSwitcher.SetActivePage(pageNumber);
		previousCategoryPages[currentCategoryNumber] = pageNumber;
	}

	public void ServerDispenseObject(GameObject objectPrefab)
	{
		pipeDispenser.Dispense(objectPrefab, pipeLayer, pipeColor);
	}

	#endregion Buttons
}
