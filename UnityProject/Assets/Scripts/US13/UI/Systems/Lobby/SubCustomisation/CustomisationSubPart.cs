using System.Linq;
using Logs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.Clothing;
using US13.Core.Sprite_Handler;
using US13.HealthV2.Living.BodyParts;
using US13.Systems.Lobby;
using US13.UI.Systems.Lobby.SubCustomisation.BodyPartCustomisations;

namespace US13.UI.Systems.Lobby.SubCustomisation
{
	public class CustomisationSubPart : MonoBehaviour, DropDownCustomProvider
	{
		public TMP_Text HeadName;

		public Dropdown Dropdown;

		public Color color = Color.white;

		public global::US13.Clothing.CustomisationType ThisType = global::US13.Clothing.CustomisationType.Null;

		public SpriteHandler RelatedSpriteRenderer;

		public CustomisationGroup thisCustomisations;

		public Image SelectionColourImage;

		public CharacterCustomization characterCustomization;

		public SpriteOrder spriteOrder;

		public SpriteHandlerNorder spriteHandlerNorder;

		[SerializeField] private bool canRandomizeColors = false;

		public void Setup(CustomisationGroup Customisations, CharacterCustomization incharacterCustomization, SpriteOrder _SpriteOrder)
		{
			thisCustomisations = Customisations;
			characterCustomization = incharacterCustomization;
			HeadName.text = Customisations.ThisType.ToString();
			spriteOrder = _SpriteOrder;
			spriteHandlerNorder.SetSpriteOrder(spriteOrder);
			ThisType = Customisations.ThisType;

			// Make a list of all available options which can then be passed to the dropdown box
			var itemOptions = Customisations.PlayerCustomisations.Select(pcd => pcd.Name).ToList();
			itemOptions.Sort();

			// Ensure "None" is at the top of the option lists
			itemOptions.Insert(0, "None");
			Dropdown.AddOptions(itemOptions);

			RelatedSpriteRenderer.gameObject.transform.SetParent(incharacterCustomization.SpriteContainer.transform);
			RelatedSpriteRenderer.gameObject.transform.localPosition = Vector3.zero;
			Dropdown.onValueChanged.AddListener(ItemChange);
		}


		public void CustomSetup(CustomDropDownItem Content, string dropdownItem)
		{
			var data = thisCustomisations.PlayerCustomisations.FirstOrDefault(pcd => pcd.Name == dropdownItem);

			if (data == null)
			{
				Loggy.Error($"Unable to find entry for {dropdownItem} ");
				return;
			}

			Content.Image.SetSpriteSO(data.SpriteEquipped);
			Content.Image.SetColor(color);
		}


		public CharacterSheet.CustomisationClass Serialise()
		{
			var newcurrentSetting = new CharacterSheet.CustomisationClass();
			// if (thisCustomisations.CanColour)
			// {
			newcurrentSetting.Colour = "#" + ColorUtility.ToHtmlStringRGB(color);
			// }
			newcurrentSetting.SelectedName = Dropdown.options[Dropdown.value].text;


			return newcurrentSetting;

		}

		public void RandomizeValues()
		{
			Dropdown.value = Random.Range(0, Dropdown.options.Count - 1);
			if (canRandomizeColors)
			{
				ColorChange(new Color(Random.Range(0.1f, 1f), Random.Range(0.1f, 1f), Random.Range(0.1f, 1f), 1f));
			}
			Refresh();
		}

		public void SetDropdownValue(CharacterSheet.CustomisationClass currentSetting)
		{
			// Find the index of the setting in the dropdown list which matches the currentSetting
			int settingIndex = Dropdown.options.FindIndex(option => option.text == currentSetting.SelectedName);

			if (settingIndex != -1)
			{
				// Make sure FindIndex is successful before changing value
				Dropdown.value = settingIndex;
			}
			else
			{
				Loggy.Warning($"Unable to find index of {currentSetting}! Using default", Category.Character);
				Dropdown.value = 0;
			}

			// if (thisCustomisations.CanColour)
			// {
			//currentCharacter.HairColor = "#" + ColorUtility.ToHtmlStringRGB(UnityEngine.Random.ColorHSV());
			Color setColor = Color.black;
			ColorUtility.TryParseHtmlString(currentSetting.Colour, out setColor);
			color = setColor;
			SelectionColourImage.color = setColor;
			RelatedSpriteRenderer.SetColor(setColor);
			// }

			Refresh();

		}

		public void DropdownScrollRight()
		{
			// Check if value should wrap around
			if (Dropdown.value < Dropdown.options.Count - 1)
			{
				Dropdown.value++;
			}
			else
			{
				Dropdown.value = 0;
			}

			//Refresh();
		}

		public void DropdownScrollLeft()
		{
			// Check if value should wrap around
			if (Dropdown.value > 0)
			{
				Dropdown.value--;
			}
			else
			{
				Dropdown.value = Dropdown.options.Count - 1;
			}
			//Refresh();
		}

		public void SetRotation(int newValue)
		{
			RelatedSpriteRenderer.SetSpriteVariant(newValue);
		}

		public void ItemChange(int newValue)
		{
			Refresh();
		}

		public void RequestColourPicker()
		{
			characterCustomization.OpenColorPicker(color, ColorChange, 32f);
		}

		private void ColorChange(Color newColor)
		{
			color = newColor;
			Refresh();
		}

		public void Refresh()
		{
			if (Dropdown.value == 0)
			{
				RelatedSpriteRenderer.Empty();
			}
			else
			{
				var ChosenOption = Dropdown.options[Dropdown.value].text;
				var GotOption = thisCustomisations.PlayerCustomisations.First(x => x.Name == ChosenOption);
				RelatedSpriteRenderer.SetSpriteSO(GotOption.SpriteEquipped);
			}

			// set Selection and sprite
			RelatedSpriteRenderer.SetColor(color);
			SelectionColourImage.color = color;
		}
	}
}
