using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hacking
{
	public class GUI_HackingPort : MonoBehaviour
	{
		public Image Image;

		public Image Highlight;

		public GUI_HackingOutputAndInput.PortData PortData;


		public GUI_HackingOutputAndInput GUI_HackingOutputAndInput;

		public void SetUp(GUI_HackingOutputAndInput.PortData InPortData,
			GUI_HackingOutputAndInput inGUI_HackingOutputAndInput)
		{
			GUI_HackingOutputAndInput = inGUI_HackingOutputAndInput;
			PortData = InPortData;
			Image.color = PortData.Colour.GetColor();
		}

		public void UnSelect(bool Update = true)
		{
			Highlight.gameObject.SetActive(false);
			if (Update)
			{
				GUI_HackingOutputAndInput.SetPortSelected(null);
			}
		}

		public void OnPressedEvent()
		{
			if (Highlight.gameObject.activeSelf == true)
			{
				Highlight.gameObject.SetActive(false);
				GUI_HackingOutputAndInput.SetPortSelected(null);
			}
			else
			{
				Pickupable handItem = PlayerManager.LocalPlayerScript.Equipment.ItemStorage.GetActiveHandSlot().Item;
				if (handItem != null)
				{
					if (Validations.HasItemTrait(handItem.gameObject, CommonTraits.Instance.Cable))
					{
						Highlight.gameObject.SetActive(true);
						GUI_HackingOutputAndInput.SetPortSelected(this);
					}
				}
			}

		}
	}
}