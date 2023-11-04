using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Atmospherics;
using UI.Systems.Tooltips.HoverTooltips;

// TODO: Figure out toggling a toggle's interactable.
// TODO: Add hover box to dispensable objects, showing the object's
// name and description from its objectattributes component

namespace UI.Objects.Atmospherics
{
	public class GUI_PipeDispenser : NetTab, IHoverTooltip
	{
		[SerializeField] private NetPageSwitcher categorySwitcher = default;
		[SerializeField] private NetPageSwitcher dispensePageSwitcher = default;

		[SerializeField] private List<NetToggle> layerToggles = default;
		[SerializeField] private List<NetToggle> colorToggles = default;
		[SerializeField] private List<NetPage> categoryPages = default;
		[SerializeField] private List<Color> colors = default;

		private int currentCategoryNumber = 0;
		private int[] previousCategoryPages;

		private PipeDispenser.PipeLayer pipeLayer = PipeDispenser.PipeLayer.LayerTwo;
		private Color pipeColor;

		private PipeDispenser pipeDispenser;

		private GameObject currentHoverTarget;

		#region Initialisation

		private void Awake()
		{
			pipeColor = colors[0];
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

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			pipeDispenser = Provider.GetComponent<PipeDispenser>();
		}

		#endregion Initialisation

		private void EnableToggles(IEnumerable<NetToggle> toggles)
		{
			foreach (NetToggle toggle in toggles)
			{
				// TODO: Enable toggle's interactable
			}
		}

		private void DisableToggles(IEnumerable<NetToggle> toggles)
		{
			foreach (NetToggle toggle in toggles)
			{
				// TODO: Disable toggle's interactable
			}
		}

		private void EnableLayerAndColorToggles()
		{
			EnableToggles(layerToggles);
			EnableToggles(colorToggles);
		}

		private void DisableLayerAndColorToggles()
		{
			layerToggles[1].MasterSetValue("1"); // Engage the toggle of Layer 2.
			colorToggles[0].MasterSetValue("1"); // Engage the toggle of Color Grey.
			DisableToggles(layerToggles);
			DisableToggles(colorToggles);
		}

		private Color GetColorFromNumber(int colorNumber)
		{
			return colors[colorNumber];
		}

		private PipeDispenser.PipeLayer GetLayerFromNumber(int layerNumber)
		{
			return layerNumber switch
			{
				1 => PipeDispenser.PipeLayer.LayerOne,
				2 => PipeDispenser.PipeLayer.LayerTwo,
				3 => PipeDispenser.PipeLayer.LayerThree,
				_ => PipeDispenser.PipeLayer.LayerTwo,
			};
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

		public void ClientSetDispenseToolTip(GameObject target)
		{
			currentHoverTarget = target;
			UIManager.SetHoverToolTip = this.gameObject;
		}

		public void ClientResetTip()
		{
			currentHoverTarget = null;
			UIManager.SetHoverToolTip = null;
		}

		#endregion Buttons

		public string HoverTip()
		{
			return currentHoverTarget.GetComponent<Attributes>().InitialDescription;
		}

		public string CustomTitle()
		{
			return currentHoverTarget.GetComponent<Attributes>().InitialName;
		}

		public Sprite CustomIcon() => null;

		public List<Sprite> IconIndicators() => null;

		public List<TextColor> InteractionsStrings() => null;
	}
}
