using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CharacterCreator
{
	public class BodyPartDropDownReplaceOrgan : BodyPartCustomisationBase
	{
		public Dropdown Dropdown;

		public List<BodyPart> ToChooseFromBodyParts = new List<BodyPart>();

		public BodyPart CurrentBodyPart;

		public BodyPart ParentBodyPart; // can be null with root

		public override void SetUp(CharacterCustomization incharacterCustomization, BodyPart Body_Part, string path)
		{
			base.SetUp(incharacterCustomization, Body_Part, path);
			RelatedBodyPart = Body_Part;
			List<string> itemOptions = null;
			// Make a list of all available options which can then be passed to the dropdown box
			foreach (var Keyv in incharacterCustomization.ParentDictionary)
			{
				if (Keyv.Value.Contains(Body_Part))
				{
					ParentBodyPart = Keyv.Key;
				}
			}

			CurrentBodyPart = Body_Part;
			ToChooseFromBodyParts = Body_Part.OptionalReplacementOrgan;
			itemOptions = Body_Part.OptionalReplacementOrgan.Select(gameObject => gameObject.name).ToList();


			itemOptions.Sort();

			// Ensure "None" is at the top of the option lists
			itemOptions.Insert(0, Body_Part.name);
			Dropdown.AddOptions(itemOptions);
			Dropdown.onValueChanged.AddListener(ItemChange);
		}

		public override void Deserialise(string InData)
		{
			var Newvalue = JsonConvert.DeserializeObject<int>(InData);
			if (Newvalue >= (ToChooseFromBodyParts.Count + 1))
			{
				Newvalue = 0;
			}
			Dropdown.value = Newvalue;
		}

		public override string Serialise()
		{
			return JsonConvert.SerializeObject(Dropdown.value);
		}

		public static void OnPlayerBodyDeserialise(BodyPart bodyPart, string InData)
		{
			var PreviousOptions = JsonConvert.DeserializeObject<int>(InData);
			if (PreviousOptions >= bodyPart.OptionalReplacementOrgan.Count + 1)
			{
				return;
			}
			var spawned = Spawn.ServerPrefab(bodyPart.OptionalReplacementOrgan[PreviousOptions].gameObject);

			bodyPart.HealthMaster.BodyPartStorage.ServerTryAdd(spawned.GameObject);
			bodyPart.HealthMaster.BodyPartStorage.ServerTryRemove(bodyPart.gameObject);
		}

		public void SetDropdownValue(string currentSetting)
		{
			// Find the index of the setting in the dropdown list which matches the currentSetting
			int settingIndex = Dropdown.options.FindIndex(option => option.text == currentSetting);

			if (settingIndex != -1)
			{
				// Make sure FindIndex is successful before changing value
				Dropdown.value = settingIndex;
			}
			else
			{
				Logger.LogWarning($"Unable to find index of {currentSetting}! Using default", Category.Character);
				Dropdown.value = 0;
			}
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
		}

		public override void RandomizeValues()
		{
			Dropdown.value = Random.Range(0, Dropdown.options.Count - 1);
			Refresh();
		}

		public void ItemChange(int newValue)
		{
			Refresh();
		}

		public override void Refresh()
		{
			base.Refresh();

			if (Dropdown.value == 0)
			{
				if (CurrentBodyPart != null)
				{
					characterCustomization.RemoveBodyPart(CurrentBodyPart);
				}

				characterCustomization.ParentDictionary[ParentBodyPart].Add(RelatedBodyPart);
				characterCustomization.SetUpBodyPart(RelatedBodyPart, false);
				CurrentBodyPart = RelatedBodyPart;
			}
			else
			{
				if (CurrentBodyPart != null)
				{
					if (CurrentBodyPart == RelatedBodyPart)
					{
						characterCustomization.RemoveBodyPart(CurrentBodyPart, false);
					}
					else
					{
						characterCustomization.RemoveBodyPart(CurrentBodyPart);
					}
				}

				var ChosenOption = Dropdown.options[Dropdown.value].text;
				CurrentBodyPart = ToChooseFromBodyParts.First(x => x.name == ChosenOption);
				characterCustomization.ParentDictionary[ParentBodyPart].Add(CurrentBodyPart);
				characterCustomization.SetUpBodyPart(CurrentBodyPart);
			}
		}
	}
}
